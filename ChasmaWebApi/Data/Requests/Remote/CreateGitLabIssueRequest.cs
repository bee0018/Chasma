namespace ChasmaWebApi.Data.Requests.Remote
{
    /// <summary>
    /// Class representing a request to create a GitLab issue.
    /// </summary>
    public class CreateGitLabIssueRequest
    {
        /// <summary>
        /// Gets or sets the repository identifier.
        /// </summary>
        public string RepositoryId { get; set; }

        public string IssueType { get; set; }
    }
}
