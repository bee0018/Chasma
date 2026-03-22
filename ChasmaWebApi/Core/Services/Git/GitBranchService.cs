using ChasmaWebApi.Core.Interfaces.Git;
using ChasmaWebApi.Core.Interfaces.Infrastructure;
using ChasmaWebApi.Data.Objects.Application;
using ChasmaWebApi.Data.Objects.Git;
using ChasmaWebApi.Util;
using LibGit2Sharp;
using System.Diagnostics;

namespace ChasmaWebApi.Core.Services.Git
{
    /// <summary>
    /// Class containing the implementation of the members on the Git branch service, which is responsible for handling Git branch-level operations such as fetching and checking out branches from a repository.
    /// </summary>
    public class GitBranchService : IGitBranchService
    {
        /// <summary>
        /// Provides logging capabilities for the GitBranchService class.
        /// </summary>
        private readonly ILogger<GitBranchService> Logger;

        /// <summary>
        /// The cache manager, which is responsible for managing cached data such as repository statuses and GitHub pull request information to optimize performance of Git operations.
        /// </summary>
        private readonly ICacheManager CacheManager;

        /// <summary>
        /// The lock object used for concurrency.
        /// </summary>
        private readonly object lockObject = new();

        #region Constructor

        /// <summary>
        /// Initializes a new instance of the <see cref="GitBranchService"/> class.
        /// </summary>
        /// <param name="logger">The logger to use for logging diagnostic and operational information within the service.</param>
        /// <param name="cacheManager">The cache manager to use for managing cached data to optimize performance of Git operations.</param>
        public GitBranchService(ILogger<GitBranchService> logger, ICacheManager cacheManager)
        {
            Logger = logger;
            CacheManager = cacheManager;
        }

        #endregion

        // <inheritdoc/>
        public bool TryAddBranch(string workingDirectory, string branchName, string username, string token, out string errorMessage)
        {
            if (TryAddBranchAutomatically(workingDirectory, branchName, username, token, out errorMessage))
            {
                return true;
            }

            Logger.LogWarning("Automatic branch creation failed for branch {branchName} in repository at {repoPath} with error: {error}. Attempting manual branch creation.", branchName, workingDirectory, errorMessage);
            return TryAddBranchManually(workingDirectory, branchName, out errorMessage);
        }

        // <inheritdoc/>
        public bool TryDeleteBranch(string repositoryId, string branchName, out string errorMessage)
        {
            errorMessage = string.Empty;
            string workingDirectory = string.Empty;
            lock (lockObject)
            {
                if (!CacheManager.WorkingDirectories.TryGetValue(repositoryId, out workingDirectory))
                {
                    errorMessage = $"No working directory could be found with repository identifier: {repositoryId}.";
                    Logger.LogError("Repository identifier {id} cannot be found and no working directory could be retrieved. Sending error response.", repositoryId);
                    return false;
                }
            }

            using Repository repository = new(workingDirectory);
            Branch branchToDelete = repository.Branches.FirstOrDefault(i => i.FriendlyName == branchName);
            if (branchToDelete == null)
            {
                errorMessage = $"{branchName} does not exist in the repository.";
                Logger.LogError("Repository identifier {id} cannot find branch with name {branchName}. Sending error response.", repositoryId, branchName);
                return false;
            }

            repository.Branches.Remove(branchToDelete);
            Logger.LogInformation("Successfully deleted branch {branchName} from repository with id: {id}", branchName, repository);

            // Delete tracking of pull requests associated with this branch.
            StopTrackingRemotePullRequests(workingDirectory, branchName);
            return true;
        }

        // <inheritdoc />
        public bool TryCheckoutBranch(string workingDirectory, string branchName, out string errorMessage)
        {
            errorMessage = string.Empty;
            try
            {
                using Repository repo = new(workingDirectory);
                Commands.Checkout(repo, branchName);
                return true;
            }
            catch (Exception e)
            {
                Logger.LogWarning(e, "Failed to checkout branch {branchName} for repository at {workingDirectory}. Attempting manual checkout.", branchName, workingDirectory);
                if (ShellUtility.TryExecuteShellCommand($"git checkout {branchName}", workingDirectory, out errorMessage))
                {
                    return true;
                }

                errorMessage = $"Failed to checkout branch {branchName} for repository at {workingDirectory}. Check server logs for more information.";
                Logger.LogError(e, errorMessage);
                return false;
            }
        }

