namespace ChasmaWebApi.Data.Objects.DryRun
{
    /// <summary>
    /// Class representing the simulated result of a merge.
    /// </summary>
    public class SimulatedMergeResult : SimulatedResultBase
    {
        /// <summary>
        /// Gets or sets the list of local file paths that have merge conflicts.
        /// </summary>
        public List<string> ConflictingFiles { get; set; } = new();

        /// <summary>
        /// Gets or sets the status of the simulated merge.
        /// </summary>
        public string MergeStatus { get; set; }
    }
}
