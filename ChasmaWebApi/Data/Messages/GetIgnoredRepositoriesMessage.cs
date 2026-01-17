using ChasmaWebApi.Util;

namespace ChasmaWebApi.Data.Messages
{
    /// <summary>
    /// Class containing the information local git repositories a user has chosen to ignore.
    /// </summary>
    public class GetIgnoredRepositoriesMessage : ChasmaXmlBase
    {
        /// <summary>
        /// Gets or sets the list of ignored repositories.
        /// Note: Each element will be in the format: repo name: repo identifier
        /// </summary>
        public List<string> IgnoredRepositories { get; set; } = new();
    }
}