        // <inheritdoc />
        public List<string> GetAllBranches(string workingDirectory)
        {
            using Repository repo = new(workingDirectory);
            try
            {
                Commands.Fetch(repo, repo.Head.RemoteName, [], new FetchOptions(), null);
            }
            catch (Exception e)
            {
                Logger.LogWarning(e, "Failed to fetch updates from remote {remote} for repository at {path}. Attempting manual fetch.", repo.Head.RemoteName, repo.Info.WorkingDirectory);
                if (ShellUtility.TryExecuteShellCommand("git fetch", workingDirectory, out string errorMessage))
                {
                    Logger.LogError(errorMessage);
                }
            }

            return repo.Branches
                .Where(i => i.IsTracking)
                .Select(i => i.FriendlyName)
                .OrderBy(i => i)
                .ToList();
        }

        // <inheritdoc />
        public bool TryMergeBranch(string workingDirectory, string sourceBranchName, string destinationBranchName, string fullName, string email, string token, out string errorMessage)
        {
            if (TryMergeBranchAutomatically(workingDirectory, sourceBranchName, destinationBranchName, fullName, email, token, out errorMessage))
            {
                return true;
            }

            Logger.LogWarning("Automatic merge failed for branch {sourceBranchName} into {destinationBranchName}. Reason: {errorMessage}", sourceBranchName, destinationBranchName, errorMessage);
            return TryMergeBranchManually(workingDirectory, destinationBranchName, sourceBranchName, out errorMessage);
        }

        #region Private Methods

        /// <summary>
        /// Tries to merge the source branch into the destination branch automatically.
        /// </summary>
        /// <param name="workingDirectory">The working directory of the repository.</param>
        /// <param name="sourceBranchName">The source branch.</param>
        /// <param name="destinationBranchName">The destination branch to be merged in to.</param>
        /// <param name="fullName">The name of the user.</param>
        /// <param name="email">The email of the user.</param>
        /// <param name="token">The Git client API token.</param>
        /// <param name="errorMessage">The error message.</param>
        /// <returns>True if the branch was merged; false otherwise.</returns>
        private bool TryMergeBranchAutomatically(string workingDirectory, string sourceBranchName, string destinationBranchName, string fullName, string email, string token, out string errorMessage)
        {
            errorMessage = string.Empty;
            try
            {
                // The following git commands is what is being done programatically:
                // git fetch && git checkout main && git merge origin/feature && git push
                using Repository repo = new(workingDirectory);
                Branch sourceBranch = repo.Branches[sourceBranchName];
                if (sourceBranch == null)
                {
                    errorMessage = $"Source branch {sourceBranchName} does not exist.";
                    Logger.LogError("Source branch {sourceBranchName} does not exist. Sending error response.", sourceBranchName);
                    return false;
                }

                Branch destinationBranch = repo.Branches[destinationBranchName];
                if (destinationBranch == null)
                {
                    errorMessage = $"Destination branch {destinationBranchName} does not exist.";
                    Logger.LogError("Destination branch {destinationBranchName} does not exist. Sending error response.", destinationBranchName);
                    return false;
                }

                Commands.Fetch(repo, sourceBranch.RemoteName, [], new FetchOptions(), null);
                Commands.Checkout(repo, destinationBranch);
                Signature author = new(fullName, email, DateTimeOffset.Now);
                MergeOptions options = new()
                {
                    CommitOnSuccess = true,
                    FailOnConflict = true,
                    FastForwardStrategy = FastForwardStrategy.Default,
                    MergeFileFavor = MergeFileFavor.Normal,
                    FileConflictStrategy = CheckoutFileConflictStrategy.Normal,
                };

                MergeResult mergeResult = repo.Merge(sourceBranch, author, options);
                if (mergeResult.Status == MergeStatus.Conflicts)
                {
                    errorMessage = $"Merge failed with status {mergeResult.Status}. May: resolve merge conflicts, abort merge, or reset and then redo merge.";
                    Logger.LogError("Merge of branch {sourceBranchName} into {destinationBranchName} failed with status {status}.", sourceBranchName, destinationBranchName, mergeResult.Status);
                    return false;
                }

                string username = repo.Config.Get<string>("user.name")?.Value ?? "chasma-bot";
                PushOptions pushOptions = new()
                {
                    CredentialsProvider = (url, usernameFromUrl, types) =>
                        new UsernamePasswordCredentials
                        {
                            Username = username,
                            Password = token,
                        }
                };

                repo.Network.Push(destinationBranch, pushOptions);
                return true;
            }
            catch (Exception e)
            {
                errorMessage = $"Failed to merge branch {sourceBranchName} into {destinationBranchName} for repository at {workingDirectory}. Check server logs for more information.";
                Logger.LogError(e, errorMessage);
                return false;
            }
        }

