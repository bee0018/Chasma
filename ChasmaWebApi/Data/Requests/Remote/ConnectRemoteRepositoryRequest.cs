namespace ChasmaWebApi.Data.Requests.Remote
{
    /// <summary>
    /// Class representing a request to connect to a remote repository.
    /// </summary>
    public class ConnectRemoteRepositoryRequest
    {
        /// <summary>
        /// Gets or sets the user identifier.
        /// </summary>
        public int UserId { get; set; }

        /// <summary>
        /// Gets or sets the url of the remote repository to connect to.
        /// </summary>
        public string Url { get; set; }

        /// <summary>
        /// Gets or sets the name of the head branch to connect to.
        /// </summary>
        public string? HeadBranchName { get; set; }

        /// <summary>
        /// Gets or sets the repository identifier.
        /// </summary>
        public string RepositoryId { get; set; }
    }
}
