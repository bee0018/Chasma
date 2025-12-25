using ChasmaWebApi.Data.Objects;

namespace ChasmaWebApi.Data.Responses
{
    /// <summary>
    /// Class representing the components of a Git Status response.
    /// </summary>
    public class GitStatusResponse : ResponseBase
    {
        /// <summary>
        /// Gets or sets the results of the 'git status' command for the repository.
        /// </summary>
        public List<RepositoryStatusElement> StatusElements { get; set; } = new();

        /// <summary>
        /// Gets or sets the number of commits the local repository is ahead of the remote.
        /// </summary>
        public int CommitsAhead { get; set; }

        /// <summary>
        /// Gets or sets the number of commits the local repository is behind the remote.
        /// </summary>
        public int CommitsBehind { get; set; }

        /// <summary>
        /// Gets or sets the current branch name of the repository.
        /// </summary>
        public string BranchName { get; set; }

        /// <summary>
        /// Gets or sets the remote branch URL of the repository.
        /// </summary>
        public string RemoteUrl { get; set; }

        /// <summary>
        /// Gets or sets the latest commit hash of the repository.
        /// </summary>
        public string CommitHash { get; set; }
    }
}
