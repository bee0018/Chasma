using ChasmaWebApi.Util;

namespace ChasmaWebApi.Data.Requests.Status
{
    /// <summary>
    /// Class representing a Git merge request.
    /// </summary>
    public class GitMergeRequest : ChasmaXmlBase
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
        /// Gets or sets the source branch name.
        /// </summary>
        public string SourceBranch { get; set; }

        /// <summary>
        /// Gets or sets the destination branch name.
        /// </summary>
        public string DestinationBranch { get; set; }
    }
}
