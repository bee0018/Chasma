namespace ChasmaWebApi.Data.Requests.Remote
{
    /// <summary>
    /// Class representing the details to get the GitLab project members.
    /// </summary>
    public class GetGitLabProjectMembersRequest
    {
        /// <summary>
        /// Gets or sets the repository identifier.
        /// </summary>
        public string RepositoryId { get; set; }
    }
}
