namespace ChasmaWebApi.Data.Requests.Configuration
{
    /// <summary>
    /// Class representing the request to apply a work context snapshot for a repository.
    /// </summary>
    public class ApplyWorkContextSnapshotRequest
    {
        /// <summary>
        /// Gets or sets the snapshot identifier to apply.
        /// </summary>
        public int SnapshotId { get; set; }
    }
}
