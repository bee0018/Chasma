using ChasmaWebApi.Data.Objects.DryRun;
using ChasmaWebApi.Util;

namespace ChasmaWebApi.Data.Requests.DryRun
{
    /// <summary>
    /// Class representing a request to simulate an 'add branch' operation in a dry run for the specified repositories.
    /// </summary>
    public class SimulateAddBranchRequest : ChasmaXmlBase
    {
        /// <summary>
        /// Gets or sets the list of simulation inputs for which the 'add branch' operation should be simulated.
        /// </summary>
        public List<AddBranchSimulationEntry> Entries { get; set; } = new();
    }
}
