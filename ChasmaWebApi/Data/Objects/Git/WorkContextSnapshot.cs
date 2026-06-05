namespace ChasmaWebApi.Data.Objects.Git
{
    /// <summary>
    /// Class representing the development workspace context snapshot.
    /// </summary>
    public class WorkContextSnapshot
    {
        /// <summary>
        /// Gets or sets the snapshot identifier.
        /// </summary>
        public int SnapshotId { get; set; }

        /// <summary>
        /// Gets or sets the user identifier associated with the work context snapshot.
        /// </summary>
        public int UserId { get; set; }

        /// <summary>
        /// Gets or sets the snapshot display name.
        /// </summary>
        public string DisplayName { get; set; }

        /// <summary>
        /// Gets or sets the overall working note of the work context snapshot.
        /// </summary>
        public string? SnapshotNote { get; set; }

        /// <summary>
        /// Gets or sets the repository snapshot entries associated with the snapshot.
        /// </summary>
        public List<RepsoitoryWorkContextSnapshotEntry> RepositorySnapshots { get; set; } = new();
    }
}
