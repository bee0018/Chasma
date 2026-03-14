using ChasmaWebApi.Core.Interfaces.Infrastructure;
using ChasmaWebApi.Core.Interfaces.Remote;
using ChasmaWebApi.Data.Objects.Remote;
using LibGit2Sharp;
using Octokit;
using Branch = LibGit2Sharp.Branch;
using Credentials = Octokit.Credentials;
using Repository = LibGit2Sharp.Repository;

namespace ChasmaWebApi.Core.Services.Remote
{
    /// <summary>
    /// Service class containing the implementation of the members on the GitHub service, which is responsible for handling GitHub-level operations such as fetching repositories from a user's GitHub account.
    /// </summary>
    public class GitHubService : IGitHubService
    {
        /// <summary>
        /// The logger instance for logging diagnostic and operational information within class.
        /// </summary>
        private readonly ILogger<GitHubService> Logger;

        /// <summary>
        /// The cache manager instance for managing cached data across the application, such as cached pull request information to minimize redundant API calls to GitHub.
        /// </summary>
        private readonly ICacheManager CacheManager;

        /// <summary>
        /// Gets or sets the GitHub client instance for making API calls to GitHub. This client is initialized with the appropriate credentials when making requests to create pull requests or issues on GitHub.
        /// </summary>
        private GitHubClient Client { get; set; }

        #region Constructor

        /// <summary>
        /// Initializes a new instance of the <see cref="GitHubService"/> class.
        /// </summary>
        /// <param name="logger">The internal logger instance.</param>
        /// <param name="cacheManager">The internal API cache manager.</param>
        public GitHubService(ILogger<GitHubService> logger, ICacheManager cacheManager)
        {
            Logger = logger;
            CacheManager = cacheManager;
        }

        #endregion

        // <inheritdoc />
        public bool TryCreatePullRequest(string workingDirectory, string owner, string repoName, string title, string headBranch, string baseBranch, string body, string token, out int pullRequestId, out string prUrl, out string timestamp, out string errorMessage)
        {
            errorMessage = string.Empty;
            prUrl = string.Empty;
            timestamp = string.Empty;
            pullRequestId = -1;

            if (!PullRequestCanBeCreated(workingDirectory, headBranch, baseBranch, out string reason))
            {
                errorMessage = reason;
                Logger.LogError("Cannot create pull request in {repoName}: {reason}", repoName, reason);
                return false;
            }

            Client = new GitHubClient(new ProductHeaderValue(repoName))
            {
                Credentials = new Credentials(token)
            };

            NewPullRequest newPullRequest = new(title, headBranch, baseBranch)
            {
                Body = body
            };

            Task<PullRequest?> createPrTask = SendPrRequest(Client, owner, repoName, newPullRequest);
            PullRequest? createdPullRequest = createPrTask.Result;
            if (createdPullRequest == null)
            {
                errorMessage = $"Failed to create pull request in {repoName}. Check server logs for more information.";
                return false;
            }

            pullRequestId = createdPullRequest.Number;
            prUrl = createdPullRequest.HtmlUrl;
            timestamp = createdPullRequest.CreatedAt.ToLocalTime().ToString("g");
            GitHubPullRequest pr = new()
            {
                Number = createdPullRequest.Number,
                RepositoryName = repoName,
                RepositoryOwner = owner,
                BranchName = createdPullRequest.Head.Ref,
                ActiveState = createdPullRequest.State.StringValue,
                MergeableState = createdPullRequest.MergeableState.HasValue ? createdPullRequest.MergeableState.Value.StringValue : MergeableState.Unknown.ToString(),
                CreatedAt = timestamp,
                MergedAt = null,
                Merged = false,
                HtmlUrl = createdPullRequest.HtmlUrl
            };
            CacheManager.GitHubPullRequests.TryAdd(pr.Number, pr);
            Logger.LogInformation("Created pull request {prId} in {repoName}.", pullRequestId, repoName);
            return true;
        }

