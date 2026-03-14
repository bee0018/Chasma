using ChasmaWebApi.Core.Interfaces.Git;
using ChasmaWebApi.Core.Interfaces.Infrastructure;
using ChasmaWebApi.Data.Objects.Git;
using ChasmaWebApi.Data.Objects.Remote;
using ChasmaWebApi.Util;
using LibGit2Sharp;
using System.Diagnostics;

namespace ChasmaWebApi.Core.Services.Git
{
    /// <summary>
    /// Service class containing the implementation of the members on the Git repository service, which is responsible for handling Git repository-level operations such as fetching branches and commits from a repository.
    /// </summary>
    public class GitRepositoryService : IGitRepositoryService
    {
        /// <summary>
        /// The logger instance for this service, used for logging information and errors related to Git repository operations.
        /// </summary>
        private readonly ILogger<GitRepositoryService> Logger;

        /// <summary>
        /// The cache manager, which is responsible for managing cached data such as repository statuses and GitHub pull request information to optimize performance of Git operations.
        /// </summary>
        private readonly ICacheManager CacheManager;

        /// <summary>
        /// The Git branch service, which is responsible for handling branch related operations.
        /// </summary>
        private readonly IGitBranchService GitBranchService;

        #region Constructor

        /// <summary>
        /// Initializes a new instance of the <see cref="GitRepositoryService"/> class.
        /// </summary>
        /// <param name="logger">The logger used to record diagnostic and operational messages for the service.</param>
        /// <param name="cacheManager">The cache manager used to store and retrieve cached data for repository operations.</param>
        /// <param name="branchService">The Git branch service used to perform branch-related operations.</param>
        public GitRepositoryService(ILogger<GitRepositoryService> logger, ICacheManager cacheManager, IGitBranchService branchService)
        {
            Logger = logger;
            CacheManager = cacheManager;
            GitBranchService = branchService;
        }

        #endregion

        // <inheritdoc/>
        public RepositorySummary? GetRepositoryStatus(string repoKey, string username, string token)
        {
            if (!CacheManager.WorkingDirectories.TryGetValue(repoKey, out string workingDirectory))
            {
                Logger.LogError("Invalid repository key {repoKey} provided to get repository status.", repoKey);
                return null;
            }

            using Repository repo = new Repository(workingDirectory);
            List<RepositoryStatusElement> statusElements = new();
            RepositoryStatus status = repo.RetrieveStatus();
            foreach (StatusEntry item in status)
            {
                FileStatus state = item.State;
                if (state == FileStatus.Ignored)
                {
                    // We only care about modified, deleted, and new files for now.
                    continue;
                }

                bool isStaged = IsFileStaged(state, out bool hasUnstagedChanges);
                RepositoryStatusElement statusElement = new()
                {
                    RepositoryId = repoKey,
                    FilePath = item.FilePath,
                    State = item.State,
                    IsStaged = isStaged,
                };
                statusElements.Add(statusElement);
                if (hasUnstagedChanges)
                {
                    // Add another commitEntry for the unstaged changes.
                    RepositoryStatusElement unstagedElement = new()
                    {
                        RepositoryId = repoKey,
                        FilePath = item.FilePath,
                        State = FileStatus.ModifiedInWorkdir,
                        IsStaged = false,
                    };
                    statusElements.Add(unstagedElement);
                }
            }

            Logger.LogInformation("Retrieved repository status for {repoKey} with {count} changes.", repoKey, statusElements.Count);
            (string branchName, int aheadCount, int behindCount) = GetBranchDiversionCalculation(workingDirectory, username, token);
            string remoteUrl = GetRemoteUrl(repo.Head, repo.Network.Remotes, workingDirectory) ?? string.Empty;
            string commitHash = GetCommitHash(repo.Head, Logger);
            List<GitHubPullRequest> gitHubPullRequests = CacheManager.GitHubPullRequests.Values.Where(i => i.BranchName == branchName).ToList();
            RepositorySummary repositorySummary = new()
            {
                StatusElements = statusElements,
                CommitsAhead = aheadCount,
                CommitsBehind = behindCount,
                BranchName = branchName,
                RemoteUrl = remoteUrl,
                CommitHash = commitHash,
                PullRequests = gitHubPullRequests,
            };
            return repositorySummary;
        }

