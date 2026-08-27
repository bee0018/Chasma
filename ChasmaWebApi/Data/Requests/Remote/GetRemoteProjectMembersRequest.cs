using ChasmaWebApi.Data.Objects.Application;

namespace ChasmaWebApi.Data.Requests.Remote
{
    /// <summary>
    /// Class representing the details to get the cloud provider's repository project members.
    /// </summary>
    public class GetRemoteProjectMembersRequest
    {
        /// <summary>
        /// Gets or sets the repository identifier.
        /// </summary>
        public string RepositoryId { get; set; }
    }
}
