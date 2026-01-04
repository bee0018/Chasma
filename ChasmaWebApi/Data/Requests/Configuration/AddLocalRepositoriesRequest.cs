using ChasmaWebApi.Util;

namespace ChasmaWebApi.Data.Requests.Configuration
{
    /// <summary>
    /// Class representing the request to add local repositories from the logical drives on the system.
    /// </summary>
    public class AddLocalRepositoriesRequest : ChasmaXmlBase
    {
        /// <summary>
        /// Gets or sets the user identifier.
        /// </summary>
        public int UserId { get; set; }
    }
}
