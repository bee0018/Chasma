namespace ChasmaWebApi.Data.Responses
{
    /// <summary>
    /// Class representing the details of the GitHub issue.
    /// </summary>
    public class CreateGitHubIssueResponse : ResponseBase
    {
        /// <summary>
        /// Gets or sets the issue identifier.
        /// </summary>
        public int IssueId { get; set; }

        /// <summary>
        /// Gets or sets the issue URL.
        /// </summary>
        public string IssueUrl { get; set; }
    }
}
