namespace ChasmaWebApi.Data.Requests.Status
{
    /// <summary>
    /// Class representing the git function to 'git push'.
    /// </summary>
    public class GitPushRequest
    {
        /// <summary>
        /// Gets or sets the repository identifier.
        /// </summary>
        public string RepositoryId { get; set; }
    }
}
