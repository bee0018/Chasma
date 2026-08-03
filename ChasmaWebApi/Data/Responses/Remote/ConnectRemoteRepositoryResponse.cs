using ChasmaWebApi.Data.Objects.Git;

namespace ChasmaWebApi.Data.Responses.Remote
{
    /// <summary>
    /// Class representing a response to a request to connect to a remote repository.
    /// </summary>
    public class ConnectRemoteRepositoryResponse : ResponseBase
    {
        /// <summary>
        /// Gets or sets the local git repository that was connected to the remote repository.
        /// </summary>
        public LocalGitRepository Repository { get; set; }
    }
}
