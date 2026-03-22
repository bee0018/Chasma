namespace ChasmaWebApi.Data.Requests.Remote
{
    /// <summary>
    /// Class representing a request to get pipeline jobs from GitLab.
    /// </summary>
    public class GetPipelineJobsRequest
    {
        /// <summary>
        /// Gets or sets the repository identifier.
        /// </summary>
        public string RepositoryId { get; set; }
    }
}
