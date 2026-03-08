namespace ChasmaWebApi.Data.Objects.DryRun
{
    /// <summary>
    /// Class representing the result of simulating the result of creating a branch.
    /// </summary>
    public class SimulatedAddBranchResult : SimulatedResultBase
    {
        /// <summary>
        /// Gets or sets an informational message describing the result of simulating the creation of a branch.
        /// </summary>
        public string InfoMessage { get; set; }

        /// <summary>
        /// Gets or sets the branch to add.
        /// </summary>
        public string BranchToAdd { get; set; }
    }
}
