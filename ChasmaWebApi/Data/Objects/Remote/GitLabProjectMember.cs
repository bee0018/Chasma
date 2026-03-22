namespace ChasmaWebApi.Data.Objects.Remote
{
    /// <summary>
    /// Class representing a member of a GitLab project.
    /// </summary>
    public class GitLabProjectMember
    {
        /// <summary>
        /// Gets or sets the member identifier.
        /// </summary>
        public long AssigneeId { get; set; }

        /// <summary>
        /// Gets or sets the member's GitLab user name.
        /// </summary>
        public string UserName { get; set; }

        /// <summary>
        /// Gets or sets the user's full name on GitLab.
        /// </summary>
        public string FullName { get; set; }
    }
}