        // <inheritdoc />
        public List<RepositoryStatusElement>? ApplyStagingAction(string repoKey, string fileName, bool stagingFile, string username, string token)
        {
            List<RepositoryStatusElement> statusElements = new();
            if (!CacheManager.WorkingDirectories.TryGetValue(repoKey, out string workingDirectory))
            {
                Logger.LogError("Invalid repository key {repoKey} provided to stage the file {fileName}.", repoKey, fileName);
                return statusElements;
            }

            using Repository repo = new Repository(workingDirectory);
            string stagingAction;
            if (stagingFile)
            {
                stagingAction = "Staged";
                Commands.Stage(repo, fileName);
            }
            else
            {
                stagingAction = "Unstaged";
                Commands.Unstage(repo, fileName);
            }

            Logger.LogInformation("{action} file {file}", stagingAction, fileName);
            RepositorySummary summary = GetRepositoryStatus(repoKey, username, token);
            return summary?.StatusElements;
        }

        // <inheritdoc />
        public void CommitChanges(string filePath, string fullName, string email, string commitMessage)
        {
            using Repository repo = new(filePath);
            Signature author = new(fullName, email, DateTimeOffset.Now);
            repo.Commit(commitMessage, author, author);

        }

        // <inheritdoc />
        public bool TryPushChanges(string filePath, string token, out string errorMessage)
        {
            errorMessage = string.Empty;
            using Repository repo = new Repository(filePath);
            Branch branch = repo.Head;
            if (branch == null)
            {
                errorMessage = $"Failed to push changes. Could not get branch information for repository at {filePath}.";
                Logger.LogError(errorMessage);
                return false;
            }

            if (repo.Info.IsHeadDetached)
            {
                errorMessage = $"Failed to push changes. The HEAD is in a detached state for repository at {filePath}.";
                Logger.LogError(errorMessage);
                return false;
            }

            if (branch.TrackedBranch == null)
            {
                errorMessage = $"Failed to push changes. No upstream set for branch {branch.FriendlyName}.";
                Logger.LogError(errorMessage);
                return false;
            }

            try
            {
                string username = repo.Config.Get<string>("user.name")?.Value ?? "chasma-bot";
                PushOptions options = new()
                {
                    CredentialsProvider = (url, usernameFromUrl, types) =>
                        new UsernamePasswordCredentials
                        {
                            Username = username,
                            Password = token,
                        }
                };

                repo.Network.Push(branch, options);
                return true;
            }
            catch (Exception e)
            {
                Logger.LogWarning("Failed to push changes automatically, trying manual push.");
                if (!ShellUtility.TryExecuteShellCommand("git push", filePath, out string pushError))
                {
                    errorMessage = $"Failed to push changes to remote for branch {branch.FriendlyName}: {pushError}";
                    Logger.LogError(e, pushError);
                    return false;

                }

                return true;
            }
        }

        // <inheritdoc />
        public bool TryPullChanges(string workingDirectory, string fullName, string email, string token, out string errorMessage)
        {
            errorMessage = string.Empty;
            using Repository repo = new Repository(workingDirectory);
            string username = repo.Config.Get<string>("user.name")?.Value ?? "chasma-bot";
            Signature author = new(fullName, email, DateTimeOffset.Now);
            PullOptions options = new()
            {
                FetchOptions = new FetchOptions
                {
                    CredentialsProvider = (url, usernameFromUrl, types) =>
                        new UsernamePasswordCredentials
                        {
                            Username = username,
                            Password = token,
                        }
                }
            };

            try
            {
                Commands.Pull(repo, author, options);
                return true;
            }
            catch (Exception e)
            {
                Logger.LogWarning("Failed to automatically pull changes. Attempting manual pull.");
                if (!ShellUtility.TryExecuteShellCommand("git pull", workingDirectory, out string pullError))
                {
                    errorMessage = $"Failed to pull changes from remote for repository at {workingDirectory}: {pullError}";
                    Logger.LogError(e, pullError);
                    return false;
                }

                return true;
            }
        }

        // <inheritdoc />
        public bool TryResetRepository(string workingDirectory, string revParseSpec, ResetMode resetMode, out string commitMessage, out string errorMessage)
        {
            errorMessage = string.Empty;
            commitMessage = string.Empty;
            try
            {
                string revision = !string.IsNullOrEmpty(revParseSpec) ? revParseSpec : "HEAD";
                using Repository repo = new(workingDirectory);
                repo.Reset(resetMode, revision);
                commitMessage = repo.Head.Tip.MessageShort;
                Logger.LogInformation("Successfully reset repository at {workingDirectory} to {revParseSpec} with reset mode {resetMode}.", workingDirectory, revParseSpec, resetMode);
                return true;
            }
            catch (Exception e)
            {
                errorMessage = $"Failed to reset repository to {revParseSpec} with reset mode {resetMode}. Check server logs for more information.";
                Logger.LogError(e, errorMessage);
                return false;
            }
        }

