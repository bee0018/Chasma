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
        /// <returns>A list of the elements as a result of running the command 'git status'.</returns>
        List<RepositoryStatusElement>? GetRepositoryStatus(string repoKey);

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
    }
}
