using ChasmaWebApi.Util;

namespace ChasmaWebApi.Data.Requests.Configuration
{
    /// <summary>
    /// Class representing the request to ignore a repository.
    /// </summary>
    public class IgnoreRepositoryRequest : ChasmaXmlBase
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
        /// Gets or sets a value indicating whether the user is wanting to include/ignore the repository.
        /// </summary>
        public bool IsIgnored { get; set; }
    }
}
