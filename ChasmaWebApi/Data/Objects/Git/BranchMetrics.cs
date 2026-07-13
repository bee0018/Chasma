namespace ChasmaWebApi.Data.Objects.Git
{
    /// <summary>
    /// Class representing metrics related to a git branch.
    /// </summary>
    public class BranchMetrics
    {
        /// <summary>
        /// Gets or sets the name of the branch.
        /// </summary>
        public string BranchName { get; set; }

        /// <summary>
        /// Gets or sets the number of commits that the branch is ahead of its upstream branch.
        /// </summary>
        public int AheadCount { get; set; }

        /// <summary>
        /// Gets or sets the number of commits that the branch is behind its upstream branch.
        /// </summary>
        public int BehindCount { get; set; }

        /// <summary>
        /// Gets or sets the date and time when the branch was last updated.
        /// </summary>
        public string LastUpdated { get; set; }
    }
}
