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
        /// <param name="repository">The repository in cache.</param>
        /// <param name="buildResults">The build results.</param>
        /// <param name="errorMessage">The error message.</param>
        /// <returns>True if the pipeline jobs retrieval was successful; false otherwise.</returns>
        public bool TryGetPipelineJobResults(LocalGitRepository repository, out List<WorkflowRunResult> buildResults, out string errorMessage);

        /// <summary>
        /// Tries to create a GitLab issue for the specified repository.
        /// </summary>
        /// <param name="issueCreation">The issue creation details.</param>
        /// <param name="issue">The newly created repository issue.</param>
        /// <param name="errorMessage">The error message.</param>
        /// <returns>True if the GitLab issue was successfully created; false otherwise.</returns>
        public bool TryCreateIssue(PreparedGitLabIssue issueCreation, out GitLabIssueResult issue, out string errorMessage);

        /// <summary>
        /// Tries to get the users that have access to the specified project.
        /// </summary>
        /// <param name="repository">The repository to get members for.</param>
        /// <param name="projectMembers">The members of the repository.</param>
        /// <param name="projectId">The project identifier that the members belong to.</param>
        /// <param name="errorMessage">The error message.</param>
        /// <returns>True if the members were retrieved; false otherwise.</returns>
        bool TryGetUsersInProject(LocalGitRepository repository, out List<GitLabProjectMember> projectMembers, out long projectId, out string errorMessage);

        /// <summary>
        /// Tries to create a merge request in the specified repository.
        /// </summary>
        /// <param name="preparedMergeRequest">The merge request prepared outline.</param>
        /// <param name="mergeResult">The newly created merge request result.</param>
        /// <param name="errorMessage">The error message.</param>
        /// <returns>True if the merge request was created; false otherwise.</returns>
        bool TryCreateMergeRequest(PreparedGitLabMergeRequest preparedMergeRequest, out MergeRequestResult mergeResult, out string errorMessage);
    }
}
