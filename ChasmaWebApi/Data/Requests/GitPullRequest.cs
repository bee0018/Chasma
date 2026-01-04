using ChasmaWebApi.Util;

namespace ChasmaWebApi.Data.Requests
{
    /// <summary>
    /// Class representing the git function to pull latest changes.
    /// </summary>
    public class GitPullRequest : ChasmaXmlBase
    {
        /// <summary>
        /// Gets or sets the repository identifier.
        /// </summary>
        public string RepositoryId { get; set; }

        /// <summary>
        /// Gets or sets the user identifier.
        /// </summary>
        public int UserId { get; set; }

        /// <summary>
        /// Gets or sets the user's email.
        /// </summary>
        public string Email { get; set; }
    }
}
