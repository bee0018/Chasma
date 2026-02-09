using ChasmaWebApi.Data.Objects;

namespace ChasmaWebApi.Data.Responses.Configuration
{
    /// <summary>
    /// Class representing the response to a request for retrieving the list of stashes from a repository.
    /// </summary>
    public class GetStashListResponse : ResponseBase
    {
        /// <summary>
        /// Gets or sets the list of stash entries retrieved from the repository.
        /// </summary>
        public List<StashEntry> StashList { get; set; } = new();
    }
}
