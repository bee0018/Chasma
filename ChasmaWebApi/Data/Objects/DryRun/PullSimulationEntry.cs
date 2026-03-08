namespace ChasmaWebApi.Data.Objects.DryRun
{
    /// <summary>
    /// Class representing the details to simulate a pull operation.
    /// </summary>
    public class PullSimulationEntry
    {
        /// <summary>
        /// Gets or sets the repository identifier.
        /// </summary>
        public string RepositoryId { get; set; }

        /// <summary>
        /// Gets or sets the branch to pull.
        /// </summary>
        public string BranchToPull { get; set; }
    }
}
