namespace ChasmaWebApi.Data.Requests.Configuration
{
    /// <summary>
    /// Class representing a request to initialize a repository.
    /// </summary>
    public class InitializeRepositoryRequest
    {
        /// <summary>
        /// Gets or sets the user identifier.
        /// </summary>
        public int UserId { get; set; }

        /// <summary>
        /// Gets or sets the repository identifier.
        /// </summary>
        public string RepositoryId { get; set; }

        /// <summary>
        /// Gets or sets the commit message for the initial commit.
        /// </summary>
        public string? CommitMessage { get; set; }

        /// <summary>
        /// Gets or sets the name of the head branch for the repository.
        /// </summary>
        public string? HeadBranchName { get; set; }
    }
}
