using ChasmaWebApi.Util;

namespace ChasmaWebApi.Data.Requests
{
    /// <summary>
    /// Class representing a request to create a pull request.
    /// Note: This request will be used for GitHub pull requests since it will be used with the Ocktokit library.
    /// </summary>
    public class CreatePRRequest : ChasmaXmlBase
    {
        /// <summary>
        /// Gets or sets the repository identifier.
        /// </summary>
        public string RepositoryId { get; set; }

        /// <summary>
        /// Gets or sets the pull request title.
        /// </summary>
        public string PullRequestTitle { get; set; }

        /// <summary>
        /// Gets or sets the name of the working branch that will be merged into the base branch.
        /// </summary>
        public string WorkingBranchName { get; set; }

        /// <summary>
        /// Gets or sets the name of the destination branch that is being mreged into.
        /// </summary>
        public string DestinationBranchName { get; set; }

        /// <summary>
        /// Gets or sets the pull request body message.
        /// </summary>
        public string PullRequestBody { get; set; }

        /// <summary>
        /// Gets or sets the repository name.
        /// </summary>
        public string RepositoryName { get; set; }
    }
}