        // <inheritdoc />
        public bool TryGetGitDiff(string workingDirectory, string filePath, bool isStaged, out string diffContent, out string errorMessage)
        {
            diffContent = string.Empty;
            if (!Directory.Exists(workingDirectory))
            {
                errorMessage = $"The working directory {workingDirectory} does not exist on filesystem. Cannot diff {filePath}.";
                Logger.LogError(errorMessage);
                return false;
            }

            using Repository repo = new(workingDirectory);
            RepositoryStatus updatedFilesInRepo = repo.RetrieveStatus();
            StatusEntry matchedFile = updatedFilesInRepo.FirstOrDefault(i => i.FilePath == filePath);
            if (matchedFile == null)
            {
                errorMessage = $"The file {filePath} does not exist in the changeset of this repository status";
                Logger.LogError("{error}. Sending error response.", errorMessage);
                return false;
            }

            string command = GetDiffCommand(isStaged, matchedFile.State, out bool isNewInWorkingDirectory);
            using Process process = ShellUtility.GetFileProcessingShell(command, filePath, workingDirectory);
            process.Start();
            diffContent = process.StandardOutput.ReadToEnd();
            errorMessage = process.StandardError.ReadToEnd();
            process.WaitForExit();
            if (process.ExitCode != 0 && !isNewInWorkingDirectory)
            {
                /**
                 * git diff --no-index NUL return an error code of 1 so we added this check to make sure
                 * that we treat this as an error only when the file is not new in the working directory.
                 */
                errorMessage = $"Git diff command failed with exit code {process.ExitCode}. Error: {errorMessage}";
                Logger.LogError(errorMessage);
                return false;
            }

            return true;
        }

        #region Private Methods

        /// <summary>
        /// Determines if the file is staged based on its file status.
        /// </summary>
        /// <param name="fileStatus">The current file status.</param>
        /// <returns>True if the file is staged; false otherwise.</returns>
        private static bool IsFileStaged(FileStatus fileStatus, out bool hasUnstagedChanges)
        {
            hasUnstagedChanges = false;
            string[] statusStrings = fileStatus.ToString().Split(",");
            string[] trimmedStatusStrings = statusStrings.Select(i => i.Trim()).ToArray();
            FileStatus[] fileStatuses = trimmedStatusStrings.Select(Enum.Parse<FileStatus>).ToArray();
            bool isStaged = fileStatuses.Any(IsStateStaged);
            if (isStaged && fileStatuses.Any(state => state == FileStatus.ModifiedInWorkdir))
            {
                // The file is both staged and has unstaged changes.
                hasUnstagedChanges = true;
            }

            return isStaged;
        }

        /// <summary>
        /// Determines whether the file is staged in the repository.
        /// </summary>
        /// <param name="fileStatus">The current file status.</param>
        /// <returns>True if the file is in staged (in index); false otherwise.</returns>
        private static bool IsStateStaged(FileStatus fileStatus)
        {
            return fileStatus.HasFlag(FileStatus.NewInIndex) ||
                   fileStatus.HasFlag(FileStatus.ModifiedInIndex) ||
                   fileStatus.HasFlag(FileStatus.DeletedFromIndex) ||
                   fileStatus.HasFlag(FileStatus.RenamedInIndex) ||
                   fileStatus.HasFlag(FileStatus.TypeChangeInIndex);
        }

        /// <summary>
        /// Gets the remote URL for the specified repository.
        /// </summary>
        /// <param name="branch">The current checked out branch.</param>
        /// <param name="remoteBranches">The collection of remote branches.</param>
        /// <param name="workingDirectory">The working directory of the repository.</param>
        /// <returns>The remote URL of the repository.</returns>
        private string? GetRemoteUrl(Branch branch, RemoteCollection remoteBranches, string workingDirectory)
        {
            string remoteName = !string.IsNullOrEmpty(branch.RemoteName) ? branch.RemoteName : "origin";
            LibGit2Sharp.Remote? remote = remoteBranches[remoteName];
            if (remote == null)
            {
                Logger.LogWarning("Could not find remote {remoteName} for repository at {path}.", remoteName, workingDirectory);
                return null;
            }

            return remote.PushUrl ?? remote.Url;
        }

        /// <summary>
        /// Gets the commit hash for the specified branch.
        /// </summary>
        /// <param name="branch">The current branch.</param>
        /// <param name="logger">The logger.</param>
        /// <returns>The latest commit hash.</returns>
        private static string GetCommitHash(Branch branch, ILogger logger)
        {
            if (branch?.Tip == null)
            {
                logger.LogError("Cannot get commit hash. Failed to get branch information.");
                return string.Empty;
            }

            return GetCommitHash(branch.Tip, logger);
        }

