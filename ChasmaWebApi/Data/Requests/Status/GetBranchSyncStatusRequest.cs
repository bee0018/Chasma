namespace ChasmaWebApi.Data.Requests.Status
{
    /// <summary>
    /// Class representing the request to get the branch synchronization status.
    /// </summary>
    public class GetBranchSyncStatusRequest
    {
        /// <summary>
        /// Gets or sets the name of the branch to get statuses for.
        /// </summary>
        public string BranchName { get; set; }

        /// <summary>
        /// Gets or sets the user identifier.
        /// </summary>
        public int UserId { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether to skip branch build retrieval.
        /// </summary>
        public bool SkipBuildRetrieval { get; set; }
    }
}
