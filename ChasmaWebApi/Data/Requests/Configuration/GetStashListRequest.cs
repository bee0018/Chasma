using ChasmaWebApi.Util;

namespace ChasmaWebApi.Data.Requests.Configuration
{
    /// <summary>
    /// Class representing a request to retrieve the list of stashes from the specified repository.
    /// </summary>
    public class GetStashListRequest : ChasmaXmlBase
    {
        /// <summary>
        /// Gets or sets the identifier of the repository for which to retrieve the stash list.
        /// </summary>
        public string RepositoryId { get; set; }
    }
}
