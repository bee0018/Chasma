using ChasmaWebApi.Util;

namespace ChasmaWebApi.Data.Requests.Shell
{
    /// <summary>
    /// Class representing a request to open a file.
    /// </summary>
    public class OpenFileRequest : ChasmaXmlBase
    {
        /// <summary>
        /// Gets or sets the repository identifier.
        /// </summary>
        public string RepositoryId { get; set; }

        /// <summary>
        /// Gets or sets the file path to open.
        /// </summary>
        public string FilePath { get; set; }
    }
}
