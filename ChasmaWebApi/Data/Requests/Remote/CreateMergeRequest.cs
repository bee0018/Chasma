using ChasmaWebApi.Data.Objects.Remote;

namespace ChasmaWebApi.Data.Requests.Remote
{
    /// <summary>
    /// Class representing the details needed to create a GitLab merge request.
    /// </summary>
    public class CreateMergeRequest
    {
        public string RepositoryId { get; set; }

        /// <summary>
        /// Gets or sets the base branch.
        /// </summary>
        public string SourceBranch { get; set; }

        /// <summary>
        /// Gets or sets the branch to merge changes into.
        /// </summary>
        public string TargetBranch { get; set; }

        /// <summary>
        /// Gets or sets the title of the merge request.
        /// </summary>
        public string Title { get; set; }

        /// <summary>
        /// Gets or sets the target project identifier.
        /// </summary>
        public long? TargetProjectId { get; set; }

        /// <summary>
        /// Gets or sets the assignee of the merge request.
        /// </summary>
        public GitLabProjectMember Assignee { get; set; }

        /// <summary>
        /// Gets or sets the additional assignees of the merge request.
        /// </summary>
        public List<GitLabProjectMember> AdditionalAssignees { get; set; } = new();

        /// <summary>
        /// Gets or sets the reviewers of the merge request.
        /// </summary>
        public List<GitLabProjectMember> Reviewers { get; set; } = new();

        /// <summary>
        /// Gets or sets the description of the merge request.
        /// </summary>
        public string Description { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether to remove the source branch once merged.
        /// </summary>
        public bool RemoveSourceBranch { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether to squash commits when the branch is merged.
        /// </summary>
        public bool Squash { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether to allow collaboration.
        /// </summary>
        public bool? AllowCollaboration { get; set; }
    }
}
