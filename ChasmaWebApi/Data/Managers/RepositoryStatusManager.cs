using ChasmaWebApi.Data.Interfaces;
using ChasmaWebApi.Data.Objects;
using LibGit2Sharp;
using Octokit;
using Branch = LibGit2Sharp.Branch;
using Credentials = Octokit.Credentials;
using Repository = LibGit2Sharp.Repository;
using Signature = LibGit2Sharp.Signature;

namespace ChasmaWebApi.Data.Managers
{
    /// <summary>
    /// Class representing the manager for processing repository status data.
    /// </summary>
    /// <param name="logger">The internal server logger.</param>
    /// <param name="cacheManager">The internal API cache manager.</param>
    public class RepositoryStatusManager(ILogger<RepositoryStatusManager> logger, ICacheManager cacheManager)
        : ClientManagerBase<RepositoryStatusManager>(logger, cacheManager), IRepositoryStatusManager
    {
        // <inheritdoc/>
        public bool TryGetWorkflowRunResults(string repoName, string repoOwner, string token, int buildCount, out List<WorkflowRunResult> workflowRunResults, out string errorMessage)
        {
            errorMessage = string.Empty;
            workflowRunResults = new();
            ProductHeaderValue productHeader = new ProductHeaderValue(repoName);
            Client = new(productHeader) { Credentials = new Credentials(token) };
            Task<WorkflowRunsResponse?> workflowRunsResponseTask = GetWorkFlowRuns(Client, repoOwner, repoName);
            WorkflowRunsResponse workFlowRunsResponse = workflowRunsResponseTask.Result;
            if (workFlowRunsResponse == null)
            {
                errorMessage = $"Failed to fetch workflow runs for {repoName}. Check server logs for more information.";
                return false;
            }

            List<WorkflowRun> runs = workFlowRunsResponse.WorkflowRuns.Take(buildCount).ToList();
            foreach (WorkflowRun run in runs)
            {
                WorkflowRunResult buildResult = new()
                {
                    BranchName = run.HeadBranch,
                    RunNumber = run.RunNumber,
                    BuildTrigger = run.Event,
                    CommitMessage = run.HeadCommit.Message,
                    BuildStatus = run.Status.StringValue,
                    BuildConclusion = run.Conclusion.HasValue ? run.Conclusion.Value.ToString() : "Unknown",
                    CreatedDate = run.CreatedAt.ToString("g"),
                    UpdatedDate = run.UpdatedAt.ToString("g"),
                    WorkflowUrl = run.HtmlUrl,
                    AuthorName = run.Actor.Login,
                };
                workflowRunResults.Add(buildResult);
            }

            ClientLogger.LogInformation("Retrieved {count} build runs from {repo}.", runs.Count, repoName);
            return true;
        }

        // <inheritdoc/>
        public RepositorySummary? GetRepositoryStatus(string repoKey)
        {
            if (!CacheManager.WorkingDirectories.TryGetValue(repoKey, out string workingDirectory))
            {
                ClientLogger.LogError("Invalid repository key {repoKey} provided to get repository status.", repoKey);
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

                bool isStaged = IsFileStaged(state);
                RepositoryStatusElement statusElement = new()
                {
                    RepositoryId = repoKey,
                    FilePath = item.FilePath,
                    State = item.State,
                    IsStaged = isStaged,
                };
                statusElements.Add(statusElement);
            }

            ClientLogger.LogInformation("Retrieved repository status for {repoKey} with {count} changes.", repoKey, statusElements.Count);
            (string branchName, int aheadCount, int behindCount) = GetBranchDiversionCalculation(workingDirectory);
            string remoteUrl = GetRemoteUrl(repo.Head, repo.Network.Remotes, workingDirectory) ?? string.Empty;
            string commitHash = GetCommitHash(repo.Head);
            RepositorySummary repositorySummary = new()
            {
                StatusElements = statusElements,
                CommitsAhead = aheadCount,
                CommitsBehind = behindCount,
                BranchName = branchName,
                RemoteUrl = remoteUrl,
                CommitHash = commitHash,
            };
            return repositorySummary;
        }

        // <inheritdoc />
        public List<RepositoryStatusElement>? ApplyStagingAction(string repoKey, string fileName, bool stagingFile)
        {
            List<RepositoryStatusElement> statusElements = new();
            if (!CacheManager.WorkingDirectories.TryGetValue(repoKey, out string workingDirectory))
            {
                ClientLogger.LogError("Invalid repository key {repoKey} provided to stage the file {fileName}.", repoKey, fileName);
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
            
            ClientLogger.LogInformation("{action} file {file}", stagingAction, fileName);
            RepositorySummary summary = GetRepositoryStatus(repoKey);
            return summary?.StatusElements;
        }

        // <inheritdoc />
        public void CommitChanges(string filePath, string fullName, string email, string commitMessage)
        {
            using Repository repo = new(filePath);
            Signature author = new(fullName, email, DateTimeOffset.Now);
            var x = repo.Commit(commitMessage, author, author);

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
                ClientLogger.LogError(errorMessage);
                return false;
            }

            if (repo.Info.IsHeadDetached)
            {
                errorMessage = $"Failed to push changes. The HEAD is in a detached state for repository at {filePath}.";
                ClientLogger.LogError(errorMessage);
                return false;
            }

            if (branch.TrackedBranch == null)
            {
                errorMessage = $"Failed to push changes. No upstream set for branch {branch.FriendlyName}.";
                ClientLogger.LogError(errorMessage);
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
                errorMessage = $"Failed to push changes to remote for branch {branch.FriendlyName}. Check server logs for more information.";
                ClientLogger.LogError(e, errorMessage);
                return false;
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
                errorMessage = $"Failed to pull changes from remote for repository at {workingDirectory}. Check server logs for more information.";
                ClientLogger.LogError(e, errorMessage);
                return false;
            }
        }

        /// <summary>
        /// Gets the workflow run for the specified repository.
        /// </summary>
        /// <param name="client">The GitHub API client.</param>
        /// <param name="repoOwner">The repository owner.</param>
        /// <param name="repoName">The repository name.</param>
        /// <returns>Task containing the workflow run response from the API client.</returns>
        private async Task<WorkflowRunsResponse?> GetWorkFlowRuns(GitHubClient client, string repoOwner, string repoName)
        {
            try
            {
                return await client.Actions.Workflows.Runs.List(repoOwner, repoName);
            }
            catch (Exception e)
            {
                ClientLogger.LogError("Error when trying to retrieve workflow runs for {repoName}: {error}", repoName, e);
                return null;
            }
        }

        /// <summary>
        /// Determines if the file is staged based on its file status.
        /// </summary>
        /// <param name="fileStatus">The current file status.</param>
        /// <returns>True if the file is staged; false otherwise.</returns>
        private static bool IsFileStaged(FileStatus fileStatus)
        {
            string[] statusStrings = fileStatus.ToString().Split(",");
            string[] trimmedStatusStrings = statusStrings.Select(i => i.Trim()).ToArray();
            FileStatus[] fileStatuses = trimmedStatusStrings.Select(Enum.Parse<FileStatus>).ToArray();
            return fileStatuses.Any(IsStateStaged);
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
        /// Gets the branch diversion calculation for the specified repository.
        /// </summary>
        /// <param name="repo">The specified repository working directory.</param>
        /// <returns>The number of local branch name, commits ahead, and behind.</returns>
        private (string branchName, int aheadCount, int behindCount) GetBranchDiversionCalculation(string workingDirectory)
        {
            using Repository repo = new Repository(workingDirectory);
            Branch branch = repo.Head;
            if (branch == null)
            {
                ClientLogger.LogError("Cannot get branch diversion calculation. Failed to get branch information for repository at {path}.", repo.Info.WorkingDirectory);
                return ("", 0, 0);
            }

            if (repo.Info.IsHeadDetached)
            {
                ClientLogger.LogWarning("Cannot get branch diversion calculation. The HEAD is in a detached state for repository at {path}.", repo.Info.WorkingDirectory);
                return ("", 0, 0);
            }

            try
            {
                Commands.Fetch(repo, branch.RemoteName, [], new FetchOptions(), null);
            }
            catch (Exception e)
            {
                ClientLogger.LogWarning(e, "Failed to fetch updates from remote {remote} for repository at {path}.", branch.RemoteName, repo.Info.WorkingDirectory);
            }
            
            string localBranchName = branch.FriendlyName;
            if (string.IsNullOrEmpty(localBranchName))
            {
                ClientLogger.LogError("Cannot get branch diversion calculation. No local branch found for repository at {path} with the branch name {branchName}.", repo.Info.WorkingDirectory, localBranchName);
                return ("", 0, 0);
            }

            if (branch.TrackedBranch == null)
            {
                ClientLogger.LogWarning("Cannot get branch diversion calculation. Could not find the tracked branch for the local branch {branchName}.", localBranchName);
                return (localBranchName, 0, 0);
            }

            string upstreamBranchName = branch.TrackedBranch.FriendlyName;
            Branch localBranch = repo.Branches[localBranchName];
            Branch upstreamBranch = repo.Branches[upstreamBranchName];
            if (localBranch == null)
            {
                ClientLogger.LogError("Cannot get branch diversion calculation. No local branch with name {branchName} found.", localBranchName);
                return (localBranchName, 0, 0);
            }

            if (upstreamBranch == null)
            {
                ClientLogger.LogError("Cannot get branch diversion calculation. No upstream branch with name {branchName} found.", upstreamBranchName);
                return (localBranchName, 0, 0);
            }

            HistoryDivergence divergence = repo.ObjectDatabase.CalculateHistoryDivergence(localBranch.Tip, upstreamBranch.Tip);
            return (localBranchName, divergence.AheadBy ?? 0, divergence.BehindBy ?? 0);
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
            string remoteName = branch.RemoteName;
            Remote? remote = remoteBranches[remoteName];
            if (remote == null)
            {
                ClientLogger.LogWarning("Could not find remote {remoteName} for repository at {path}.", remoteName, workingDirectory);
                return null;
            }

            return remote.PushUrl ?? remote.Url;
        }

        /// <summary>
        /// Gets the commit hash for the specified branch.
        /// </summary>
        /// <param name="branch">The current branch.</param>
        /// <returns>The latest commit hash.</returns>
        private string GetCommitHash(Branch branch)
        {
            if (branch?.Tip == null)
            {
                ClientLogger.LogError("Cannot get commit hash. Failed to get branch information.");
                return string.Empty;
            }

            return branch.Tip.Sha.Length > 7 ? branch.Tip.Sha[..7] : branch.Tip.Sha;
        }
    }
}
