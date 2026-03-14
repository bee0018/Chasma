using ChasmaWebApi.Data.Objects.DryRun;
using ChasmaWebApi.Util;

namespace ChasmaWebApi.Data.Requests.DryRun
{
    /// <summary>
    /// Class representing a request to simulate a branch merge operation in a dry run for the specified repositories.
    /// </summary>
    public class SimulateBranchMergeRequest : ChasmaXmlBase
    {
        /// <summary>
        /// Gets or sets the list of merge entries for which the branch merge operation should be simulated.
        /// </summary>
        public List<MergeSimulationEntry> MergeEntries { get; set; } = new();
    }
}
