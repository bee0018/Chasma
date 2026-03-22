namespace ChasmaWebApi.Data.Objects.Remote
{
    /// <summary>
    /// Class representing the GitLab issue that was newly created.
    /// </summary>
    public class GitLabIssueResult
    {
        /// <summary>
        /// Gets or sets the issue identifier.
        /// </summary>
        public long IssueId { get; set; }

        /// <summary>
        /// Gets or sets the URL of the newly created issue.
        /// </summary>
        public string Url { get; set; }
    }
}
