using ChasmaWebApi.Data.Objects.Git;
using ChasmaWebApi.Util;

namespace ChasmaWebApi.Data.Requests.Configuration
{
    /// <summary>
    /// Class representing a request to retrieve the details of a specific stash entry from the specified repository.
    /// </summary>
    public class GetStashDetailsRequest : ChasmaXmlBase
    {
        /// <summary>
        /// Gets or sets the identifier of the repository for which to retrieve the stash details.
        /// </summary>
        public string RepositoryId { get; set; }

        /// <summary>
        /// Gets or sets the stash entry for which to retrieve the details, including the patch entries of the stashed changes.
        /// </summary>
        public StashEntry StashEntry { get; set; }
    }
}
