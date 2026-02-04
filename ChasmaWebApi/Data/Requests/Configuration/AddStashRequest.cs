using ChasmaWebApi.Util;
using LibGit2Sharp;

namespace ChasmaWebApi.Data.Requests.Configuration
{
    /// <summary>
    /// Class representing a request to add stashed changes to a git repository.
    /// </summary>
    public class AddStashRequest : ChasmaXmlBase
    {
        /// <summary>
        /// Gets or sets the repository identifier where the stash operation will be performed.
        /// </summary>
        public string RepositoryId { get; set; }

        /// <summary>
        /// Gets or sets the user identifier associated with the stash operation.
        /// </summary>
        public int UserId { get; set; }

        /// <summary>
        /// Gets or sets the stash modifiers to control the stashing behavior.
        /// </summary>
        StashModifiers StashModifier { get; set; }

        /// <summary>
        /// Gets or sets the message associated with the stashed changes.
        /// </summary>
        public string Message { get; set; }
    }
}
