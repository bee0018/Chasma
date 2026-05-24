using ChasmaWebApi.Data.Objects.Git;
using ChasmaWebApi.Util;

namespace ChasmaWebApi.Data.Requests.Configuration
{
    /// <summary>
    /// Class representing a request to clone a git repository.
    /// </summary>
    public class GitCloneRequest : ChasmaXmlBase
    {
        /// <summary>
        /// Gets or sets the list of blueprints for cloning git repositories.
        /// </summary>
        public List<GitCloneBlueprint> Blueprints { get; set; } = [];

        /// <summary>
        /// Gets or sets the user identifier that the repositories will be associated to.
        /// </summary>
        public int UserId { get; set; }
    }
}
