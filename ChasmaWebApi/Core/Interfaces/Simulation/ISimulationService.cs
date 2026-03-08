using ChasmaWebApi.Data.Objects.DryRun;

namespace ChasmaWebApi.Core.Interfaces.Simulation
{
    /// <summary>
    /// Interface defining the members of the simulation service.
    /// </summary>
    public interface ISimulationService
    {
        /// <summary>
        /// Simulates a git pull operation for the specified repositories.
        /// </summary>
        /// <param name="entries">The simulation input entries.</param>
        /// <returns>The list of simulated git pull dry run results.</returns>
        List<SimulatedGitPullResult> SimulateGitPull(IEnumerable<PullSimulationEntry> entries);

        /// <summary>
        /// Simulates adding a new branch to the specified repositories.
        /// </summary>
        /// <param name="entries">The simulation entries.</param>
        /// <returns>The simulation results of adding branches to the repository.</returns>
        List<SimulatedAddBranchResult> SimulateAddBranch(IEnumerable<AddBranchSimulationEntry> entries);

        /// <summary>
        /// Simulates the merges for the specified repositories.
        /// </summary>
        /// <param name="entries">The merge parameters.</param>
        /// <returns>The list of simulation results.</returns>
        List<SimulatedMergeResult> SimulateMergeBranch(IEnumerable<MergeSimulationEntry> entries);
    }
}
