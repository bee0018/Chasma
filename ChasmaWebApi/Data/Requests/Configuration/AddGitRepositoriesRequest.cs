using ChasmaWebApi.Util;

namespace ChasmaWebApi.Data.Requests.Configuration
{
    /// <summary>
    /// Class representing a request to add multiple Git repositories from the local filesystem.
    /// </summary>
    public class AddGitRepositoriesRequest : ChasmaXmlBase
    {
        /// <summary>
        /// Gets or sets the unique identifier for the user.
        /// </summary>
        public int UserId { get; set; }

        /// <summary>
        /// Gets or sets the list of file system paths to the Git repositories to be added.
        /// </summary>
        public List<string> RepositoryPaths { get; set; } = new();
    }
}
