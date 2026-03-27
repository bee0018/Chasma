namespace ChasmaWebApi.Data.Objects.Git
{
    /// <summary>
    /// Class representing a state snapshot of a repository/branch relationship.
    /// </summary>
    public class BranchSyncStatus
    {
        /// <summary>
        /// Gets or sets the repository name.
        /// </summary>
        public string RepositoryName { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether the specified branch exists.
        /// </summary>
        public bool BranchExists { get; set; }

        /// <summary>
        /// Gets or sets the number of commits ahead of the base branch.
        /// </summary>
        public string Ahead { get; set; }

        /// <summary>
        /// Gets or sets the number of commits behind of the base branch.
        /// </summary>
        public string Behind { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether the branch has a pull request open.
        /// </summary>
        public bool PullRequestOpen { get; set; }

        /// <summary>
        /// Gets or sets the build status of the branch.
        /// </summary>
        public string BuildStatus { get; set; }

        /// <summary>
        /// Gets or sets when the branch was last updated.
        /// </summary>
        public string LastUpdated { get; set; }
    }
}
