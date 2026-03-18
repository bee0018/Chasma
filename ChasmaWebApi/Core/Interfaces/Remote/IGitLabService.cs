using ChasmaWebApi.Data.Objects.Git;
using ChasmaWebApi.Data.Objects.Remote;

namespace ChasmaWebApi.Core.Interfaces.Remote
{
    /// <summary>
    /// Interface representing the members and functionality of the GitLab service.
    /// </summary>
    public interface IGitLabService
    {
        /// <summary>
        /// Tries to get the pipeline build results.
        /// </summary>
        /// <param name="workingDirectory">The working directory of the repository.</param>
        /// <param name="cachedRepo">The repository in cache.</param>
        /// <param name="buildResults">The build results.</param>
        /// <param name="errorMessage">The error message.</param>
        /// <returns>True if the pipeline jobs retrieval was successful; false otherwise.</returns>
        public bool TryGetPipelineJobResults(string workingDirectory, LocalGitRepository cachedRepo, out List<WorkflowRunResult> buildResults, out string errorMessage);

        /// <summary>
        /// Tries to create a GitLab issue for the specified repository.
        /// </summary>
        /// <param name="issueCreation">The issue creation details.</param>
        /// <param name="issue">The newly created repository issue.</param>
        /// <param name="errorMessage">The error message.</param>
        /// <returns>True if the GitLab issue was successfully created; false otherwise.</returns>
        public bool TryCreateIssue(GitLabIssueCreation issueCreation, out GitLabIssueResult issue, out string errorMessage);
    }
}
