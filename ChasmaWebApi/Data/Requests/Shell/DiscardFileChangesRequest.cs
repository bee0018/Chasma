using ChasmaWebApi.Util;

namespace ChasmaWebApi.Data.Requests.Shell
{
    /// <summary>
    /// Class representing a request to discard file changes.
    /// </summary>
    public class DiscardFileChangesRequest : ChasmaXmlBase
    {
        /// <summary>
        /// Gets or sets the repository identifier.
        /// </summary>
        public string RepositoryId { get; set; }

        /// <summary>
        /// Gets or sets the filepath to discard changes.
        /// </summary>
        public string FilePath { get; set; }
    }
}