        /// <summary>
        /// Gest the commit hash for the specified commit.
        /// </summary>
        /// <param name="commit">The commit.</param>
        /// <param name="logger">The logger.</param>
        /// <returns>The short commit hash.</returns>
        public static string GetCommitHash(Commit commit, ILogger logger)
        {
            if (commit == null)
            {
                logger.LogError("Cannot get commit hash. Commit information is null.");
                return string.Empty;
            }

            return commit.Sha.Length > 7 ? commit.Sha[..7] : commit.Sha;
        }

        /// <summary>
        /// Gets the diff command based on the file status and whether it is staged.
        /// </summary>
        /// <param name="isStaged">Flag indicating whether the file is staged.</param>
        /// <param name="fileStatus">The file status state.</param>
        /// <param name="isNewInWorkingDirectory">Flag indicating whether the file is new in the working directory.</param>
        /// <returns>The appropriate git diff command.</returns>
        private static string GetDiffCommand(bool isStaged, FileStatus fileStatus, out bool isNewInWorkingDirectory)
        {
            isNewInWorkingDirectory = false;
            if (fileStatus == FileStatus.NewInWorkdir)
            {
                isNewInWorkingDirectory = true;
                string emptyFilePlaceholderPath = OperatingSystem.IsWindows() ? "NUL" : "/dev/null";
                return $"git diff --no-index {emptyFilePlaceholderPath}";
            }
            else if (fileStatus == FileStatus.DeletedFromWorkdir || fileStatus == FileStatus.DeletedFromIndex)
            {
                return "git diff HEAD --";
            }
            else if (!isStaged)
            {
                return "git diff";
            }
            else
            {
                return "git diff --cached";
            }
        }

        /// <summary>
        /// Gets the branch diversion calculation for the specified repository.
        /// </summary>
        /// <param name="workingDirectory">The specified repository working directory.</param>
        /// <param name="username">The username for authentication when fetching updates from remote.</param>
        /// <param name="token">The token for authentication when fetching updates from remote.</param>
        /// <returns>The number of local branch name, commits ahead, and behind.</returns>
        private (string branchName, int aheadCount, int behindCount) GetBranchDiversionCalculation(string workingDirectory, string username, string token)
        {
            using Repository repo = new(workingDirectory);
            Branch branch = repo.Head;
            if (branch == null)
            {
                Logger.LogError("Cannot get branch diversion calculation. Failed to get branch information for repository at {path}.", repo.Info.WorkingDirectory);
                return ("", 0, 0);
            }

            if (repo.Info.IsHeadDetached)
            {
                Logger.LogWarning("Cannot get branch diversion calculation. The HEAD is in a detached state for repository at {path}.", repo.Info.WorkingDirectory);
                return ("", 0, 0);
            }

            try
            {
                FetchOptions fetchOptions = new()
                {
                    CredentialsProvider = (url, user, credentials) =>
                    new UsernamePasswordCredentials
                    {
                        Username = username,
                        Password = token
                    }
                };
                Commands.Fetch(repo, branch.RemoteName, [], fetchOptions, null);
            }
            catch (Exception e)
            {
                Logger.LogWarning(e, "Failed to fetch updates from remote {remote} for repository at {path}. Attempting manual fetch.", branch.RemoteName, repo.Info.WorkingDirectory);
                if (!ShellUtility.TryExecuteShellCommand("git fetch", workingDirectory, out string errorMessage))
                {
                    Logger.LogError(errorMessage);
                }
            }

            string localBranchName = branch.FriendlyName;
            if (string.IsNullOrEmpty(localBranchName))
            {
                Logger.LogError("Cannot get branch diversion calculation. No local branch found for repository at {path} with the branch name {branchName}.", repo.Info.WorkingDirectory, localBranchName);
                return ("", 0, 0);
            }

            if (branch.TrackedBranch == null)
            {
                Logger.LogWarning("Cannot get branch diversion calculation. Could not find the tracked branch for the local branch {branchName}.", localBranchName);
                return (localBranchName, 0, 0);
            }

            string upstreamBranchName = branch.TrackedBranch.FriendlyName;
            Branch localBranch = repo.Branches[localBranchName];
            Branch upstreamBranch = repo.Branches[upstreamBranchName];
            if (localBranch == null)
            {
                Logger.LogError("Cannot get branch diversion calculation. No local branch with name {branchName} found.", localBranchName);
                return (localBranchName, 0, 0);
            }

            if (upstreamBranch == null)
            {
                Logger.LogError("Cannot get branch diversion calculation. No upstream branch with name {branchName} found.", upstreamBranchName);
                return (localBranchName, 0, 0);
            }

            HistoryDivergence divergence = repo.ObjectDatabase.CalculateHistoryDivergence(localBranch.Tip, upstreamBranch.Tip);
            return (localBranchName, divergence.AheadBy ?? 0, divergence.BehindBy ?? 0);
        }

        #endregion
    }
}
