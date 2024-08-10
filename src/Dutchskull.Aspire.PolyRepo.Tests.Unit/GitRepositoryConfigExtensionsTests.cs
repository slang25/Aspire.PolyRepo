using Dutchskull.Aspire.PolyRepo.Interfaces;
using FluentAssertions;
using NSubstitute;

namespace Dutchskull.Aspire.PolyRepo.Tests.Unit;

public class GitRepositoryConfigExtensionsTests
{
    private const string GitUrl = "https://github.com/example/repo.git";
    private const string CloneTargetPath = "/custom/path";
    private const string Branch = "develop";
    private const string ProjectPath = "src";
    private const string WorktreePath = "/custom/worktree/path";

    private readonly RepositoryConfigBuilder _builder = new RepositoryConfigBuilder()
        .WithGitUrl(GitUrl)
        .WithFileSystem(Substitute.For<IFileSystem>())
        .WithProcessCommandExecutor(Substitute.For<IProcessCommandExecutor>());

    [Theory]
    [InlineData(true, 0)]
    [InlineData(false, 1)]
    public void Should_check_clone_is_called(bool pathIsCorrect, int callAmount)
    {
        // Arrange
        RepositoryConfig gitRepositoryConfig = _builder
            .WithTargetPath(CloneTargetPath)
            .Build();

        gitRepositoryConfig.FileSystem
            .DirectoryExists(gitRepositoryConfig.RepositoryPath)
            .Returns(pathIsCorrect);

        // Act
        gitRepositoryConfig.CloneRepository();

        // Assert
        gitRepositoryConfig.ProcessCommandsExecutor
            .Received(callAmount)
            .CloneGitRepository(gitRepositoryConfig.GitUrl, gitRepositoryConfig.RepositoryPath,
                gitRepositoryConfig.Branch);
    }

    [Fact]
    public void InitializeGitRepository_ShouldReturnInitializedConfig()
    {
        // Arrange
        IFileSystem fileSystem = Substitute.For<IFileSystem>();
        string expectedPath =
            Path.GetFullPath(Path.Combine(Path.Combine(Path.GetFullPath(CloneTargetPath), "repo"), ProjectPath));
        fileSystem.FileOrDirectoryExists(Arg.Is(expectedPath)).Returns(true);
        IProcessCommandExecutor processCommandsExecutor = Substitute.For<IProcessCommandExecutor>();

        var configureGitRepository = new Action<RepositoryConfigBuilder>(builder => builder
            .WithTargetPath(CloneTargetPath)
            .WithDefaultBranch(Branch)
            .WithFileSystem(fileSystem)
            .WithProcessCommandExecutor(processCommandsExecutor));

        // Act
        RepositoryConfig config = configureGitRepository.InitializeRepository(GitUrl);

        // Assert
        config.GitUrl.Should().Be(GitUrl);
        config.Branch.Should().Be(Branch);
        config.RepositoryPath.Should().Be(Path.Combine(Path.GetFullPath(CloneTargetPath), "repo"));
        config.ProcessCommandsExecutor.Should().Be(processCommandsExecutor);
        config.FileSystem.Should().Be(fileSystem);
    }

    [Fact]
    public void SetupWorktree_ShouldCreateWorktree_WhenWorktreeDoesNotExist()
    {
        // Arrange
        IFileSystem fileSystem = Substitute.For<IFileSystem>();
        fileSystem.DirectoryExists(WorktreePath).Returns(false);
        IProcessCommandExecutor processCommandsExecutor = Substitute.For<IProcessCommandExecutor>();

        RepositoryConfig gitRepositoryConfig = _builder
            .WithTargetPath(CloneTargetPath)
            .WithWorktree(WorktreePath)
            .WithFileSystem(fileSystem)
            .WithProcessCommandExecutor(processCommandsExecutor)
            .Build();

        // Act
        gitRepositoryConfig.SetupWorktree();

        // Assert
        processCommandsExecutor.Received(1).CreateWorktree(gitRepositoryConfig.RepositoryPath, WorktreePath, Branch);
    }

    [Fact]
    public void SetupWorktree_ShouldPullAndResetWorktree_WhenWorktreeExistsAndKeepUpToDateIsTrue()
    {
        // Arrange
        IFileSystem fileSystem = Substitute.For<IFileSystem>();
        fileSystem.DirectoryExists(WorktreePath).Returns(true);
        IProcessCommandExecutor processCommandsExecutor = Substitute.For<IProcessCommandExecutor>();

        RepositoryConfig gitRepositoryConfig = _builder
            .WithTargetPath(CloneTargetPath)
            .WithWorktree(WorktreePath)
            .KeepUpToDate()
            .WithFileSystem(fileSystem)
            .WithProcessCommandExecutor(processCommandsExecutor)
            .Build();

        // Act
        gitRepositoryConfig.SetupWorktree();

        // Assert
        processCommandsExecutor.Received(1).PullAndResetRepository(WorktreePath);
    }
}
