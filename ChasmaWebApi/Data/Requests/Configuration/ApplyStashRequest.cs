using ChasmaWebApi.Util;
using LibGit2Sharp;

namespace ChasmaWebApi.Data.Requests.Configuration
{
    /// <summary>
    /// Class representing a request to apply a stash configuration.
    /// </summary>
    public class ApplyStashRequest : ChasmaXmlBase
    {
        /// <summary>
        /// Gets or sets the identifier of the repository where the stash is located.
        /// </summary>
        public string RepositoryId { get; set; }

        /// <summary>
        /// Gets or sets the index of the stash to be applied.
        /// </summary>
        public int StashIndex { get; set; }

        /// <summary>
        /// Gets or sets the options for applying the stash, such as whether to apply the stash with or without conflicts.
        /// </summary>
        public StashApplyModifiers ApplyStashModifier { get; set; }
    }
}
