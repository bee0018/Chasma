using ChasmaWebApi.Data.Objects.Remote;

namespace ChasmaWebApi.Data.Responses.Remote
{
    /// <summary>
    /// Class representing the details of a response to a request to get the cloud provider's repository project members.
    /// </summary>
    public class GetRemoteProjectMembersResponse : ResponseBase
    {
        /// <summary>
        /// Gets or sets the project identifier this group of users belong to.
        /// </summary>
        public long ProjectId { get; set; }

        /// <summary>
        /// Gets or sets the members of the GitLab project.
        /// </summary>
        public List<RemoteProjectMember> ProjectMembers { get; set; } = new();
    }
}
