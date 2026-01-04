using ChasmaWebApi.Util;

namespace ChasmaWebApi.Data.Requests.Status
{
    /// <summary>
    /// Class representing the components of a Git Status request.
    /// </summary>
    public class GitStatusRequest : ChasmaXmlBase
    {
        /// <summary>
        /// Gets or sets the repository identifier to get the status for.
        /// </summary>
        public string RepositoryId { get; set; }
    }
}