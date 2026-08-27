using ChasmaWebApi.Data.Objects.Remote;

namespace ChasmaWebApi.Data.Requests.Remote
{
    /// <summary>
    /// Class representing a request to create a GitLab issue.
    /// </summary>
    public class CreateGitLabIssueRequest
    {
        /// <summary>
        /// Gets or sets the repository identifier.
        /// </summary>
        public string RepositoryId { get; set; }

        /// <summary>
        /// Gets or sets the main assignee of an issue.
        /// </summary>
        public RemoteProjectMember? MainAssignee { get; set; }

        /// <summary>
        /// Gets or sets the contacts of an issue.
        /// </summary>
        public List<RemoteProjectMember>? Contacts { get; set; } = new();

        /// <summary>
        /// Gets or sets the title of the issue.
        /// Note: This is required.
        /// </summary>
        public string Title { get; set; }

        /// <summary>
        /// Gets or sets the description of the issue.
        /// </summary>
        public string? Description { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether the issue is confidential.
        /// </summary>
        public bool Confidential { get; set; }

        /// <summary>
        /// Gets or sets the labels to be applied to the issue.
        /// </summary>
        public List<string> Labels { get; set; } = [];
    }
}
