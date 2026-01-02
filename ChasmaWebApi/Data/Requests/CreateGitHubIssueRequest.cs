using ChasmaWebApi.Util;

namespace ChasmaWebApi.Data.Requests
{
    /// <summary>
    /// Class representing the components needed to create a GitHub issue.
    /// </summary>
    public class CreateGitHubIssueRequest : ChasmaXmlBase
    {
        /// <summary>
        /// Gets or sets the repository name.
        /// </summary>
        public string RepositoryName { get; set; }

        /// <summary>
        /// Gets or sets the repository owner.
        /// </summary>
        public string RepositoryOwner { get; set; }

        /// <summary>
        /// Gets or sets the issue title.
        /// </summary>
        public string Title { get; set; }

        /// <summary>
        /// Gets or sets the description of the issue.
        /// </summary>
        public string Body { get; set; }
    }
}
