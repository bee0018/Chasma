namespace ChasmaWebApi.Data.Objects.DryRun
{
    /// <summary>
    /// Class simulating the details to simulate a branch creation operation.
    /// </summary>
    public class AddBranchSimulationEntry
    {
        /// <summary>
        /// Gets or sets the repository identifier.
        /// </summary>
        public string RepositoryId { get; set; }

        /// <summary>
        /// Gets or sets the branch to add.
        /// </summary>
        public string BranchToAdd { get; set; }
    }
}
