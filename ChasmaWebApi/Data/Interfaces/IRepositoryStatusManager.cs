using ChasmaWebApi.Data.Objects;

namespace ChasmaWebApi.Data.Interfaces
{
    /// <summary>
    /// Interface containing the members on the repository status manager.
    /// </summary>
    public interface IRepositoryStatusManager
    {
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

        /// <summary>
        /// Gets the status of the specified repository.
        /// </summary>
        /// <param name="repoKey">The repository identifier.</param>
        /// <returns>A repository summary of running the command 'git status'.</returns>
        RepositorySummary? GetRepositoryStatus(string repoKey);

        /// <summary>
        /// Stages or unstages the file for the specified repository.
        /// </summary>
        /// <param name="repoKey">The repository key.</param>
        /// <param name="fileName">The name of the file to stage.</param>
        /// <param name="isStaging">Flag indicating whether the file is being staged.</param>
        /// <returns>A list of the updated file statuses after staging or unstaging.</returns>
        List<RepositoryStatusElement>? ApplyStagingAction(string repoKey, string fileName, bool isStaging);

        /// <summary>
        /// Commits the staged changes for the specified repository.
        /// </summary>
        /// <param name="filePath">The working directory of the repository.</param>
        /// <param name="fullName">The user's full name.</param>
        /// <param name="email">The user's email.</param>
        /// <param name="commitMessage">The message description of the commit.</param>
        void CommitChanges(string filePath, string fullName, string email, string commitMessage);

        /// <summary>
        /// Tries to push the committed changes to the remote repository.
        /// </summary>
        /// <param name="filePath">The filepath to the specified repository.</param>
        /// <param name="token">The git API token.</param>
        /// <param name="errorMessage">The error message.</param>
        /// <returns>True if the user was able to push changes; false otherwise.</returns>
        bool TryPushChanges(string filePath, string token, out string errorMessage);

        /// <summary>
        /// Tries to pull changes from the remote repository.
        /// </summary>
        /// <param name="workingDirectory">The working directory of the repository.</param>
        /// <param name="fullName">The user's full name.</param>
        /// <param name="email">The user's email.</param>
        /// <param name="token">The git API token.</param>
        /// <param name="errorMessage">The error message.</param>
        /// <returns>True if the user was able to pull changes, false otherwise.</returns>
        bool TryPullChanges(string workingDirectory, string fullName, string email, string token, out string errorMessage);

        /// <summary>
        /// Tries to checkout the specified branch in the repository.
        /// </summary>
        /// <param name="workingDirectory">The working directory of the repository.</param>
        /// <param name="branchName">The branch to checkout.</param>
        /// <param name="errorMessage">The error message if there an issue checking out the branch.</param>
        /// <returns>True if the branch is checked out; false otherwise.</returns>
        bool TryCheckoutBranch(string workingDirectory, string branchName, out string errorMessage);

        /// <summary>
        /// Gets the list of all branches in the specified repository.
        /// </summary>
        /// <param name="workingDirectory">The working directory of the repository.</param>
        /// <returns>List of all local and remote branches in the repository.</returns>
        List<string> GetAllBranches(string workingDirectory);

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
        /// Tries to get the Git diff for the specified file in the repository.
        /// </summary>
        /// <param name="workingDirectory">The working directory of file to get the diff of.</param>
        /// <param name="filePath">The path to the file to be diffed.</param>
        /// <param name="diffContent">The content as a result of the git diff operation.</param>
        /// <param name="errorMessage">The error message if an error occurs.</param>
        /// <returns>True if the file was successfully diffed; false otherwise.</returns>
        bool TryGetGitDiff(string workingDirectory, string filePath, out string diffContent, out string errorMessage);
    }
}
