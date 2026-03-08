using ChasmaWebApi.Data.Objects.DryRun;

namespace ChasmaWebApi.Data.Responses.DryRun
{
    /// <summary>
    /// Class representing the response of simulating a branch merge operation in a dry run for the specified repositories.
    /// </summary>
    public class SimulateBranchMergeResponse : ResponseBase
    {
        /// <summary>
        /// Gets or sets the list of results for each repository where the branch merge operation was simulated.
        /// </summary>
        public List<SimulatedMergeResult> SimulationResults { get; set; } = new();
    }
}
