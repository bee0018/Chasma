namespace ChasmaWebApi.Data.Objects.Git
{
    /// <summary>
    /// Class representing the repository work context entry associated to a specified snapshot.
    /// </summary>
    public class RepsoitoryWorkContextSnapshotEntry
    {
        /// <summary>
        /// Gets or sets the snapshot identifier associated with the repository work context entry.
        /// </summary>
        public int SnapshotId { get; set; }

        /// <summary>
        /// Gets or sets the repository identifier associated with the repository work context entry.
        /// </summary>
        public string RepositoryId { get; set; }

        /// <summary>
        /// Gets or sets the branch name of the repository snapshot.
        /// </summary>
        public string BranchName { get; set; }

        /// <summary>
        /// Gets or sets the commit has of the repository snapshot.
        /// </summary>
        public string? CommitHash { get; set; }

        /// <summary>
        /// Gets or sets the time at which this repository snapshot entry was created
        /// </summary>
        public string CreatedAt { get; set; }

        /// <summary>
        /// Gets or sets the stash message.
        /// Note: This will be used to search for the specified stash entry because the stash index is likely to change.
        /// </summary>
        public string? StashMessage { get; set; }

        /// <summary>
        /// Gets or sets the note of intent of the workspace.
        /// </summary>
        public string? IntentNote { get; set; }
    }
}
