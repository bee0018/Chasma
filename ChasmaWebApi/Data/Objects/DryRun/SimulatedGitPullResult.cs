using ChasmaWebApi.Data.Objects.Git;

namespace ChasmaWebApi.Data.Objects.DryRun
{
    /// <summary>
    /// Class representing the result of simulating a Git pull operation in a dry run context.
    /// </summary>
    public class SimulatedGitPullResult : SimulatedResultBase
    {
        /// <summary>
        /// Gets or sets a list of commits that would be pulled if the git pull operation were executed.
        /// </summary>
        public List<CommitEntry> CommitsToPull { get; set; } = new();

        /// <summary>
        /// Gets or sets the branch that is being pulled.
        /// </summary>
        public string BranchName { get; set; }
    }
}
