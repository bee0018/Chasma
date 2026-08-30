using ChasmaWebApi.Data.Objects.Remote;

namespace ChasmaWebApi.Data.Requests.Remote
{
    /// <summary>
    /// Class representing the components needed to create a GitHub issue.
    /// </summary>
    public class CreateGitHubIssueRequest
    {
        /// <summary>
        /// Gets or sets the repository name.
        /// </summary>
        public string RepositoryName { get; set; }

        /// <summary>
        /// Gets or sets the repository owner.
        /// </summary>
        public string RepositoryOwner { get; set; }

        /// <summary>
        /// Gets or sets the issue title.
        /// </summary>
        public string Title { get; set; }

        /// <summary>
        /// Gets or sets the description of the issue.
        /// </summary>
        public string? Body { get; set; }

        /// <summary>
        /// Gets or sets the assignees of the issue to be created.
        /// </summary>
        public List<RemoteProjectMember> Assignees { get; set; } = [];

        /// <summary>
        /// Gets or sets the labels to be applied to the issue.
        /// </summary>
        public List<string> Labels { get; set; } = [];
    }
}
