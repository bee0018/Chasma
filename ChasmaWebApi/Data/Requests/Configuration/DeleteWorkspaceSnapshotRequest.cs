namespace ChasmaWebApi.Data.Requests.Configuration
{
    /// <summary>
    /// Class representing the request to delete a work context snapshot for a repository.
    /// </summary>
    public class DeleteWorkspaceSnapshotRequest
    {
        /// <summary>
        /// Gets or sets the list snapshot identifiers to delete.
        /// </summary>
        public List<int> SnapshotIds { get; set; } = [];
    }
}
