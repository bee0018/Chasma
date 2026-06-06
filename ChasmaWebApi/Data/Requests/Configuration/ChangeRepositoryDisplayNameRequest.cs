namespace ChasmaWebApi.Data.Requests.Configuration
{
    /// <summary>
    /// Class representing a request to change the display name of a repository.
    /// </summary>
    public class ChangeRepositoryDisplayNameRequest
    {
        /// <summary>
        /// Gets or sets the identifier of the repository for which the display name is to be changed.
        /// </summary>
        public string RepositoryId { get; set; }

        /// <summary>
        /// Gets or sets the new display name for the repository.
        /// </summary>
        public string NewName { get; set; }
    }
}
