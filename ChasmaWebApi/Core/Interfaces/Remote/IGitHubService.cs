using ChasmaWebApi.Data.Objects.Remote;

namespace ChasmaWebApi.Core.Interfaces.Remote
{
    /// <summary>
    /// Interface containing the members on the GitHub service, which is responsible for handling GitHub-level operations such as fetching repositories from a user's GitHub account.
    /// </summary>
    public interface IGitHubService
    {
        /// <summary>
        /// Tries to create a pull request in the specified repository.
        /// </summary>
        /// <param name="pullRequest">The pull request to be created.</param>
        /// <param name="pullRequestId">The identtifier of the pull request.</param>
        /// <param name="prUrl">The pull request url.</param>
        /// <param name="timestamp">The timestamp of when the pull request was created.</param>
        /// <param name="errorMessage">The error message.</param>
        /// <returns>True if the pull request is created; false otherwise.</returns>
        bool TryCreatePullRequest(PreparedGitHubPullRequest pullRequest, out int pullRequestId, out string prUrl, out string timestamp, out string errorMessage);

        /// <summary>
        /// Tries to create a GitHub issue in the specified repository.
        /// </summary>
        /// <param name="repoName">The repository name.</param>
        /// <param name="repoOwner">The repository owner.</param>
        /// <param name="title">The issue title.</param>
        /// <param name="body">The issue body description.</param>
        /// <param name="token">The GitHub API token.</param>
        /// <param name="issueId">The issue identifier.</param>
        /// <param name="issueUrl">The issue URL.</param>
        /// <param name="errorMessage">The error message.</param>
        /// <returns>True if the issue is created; false otherwise.</returns>
        bool TryCreateIssue(string repoName, string repoOwner, string title, string body, string token, out int issueId, out string issueUrl, out string errorMessage);

        /// <summary>
        /// Tries to get the workflow run results for the repo with specified details.
        /// </summary>
        /// <param name="repoName">The repository name.</param>
        /// <param name="repoOwner">The repository owner.</param>
        /// <param name="token">The repository access token.</param>
        /// <param name="workflowRunResults">The list of workflow run results.</param>
        /// <param name="errorMessage">The error message if there was a failure to retrieve runs.</param>
        /// <returns>True if the workflow runs were retrieved; false otherwise.</returns>
        bool TryGetWorkflowRunResults(string repoName, string repoOwner, string token, out List<WorkflowRunResult> workflowRunResults, out string errorMessage);
    }
}
