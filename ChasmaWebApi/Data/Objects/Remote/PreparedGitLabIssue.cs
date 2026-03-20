namespace ChasmaWebApi.Data.Objects.Remote
{
    /// <summary>
    /// Class representing the issue creation details.
    /// Note: This will be what the user will fill out and then use this data to be used by the GitLab API.
    /// </summary>
    public class PreparedGitLabIssue
    {
        /// <summary>
        /// Gets or sets the repository owner.
        /// </summary>
        public string RepoOwner { get; set; }

        /// <summary>
        /// Gets or sets the repository name.
        /// </summary>
        public string RepoName { get; set; }

        /// <summary>
        /// Gets or sets the main assignee of an issue.
        /// </summary>
        public GitLabProjectMember MainAssignee { get; set; }

        /// <summary>
        /// Gets or sets the contacts of an issue.
        /// </summary>
        public List<GitLabProjectMember> Contacts { get; set; } = new();

        /// <summary>
        /// Gets or sets the title of the issue.
        /// Note: This is required.
        /// </summary>
        public string Title { get; set; }

        /// <summary>
        /// Gets or sets the description of the issue.
        /// </summary>
        public string Description { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether the issue is confidential.
        /// </summary>
        public bool Confidential { get; set; }
    }
}
