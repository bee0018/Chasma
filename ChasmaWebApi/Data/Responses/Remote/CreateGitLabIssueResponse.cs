using ChasmaWebApi.Data.Objects.Remote;

namespace ChasmaWebApi.Data.Responses.Remote
{
    /// <summary>
    /// Class representing a response to creating a GitLab issue.
    /// </summary>
    public class CreateGitLabIssueResponse : ResponseBase
    {
        /// <summary>
        /// Gets or sets the newly created issue.
        /// </summary>
        public GitLabIssueResult Issue { get; set; }
    }
}
