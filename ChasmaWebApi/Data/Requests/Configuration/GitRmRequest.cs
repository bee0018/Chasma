using ChasmaWebApi.Data.Objects.Git;
using ChasmaWebApi.Util;

namespace ChasmaWebApi.Data.Requests.Configuration
{
    /// <summary>
    /// Class representing the request to remove a specific file from the repository.
    /// </summary>
    public class GitRmRequest : ChasmaXmlBase
    {
        /// <summary>
        /// Gets or sets the selected file to be removed.
        /// </summary>
        public RepositoryStatusElement SelectedFile { get; set; }
    }
}
