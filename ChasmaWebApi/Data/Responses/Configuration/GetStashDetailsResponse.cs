using ChasmaWebApi.Data.Objects.Git;

namespace ChasmaWebApi.Data.Responses.Configuration
{
    /// <summary>
    /// Class representing a response containing the details of a specific stash entry from the specified repository, including the patch entries of the stashed changes.
    /// </summary>
    public class GetStashDetailsResponse : ResponseBase
    {
        /// <summary>
        /// Gets or sets the list of patch entries representing the details of the specified stash entry, including the stashed changes and their corresponding file paths.
        /// </summary>
        public List<PatchEntry> PatchEntries { get; set; } = new();
    }
}
