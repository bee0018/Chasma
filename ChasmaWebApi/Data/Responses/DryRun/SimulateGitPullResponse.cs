using ChasmaWebApi.Data.Objects.DryRun;

namespace ChasmaWebApi.Data.Responses.DryRun
{
    /// <summary>
    /// Class representing the response from simulating a Git pull operation in a dry run context.
    /// </summary>
    public class SimulateGitPullResponse : ResponseBase
    {
        /// <summary>
        /// Gets or sets a list of results for each repository that was included in the simulation of the git pull operation.
        /// </summary>
        public List<SimulatedGitPullResult> PullResults { get; set; } = new();
    }
}
