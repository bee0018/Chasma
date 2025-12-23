namespace ChasmaWebApi.Data.Objects
{
    /// <summary>
    /// Class representing a summary of a repository.
    /// </summary>
    public class RepositorySummary
    {
        /// <summary>
        /// Gets or sets the list of status elements for the repository.
        /// </summary>
        public List<RepositoryStatusElement> StatusElements { get; set; } = new();

        /// <summary>
        /// Gets or sets the current branch name of the repository.
        /// </summary>
        public string BranchName { get; set; }

        /// <summary>
        /// Gets or sets the number of commits the local repository is ahead of the remote.
        /// </summary>
        public int CommitsAhead { get; set; }

        /// <summary>
        /// Gets or sets the number of commits the local repository is behind the remote.
        /// </summary>
        public int CommitsBehind { get; set; }

        /// <summary>
        /// Gets or sets the remote branch URL of the repository.
        /// </summary>
        public string RemoteUrl { get; set; }
    }
}