        /// <summary>
        /// Tries to merge the source branch into the destination branch manually by executing git commands in the shell.
        /// </summary>
        /// <remarks>This is used as a fallback when automatic merging fails, which can happen for various reasons such as merge conflicts or issues with the Git library.</remarks>
        /// <param name="workingDirectory">The working directory.</param>
        /// <param name="destinationBranch">The branch to merge changes in.</param>
        /// <param name="sourceBranch">The changes to merge changes from.</param>
        /// <param name="errorMessage">The error message.</param>
        /// <returns>True if the branch is merged; false otherwise.</returns>
        private bool TryMergeBranchManually(string workingDirectory, string destinationBranch, string sourceBranch, out string errorMessage)
        {
            if (!ShellUtility.TryExecuteShellCommand("git fetch", workingDirectory, out errorMessage))
            {
                Logger.LogError(errorMessage);
                return false;
            }

            if (!ShellUtility.TryExecuteShellCommand($"git checkout {destinationBranch}", workingDirectory, out errorMessage))
            {
                Logger.LogError(errorMessage);
                return false;
            }

            if (!ShellUtility.TryExecuteShellCommand($"git merge origin/{sourceBranch}", workingDirectory, out errorMessage))
            {
                Logger.LogError(errorMessage);
                return false;
            }

            if (!ShellUtility.TryExecuteShellCommand("git push", workingDirectory, out errorMessage))
            {
                Logger.LogError(errorMessage);
                return false;
            }

            return true;
        }

        /// <summary>
        /// Tries to add a branch with the specified name to the repository at the specified working directory, and pushes it to the remote.
        /// </summary>
        /// <param name="workingDirectory">The working directory.</param>
        /// <param name="branchName">The branch to be created.</param>
        /// <param name="username">The user name of the user creating branch.</param>
        /// <param name="token">The GitHub API token.</param>
        /// <param name="errorMessage">The error message.</param>
        /// <returns>True if the branch is added; false otherwise.</returns>
        private bool TryAddBranchAutomatically(string workingDirectory, string branchName, string username, string token, out string errorMessage)
        {
            errorMessage = string.Empty;
            Repository repository = null;
            bool branchCreationFailed = false;
            try
            {
                repository = new(workingDirectory);
                if (repository.Branches.Any(i => i.FriendlyName == branchName || i.CanonicalName == branchName || i.UpstreamBranchCanonicalName == branchName))
                {
                    errorMessage = $"Branch with name {branchName} already exists in the repository.";
                    Logger.LogError("Failed to create branch with name {branchName} because it already exists in the repository at {repoPath}. Sending error response.", branchName, workingDirectory);
                    return false;
                }

                Branch newBranch = repository.CreateBranch(branchName);
                if (newBranch == null)
                {
                    errorMessage = $"Failed to create branch {branchName}. Review server logs for more information.";
                    Logger.LogError("Failed to create branch with name {branchName} in repository at {repoPath} for an unknown reason. Sending error response.", branchName, workingDirectory);
                    return false;
                }

                LibGit2Sharp.Remote remoteOrigin = repository.Network.Remotes.FirstOrDefault(i => i.Name == "origin");
                if (remoteOrigin == null)
                {
                    errorMessage = $"Failed to find remote origin for the repository, so the new branch {branchName} cannot be pushed to the remote. Review server logs for more information.";
                    Logger.LogError("Failed to find remote origin for repository at {repoPath}, so the new branch {branchName} cannot be pushed to the remote. Sending error response.", workingDirectory, branchName);
                    return false;
                }

                repository.Branches.Update(newBranch, b => b.Remote = remoteOrigin.Name, b => b.UpstreamBranch = newBranch.CanonicalName);
                PushOptions pushOptions = new()
                {
                    CredentialsProvider = (url, usernameFromUrl, types) =>
                        new UsernamePasswordCredentials
                        {
                            Username = username,
                            Password = token
                        }
                };
                repository.Network.Push(newBranch, pushOptions);
                Logger.LogInformation("Successfully created branch {branchName} in repository at {repoPath}.", branchName, workingDirectory);
                return true;
            }
            catch (Exception e)
            {
                branchCreationFailed = true;
                errorMessage = $"An error occurred while creating {branchName}. Check server logs for more information.";
                Logger.LogError("An error occurred while creating a branch in the repository at {repoPath}: {error}. Sending error response.", workingDirectory, e);
                return false;
            }
            finally
            {
                // If branch creation failed after the branch was created but before it was pushed to the remote,
                // we attempt to clean up by deleting the branch that was created locally.
                if (branchCreationFailed)
                {
                    repository?.Branches.Remove(branchName);
                    repository?.Dispose();
                }
            }
        }

