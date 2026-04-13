using ChasmaWebApi.Util;

namespace ChasmaWebApi.Data.Requests.Configuration
{
    /// <summary>
    /// Class representing a request to modify the API configuration.
    /// </summary>
    public class ModifyApiConfigRequest : ChasmaXmlBase
    {
        /// <summary>
        /// Gets or sets the newly modified API configurations to be applied.
        /// </summary>
        public ChasmaWebApiConfigurations ApiConfiguration { get; set; }
    }
}
