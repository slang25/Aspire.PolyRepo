using System.Diagnostics;
using System.Text;
using Dutchskull.Aspire.PolyRepo.Interfaces;
using LibGit2Sharp;
using Microsoft.Extensions.Logging;
using LogLevel = Microsoft.Extensions.Logging.LogLevel;

namespace Dutchskull.Aspire.PolyRepo;

public class ProcessCommandExecutor : IProcessCommandExecutor
{
    private readonly ILogger<ProcessCommandExecutor> _logger;

    public ProcessCommandExecutor()
    {
        using ILoggerFactory loggerFactory = LoggerFactory.Create(loggingBuilder => loggingBuilder
            .SetMinimumLevel(LogLevel.Trace)
            .AddConsole());

        _logger = loggerFactory.CreateLogger<ProcessCommandExecutor>();
    }

    public int BuildDotNetProject(string resolvedProjectPath) =>
        RunProcess("dotnet", $"build {resolvedProjectPath}");

    public void CloneGitRepository(string gitUrl, string resolvedRepositoryPath, string? branch = null) =>
        Repository.Clone(gitUrl, resolvedRepositoryPath, new CloneOptions { BranchName = branch });

    public int NpmInstall(string resolvedRepositoryPath) =>
        RunProcess("cmd.exe", $"/C cd {resolvedRepositoryPath} && npm i");

    public void PullAndResetRepository(string repositoryConfigRepositoryPath)
    {
        using Repository repository = new(repositoryConfigRepositoryPath);

        string? branchName = repository.Head.TrackedBranch.FriendlyName;
        Remote? remote = repository.Network.Remotes.FirstOrDefault();

        ArgumentNullException.ThrowIfNull(remote);
        ArgumentNullException.ThrowIfNull(branchName);

        FetchOptions fetchOptions = new();
        IEnumerable<string> references = remote.FetchRefSpecs.Select(x => x.Specification);
        Commands.Fetch(repository, remote.Name, references, fetchOptions, null);

        Branch? remoteBranch = repository.Branches[branchName];
        Commit? latestCommit = remoteBranch.Tip;

        repository.Reset(ResetMode.Hard, latestCommit);
    }

    private int RunProcess(string fileName, string arguments)
    {
        Process process = new()
        {
            StartInfo = new ProcessStartInfo
            {
                FileName = fileName,
                Arguments = arguments,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            }
        };

        StringBuilder output = new();
        StringBuilder error = new();

        process.OutputDataReceived += LogData(output, message => _logger.LogInformation(message!));

        process.ErrorDataReceived += LogData(error, message => _logger.LogError(message));

        process.Start();
        process.BeginOutputReadLine();
        process.BeginErrorReadLine();
        process.WaitForExit();

        if (process.ExitCode == 0)
        {
            _logger.LogInformation("Process {fileName} {arguments} finished successfully.", fileName, arguments);

            return process.ExitCode;
        }

        _logger.LogError("Process {fileName} {arguments} failed with exit code {process.ExitCode}: {error}", fileName,
            arguments, process.ExitCode, error);

        throw new Exception($"Process {fileName} {arguments} failed with exit code {process.ExitCode}: {error}");
    }

    private static DataReceivedEventHandler LogData(StringBuilder output, Action<string> logger)
    {
        return (_, e) =>
        {
            if (string.IsNullOrEmpty(e.Data))
            {
                return;
            }

            output.AppendLine(e.Data);
            logger(e.Data);
        };
    }
}