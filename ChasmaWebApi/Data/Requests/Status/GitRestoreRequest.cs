using ChasmaWebApi.Data.Objects.Git;

namespace ChasmaWebApi.Data.Requests.Status
{
    /// <summary>
    /// Class representing the request body for restoring a file.
    /// </summary>
    public class GitRestoreRequest
    {
        /// <summary>
        /// Gets or sets the selected file to be restored.
        /// </summary>
        public RepositoryStatusElement SelectedFile { get; set; }
    }
}
