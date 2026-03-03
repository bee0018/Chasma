using ChasmaWebApi.Data.Objects;

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
        /// <param name="workingDirectory">The working directory of the repository.</param>
        /// <param name="owner">The repository owner.</param>
        /// <param name="repoName">The repository name.</param>
        /// <param name="title">The name of the pull request.</param>
        /// <param name="headBranch">The working branch to be merged into another.</param>
        /// <param name="baseBranch">The branch that will be getting a branch merged into.</param>
        /// <param name="body">The message of the pull request.</param>
        /// <param name="token">The GitHub API token.</param>
        /// <param name="pullRequestId">The identtifier of the pull request.</param>
        /// <param name="prUrl">The pull request url.</param>
        /// <param name="timestamp">The timestamp of when the pull request was created.</param>
        /// <param name="errorMessage">The error message.</param>
        /// <returns>True if the pull request is created; false otherwise.</returns>
        bool TryCreatePullRequest(string workingDirectory, string owner, string repoName, string title, string headBranch, string baseBranch, string body, string token, out int pullRequestId, out string prUrl, out string timestamp, out string errorMessage);

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
        /// <param name="buildCount">The threshold representing the max number of runs to retrieve.</param>
        /// <param name="workflowRunResults">The list of workflow run results.</param>
        /// <param name="errorMessage">The error message if there was a failure to retrieve runs.</param>
        /// <returns>True if the workflow runs were retrieved; false otherwise.</returns>
        bool TryGetWorkflowRunResults(string repoName, string repoOwner, string token, int buildCount,
            out List<WorkflowRunResult> workflowRunResults, out string errorMessage);
    }
}
