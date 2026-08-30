using ChasmaWebApi.Data.Objects.Git;
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
        /// <param name="outline">The issue outline.</param>
        /// <param name="createdIssue">The created issue result.</param>
        /// <param name="errorMessage">The error message.</param>
        /// <returns>True if the issue is created; false otherwise.</returns>
        bool TryCreateIssue(IssueOutline outline, out RemoteIssueResult createdIssue, out string errorMessage);

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

        /// <summary>
        /// Tries to get the labels for the specified repository.
        /// </summary>
        /// <param name="repository">The local Git repository.</param>
        /// <param name="labels">The list of label names.</param>
        /// <param name="errorMessage">The error message if there was a failure to retrieve labels.</param>
        /// <returns>True if the labels were retrieved; false otherwise.</returns>
        bool TryGetLabelsForRepository(LocalGitRepository repository, out List<string> labels, out string errorMessage);

        /// <summary>
        /// Tries to get the users that have access to the specified project.
        /// </summary>
        /// <param name="repository">The repository to get members for.</param>
        /// <param name="projectMembers">The members of the repository.</param>
        /// <param name="errorMessage">The error message.</param>
        /// <returns>True if the members were retrieved; false otherwise.</returns>
        bool TryGetUsersInProject(LocalGitRepository repository, out List<RemoteProjectMember> projectMembers, out string errorMessage);
    }
}