        // <inheritdoc />
        public bool TryCreateIssue(string repoName, string repoOwner, string title, string body, string token, out int issueId, out string issueUrl, out string errorMessage)
        {
            errorMessage = string.Empty;
            issueUrl = string.Empty;
            issueId = -1;
            try
            {
                NewIssue newIssue = new(title) { Body = body };
                Client = new GitHubClient(new ProductHeaderValue(repoName)) { Credentials = new Credentials(token) };
                Task<Issue?> createIssueTask = SendCreateIssueRequest(Client, repoOwner, repoName, newIssue);
                Issue? issue = createIssueTask.Result;
                if (issue == null)
                {
                    errorMessage = $"Failed to create issue in {repoName}. Check server logs for more information.";
                    return false;
                }

                issueId = issue.Number;
                issueUrl = issue.HtmlUrl;
                return true;
            }
            catch (Exception ex)
            {
                errorMessage = $"Failed to create issue for {repoName} with title {title} due to an exception. Review server logs.";
                Logger.LogError(ex, errorMessage);
                return false;
            }
        }

        // <inheritdoc/>
        public bool TryGetWorkflowRunResults(string repoName, string repoOwner, string token, int buildCount, out List<WorkflowRunResult> workflowRunResults, out string errorMessage)
        {
            errorMessage = string.Empty;
            workflowRunResults = new();
            ProductHeaderValue productHeader = new(repoName);
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

            Logger.LogInformation("Retrieved {count} build runs from {repo}.", runs.Count, repoName);
            return true;
        }

        #region Private Methods

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
                Logger.LogError("Error when trying to retrieve workflow runs for {repoName}: {error}", repoName, e);
                return null;
            }
        }

        /// <summary>
        /// Sends a pull request creation request to the GitHub API.
        /// </summary>
        /// <param name="client">The Ocktokit GitHub API client.</param>
        /// <param name="owner">The repository owner.</param>
        /// <param name="repoName">The repository owner.</param>
        /// <param name="newPullRequest">The new pull request components.</param>
        /// <returns>Task containing the result of the API operation.</returns>
        private async Task<PullRequest?> SendPrRequest(GitHubClient client, string owner, string repoName, NewPullRequest newPullRequest)
        {
            try
            {
                return await client.PullRequest.Create(owner, repoName, newPullRequest);
            }
            catch (Exception e)
            {
                Logger.LogError("Error when trying to create pull request in {repoName}: {error}", repoName, e);
                return null;
            }
        }

        /// <summary>
        /// Sends a request to create the issue to the GitHub API.
        /// </summary>
        /// <param name="client">The Ocktokit GitHub API client.</param>
        /// <param name="owner">The repository owner.</param>
        /// <param name="repoName">The repository owner.</param>
        /// <param name="issue">The new issue components.</param>
        /// <returns>Task containing the result of the API operation.</returns>
        private async Task<Issue?> SendCreateIssueRequest(GitHubClient client, string owner, string repoName, NewIssue issue)
        {
            try
            {
                return await client.Issue.Create(owner, repoName, issue);
            }
            catch (Exception e)
            {
                Logger.LogError("Error when trying to create issue in {repoName}: {error}", repoName, e);
                return null;
            }
        }

        /// <summary>
        /// Determines if a pull request can be created based on the branch status.
        /// </summary>
        /// <param name="workingDirectory">The current working directory of the repository.</param>
        /// <param name="headBranchName">The branch that is to be merged.</param>
        /// <param name="baseBranchName">The branch that will have changes merged into it.</param>
        /// <param name="reason">The reason the branch cannot have a pull request created.</param>
        /// <returns>True if the pull request can be created; false otherwise.</returns>
        private static bool PullRequestCanBeCreated(string workingDirectory, string headBranchName, string baseBranchName, out string reason)
        {
            reason = string.Empty;
            using Repository repo = new(workingDirectory);
            Branch headBranch = repo.Branches[headBranchName];
            if (headBranch == null)
            {
                reason = $"Head branch {headBranchName} does not exist.";
                return false;
            }

            Branch baseBranch = repo.Branches[baseBranchName];
            if (baseBranch == null)
            {
                reason = $"Base branch {baseBranchName} does not exist.";
                return false;
            }

            if (headBranch.FriendlyName == baseBranch.FriendlyName)
            {
                reason = "Cannot create a pull request to merge a branch into itself.";
                return false;
            }

            HistoryDivergence divergence = repo.ObjectDatabase.CalculateHistoryDivergence(headBranch.Tip, baseBranch.Tip);
            if (divergence.AheadBy == 0)
            {
                reason = $"Head branch {headBranchName} has no new commits to merge into {baseBranchName}.";
                return false;
            }

            return true;
        }

        #endregion
    }
}
