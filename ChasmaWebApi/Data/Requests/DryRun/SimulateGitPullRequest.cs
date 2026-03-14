using ChasmaWebApi.Data.Objects.DryRun;
using ChasmaWebApi.Util;

namespace ChasmaWebApi.Data.Requests.DryRun
{
    /// <summary>
    /// Class representing a request to simulate a Git pull operation in a dry run context.
    /// </summary>
    public class SimulateGitPullRequest : ChasmaXmlBase
    {
        /// <summary>
        /// Gets or sets the list of simulation inputs for which the 'git pull' operation should be simulated.
        /// </summary>
        public List<PullSimulationEntry> Entries { get; set; } = new();
    }
}
