using ChasmaWebApi.Util;

namespace ChasmaWebApi.Data.Objects.Remote
{
    /// <summary>
    /// Class representing the details of a remote pull request.
    /// </summary>
    public class RemotePullRequest : ChasmaXmlBase
    {
        /// <summary>
        /// Gets or sets the pull request number.
        /// Note: This is not the database ID, but the PR number in remote Git client.
        /// </summary>
        public long Number { get; set; }

        /// <summary>
        /// Gets or sets the name of the repository this pull request is in.
        /// </summary>
        public string RepositoryName { get; set; }

        /// <summary>
        /// Gets or sets the owner of the repository this pull request is in.
        /// </summary>
        public string RepositoryOwner { get; set; }

        /// <summary>
        /// The branch name that is being merged in the pull request.
        /// </summary>
        public string BranchName { get; set; }

        /// <summary>
        /// Gets or sets the state of the pull request (e.g., open, closed).
        /// </summary>
        public string ActiveState { get; set; }

        /// <summary>
        /// Gets or sets the mergeable state of the pull request (e.g., mergeable, conflicted).
        /// </summary>
        public string MergeableState { get; set; }

        /// <summary>
        /// Gets or sets the creation timestamp of the pull request.
        /// </summary>
        public string CreatedAt { get; set; }

        /// <summary>
        /// Gets or sets the timestamp of the merge of the pull request.
        /// </summary>
        public string? MergedAt { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether the pull request has been merged.
        /// </summary>
        public bool Merged { get; set; }

        /// <summary>
        /// Gets or sets the HTML URL of the pull request.
        /// </summary>
        public string HtmlUrl { get; set; }
    }
}
