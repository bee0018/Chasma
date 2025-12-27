using ChasmaWebApi.Util;

namespace ChasmaWebApi.Data.Requests
{
    /// <summary>
    /// Class representing the components of a Git Branch Request.
    /// </summary>
    public class GitBranchRequest : ChasmaXmlBase
    {
        /// <summary>
        /// Gets or sets the repository identifier.
        /// </summary>
        public string RepositoryId { get; set; }
    }
}
