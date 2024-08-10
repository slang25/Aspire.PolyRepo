namespace Dutchskull.Aspire.PolyRepo;

internal static class RepositoryConfigExtensions
{
    internal static RepositoryConfig InitializeRepository(
        this Action<RepositoryConfigBuilder> configureGitRepository,
        string repositoryUrl)
    {
        RepositoryConfigBuilder gitRepositoryConfigBuilder = new();

        gitRepositoryConfigBuilder.WithGitUrl(repositoryUrl);

        configureGitRepository.Invoke(gitRepositoryConfigBuilder);

        return gitRepositoryConfigBuilder
            .Build()
            .SetupRepository();
    }

    internal static RepositoryConfig CloneRepository(this RepositoryConfig repositoryConfig)
    {
        bool repositoryExists = repositoryConfig.FileSystem.DirectoryExists(repositoryConfig.RepositoryPath);

        if (!repositoryExists)
        {
            repositoryConfig.ProcessCommandsExecutor
                .CloneGitRepository(
                    repositoryConfig.GitUrl,
                    repositoryConfig.RepositoryPath,
                    repositoryConfig.Branch);

            return repositoryConfig;
        }

        if (repositoryConfig.KeepUpToDate)
        {
            repositoryConfig.ProcessCommandsExecutor
                .PullAndResetRepository(repositoryConfig.RepositoryPath);
        }

        return repositoryConfig;
    }

    private static RepositoryConfig SetupRepository(this RepositoryConfig gitRepositoryConfig)
    {
        if (!string.IsNullOrEmpty(gitRepositoryConfig.WorktreePath))
        {
            return SetupWorktree(gitRepositoryConfig);
        }

        return CloneRepository(gitRepositoryConfig);
    }

    private static RepositoryConfig SetupWorktree(this RepositoryConfig gitRepositoryConfig)
    {
        bool worktreeExists = gitRepositoryConfig.FileSystem.DirectoryExists(gitRepositoryConfig.WorktreePath);

        if (!worktreeExists)
        {
            gitRepositoryConfig.ProcessCommandsExecutor
                .CreateWorktree(
                    gitRepositoryConfig.RepositoryPath,
                    gitRepositoryConfig.WorktreePath,
                    gitRepositoryConfig.Branch);

            return gitRepositoryConfig;
        }

        if (gitRepositoryConfig.KeepUpToDate)
        {
            gitRepositoryConfig.ProcessCommandsExecutor
                .PullAndResetRepository(gitRepositoryConfig.WorktreePath);
        }

        return gitRepositoryConfig;
    }
}
