using ChasmaWebApi.Data.Objects.Remote;
using ChasmaWebApi.Util;

namespace ChasmaWebApi.Data.Messages
{
    /// <summary>
    /// Message containing all the global pull request statuses across all repositories.
    /// </summary>
    public class GetPullRequestStatusMessage : ChasmaXmlBase
    {
        /// <summary>
        /// Gets or sets the list of all pull requests in all repositories.
        /// </summary>
        public List<RemotePullRequest> PullRequests { get; set; } = new();
    }
}
