using ChasmaWebApi.Data.Interfaces;
using ChasmaWebApi.Data.Objects;
using LibGit2Sharp;
using Octokit;
using Credentials = Octokit.Credentials;
using Repository = LibGit2Sharp.Repository;

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
        public List<RepositoryStatusElement>? GetRepositoryStatus(string repoKey)
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
            return statusElements;
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
            statusElements = GetRepositoryStatus(repoKey);
            return statusElements;
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
    }
}
