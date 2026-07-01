namespace ChasmaWebApi.Data.Objects.Remote
{
    /// <summary>
    /// Class representing a pull request that will be created.
    /// </summary>
    public class PreparedGitHubPullRequest
    {
        /// <summary>
        /// Gets or sets the repository identifier.
        /// </summary>
        public string RepositoryId { get; set; }

        /// <summary>
        /// Gets or sets the working directory.
        /// </summary>
        public string WorkingDirectory { get; set; }

        /// <summary>
        /// Gets or sets the repository owner.
        /// </summary>
        public string RepositoryOwner { get; set; }

        /// <summary>
        /// Gets or sets the repository name.
        /// </summary>
        public string RepositoryName { get; set; }

        /// <summary>
        /// Gets or sets the pull request title.
        /// </summary>
        public string PullRequestTitle { get; set; }

        /// <summary>
        /// Gets or sets the head branch.
        /// </summary>
        public string HeadBranch { get; set; }

        /// <summary>
        /// Gets or sets the base branch.
        /// </summary>
        public string BaseBranch { get; set; }

        /// <summary>
        /// Gets or sets the pull request body.
        /// </summary>
        public string Description { get; set; }

        /// <summary>
        /// Gets or sets the GitHub API access token.
        /// </summary>
        public string Token { get; set; }
    }
}