        /// <summary>
        /// Tries to add a branch with the specified name to the repository at the specified working directory by executing git commands in a shell, and pushes it to the remote.
        /// </summary>
        /// <param name="workingDirectory">The working directory.</param>
        /// <param name="branchName">The name of the branch to add.</param>
        /// <param name="errorMessage">The error message.</param>
        /// <returns>True if the branch is added; false otherwise.</returns>
        private bool TryAddBranchManually(string workingDirectory, string branchName, out string errorMessage)
        {
            errorMessage = string.Empty;
            Process process = new();
            try
            {
                string command = $"git checkout -b {branchName}";
                process = ShellUtility.GetStandardShell(command, workingDirectory);
                process.Start();
                string output = process.StandardOutput.ReadToEnd();
                string error = process.StandardError.ReadToEnd();
                process.WaitForExit();
                if (process.ExitCode != 0)
                {
                    errorMessage = $"Failed to create branch {branchName} due to the following error: {error}";
                    Logger.LogError("Failed to create branch with name {branchName} in repository at {repoPath} due to the following error: {error}. Sending error response.", branchName, workingDirectory, error);
                    return false;
                }

                command = $"git push --set-upstream origin {branchName}";
                process = ShellUtility.GetStandardShell(command, workingDirectory);
                process.Start();
                output = process.StandardOutput.ReadToEnd();
                error = process.StandardError.ReadToEnd();
                process.WaitForExit();
                if (process.ExitCode != 0)
                {
                    errorMessage = $"Failed to push branch changes for {branchName} due to the following error: {error}";
                    Logger.LogError("Failed to push branch changes with name {branchName} in repository at {repoPath} due to the following error: {error}. Sending error response.", branchName, workingDirectory, error);
                    return false;
                }

                Logger.LogInformation("Successfully created branch {branchName} in repository at {repoPath}.", branchName, workingDirectory);
                return true;
            }
            catch (Exception e)
            {
                errorMessage = $"An error occurred while creating {branchName}. Check server logs for more information.";
                Logger.LogError("An error occurred while creating a branch in the repository at {repoPath}: {error}. Sending error response.", workingDirectory, e);
                return false;
            }
            finally
            {
                process?.Close();
                process?.Dispose();
            }
        }

        /// <summary>
        /// Stops remote tracking pull requests for the specified branch.
        /// </summary>
        /// <param name="workingDirectory">The working directory of the repository.</param>
        /// <param name="branchName">The name of the branch to stop tracking pull requests for.</param>
        private void StopTrackingRemotePullRequests(string workingDirectory, string branchName)
        {
            if (!CacheManager.Repositories.TryGetValue(workingDirectory, out LocalGitRepository repository))
            {
                Logger.LogWarning("Could not find a repository with the identifier {directory} so {branch} pull request tracking could not be processed.", workingDirectory, branchName);
                return;
            }

            if (repository.HostPlatform == RemoteHostPlatform.GitHub)
            {
                List<long> pullRequestNumbersToDelete = CacheManager.GitHubPullRequests.Values
                    .Where(i => i.BranchName == branchName)
                    .Select(i => i.Number)
                    .ToList();
                foreach (long prNumber in pullRequestNumbersToDelete)
                {
                    CacheManager.GitHubPullRequests.TryRemove(prNumber, out _);
                }
            }
            else if (repository.HostPlatform == RemoteHostPlatform.GitLab)
            {
                List<long> mergeRequestIids = CacheManager.GitLabMergeRequests.Values
                    .Where(i => i.BranchName == branchName)
                    .Select(i => i.Number)
                    .ToList();
                foreach (long iid in mergeRequestIids)
                {
                    CacheManager.GitLabMergeRequests.TryRemove(iid, out _);
                }
            }
            else if (repository.HostPlatform == RemoteHostPlatform.Bitbucket)
            {

            }
            else
            {
                Logger.LogWarning("Could not stop tracking pull requests for {branch} because the remote host platform {platform} is not supported.", branchName, repository.HostPlatform);
            }
        }

        #endregion
    }
}
