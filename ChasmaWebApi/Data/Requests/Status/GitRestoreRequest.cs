using ChasmaWebApi.Data.Objects.Git;
using ChasmaWebApi.Util;

namespace ChasmaWebApi.Data.Requests.Status
{
    /// <summary>
    /// Class representing the request body for restoring a file.
    /// </summary>
    public class GitRestoreRequest : ChasmaXmlBase
    {
        /// <summary>
        /// Gets or sets the selected file to be restored.
        /// </summary>
        public RepositoryStatusElement SelectedFile { get; set; }
    }
}
