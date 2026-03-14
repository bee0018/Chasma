using ChasmaWebApi.Data.Objects.DryRun;

namespace ChasmaWebApi.Data.Responses.DryRun
{
    /// <summary>
    /// Class representing the response of simulating an 'add branch' operation in a dry run for the specified repositories.
    /// </summary>
    public class SimulateAddBranchResponse : ResponseBase
    {
        /// <summary>
        /// Gets or sets the list of results for each repository where the 'add branch' operation was simulated.
        /// </summary>
        public List<SimulatedAddBranchResult> SimulationResults { get; set; } = new();
    }
}
