using ChasmaWebApi.Util;

namespace ChasmaWebApi.Data.Requests.Configuration
{
    /// <summary>
    /// Class representing a request to delete a stash configuration.
    /// </summary>
    public class DeleteStashRequest : ChasmaXmlBase
    {
        /// <summary>
        /// Gets or sets the identifier of the repository where the stash is located.
        /// </summary>
        public string RepositoryId { get; set; }

        /// <summary>
        /// Gets or sets the index of the stash to be deleted.
        /// </summary>
        public int StashIndex { get; set; }
    }
}
