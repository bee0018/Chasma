using ChasmaWebApi.Util;

namespace ChasmaWebApi.Data.Requests.Configuration
{
    /// <summary>
    /// Class representing a request to add a Git repository from the local filesystem.
    /// </summary>
    public class AddGitRepositoryRequest : ChasmaXmlBase
    {
        /// <summary>
        /// Gets or sets the unique identifier for the user.
        /// </summary>
        public int UserId { get; set; }

        /// <summary>
        /// Gets or sets the file system path to the Git repository.
        /// </summary>
        public string RepositoryPath { get; set; }
    }
}
