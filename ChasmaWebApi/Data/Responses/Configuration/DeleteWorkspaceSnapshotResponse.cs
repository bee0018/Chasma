namespace ChasmaWebApi.Data.Responses.Configuration
{
    /// <summary>
    /// Class representing the response for deleting a work context snapshot for a repository.
    /// </summary>
    public class DeleteWorkspaceSnapshotResponse : ResponseBase
    {
        /// <summary>
        /// Gets or sets the list of deleted snapshot identifiers.
        /// </summary>
        public List<int> SnapshotIds { get; set; } = [];
    }
}
