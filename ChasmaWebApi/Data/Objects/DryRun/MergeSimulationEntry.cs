namespace ChasmaWebApi.Data.Objects.DryRun
{
    /// <summary>
    /// Class representing the details to simulate a merge.
    /// </summary>
    public class MergeSimulationEntry
    {
        /// <summary>
        /// Gets or sets the repository identifier.
        /// </summary>
        public string RepositoryId { get; set; }

        /// <summary>
        /// Gets or sets the user identifier.
        /// </summary>
        public int UserId { get; set; }

        /// <summary>
        /// Gets or sets the source branch name.
        /// </summary>
        public string SourceBranch { get; set; }

        /// <summary>
        /// Gets or sets the destination branch name.
        /// </summary>
        public string DestinationBranch { get; set; }
    }
}
