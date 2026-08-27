namespace ChasmaWebApi.Data.Requests.Remote
{
    /// <summary>
    /// Represents a request to get GitHub labels for a repository.
    /// </summary>
    public class GetLabelsRequest
    {
        /// <summary>
        /// Gets or sets the repository identifier for which to retrieve labels.
        /// </summary>
        public string RepositoryId { get; set; }
    }
}
