using ChasmaWebApi.Data.Objects;
using ChasmaWebApi.Util;
using System.Xml.Serialization;

namespace ChasmaWebApi
{
    [XmlRoot("configurations")]
    public class ChasmaWebApiConfigurations : ChasmaXmlBase
    {
        /// <summary>
        /// Gets or sets the Chasma web API url.
        /// </summary>
        [XmlElement("webApiUrl")]
        public required string WebApiUrl { get; set; }

        /// <summary>
        /// Gets or sets the database configuration options.
        /// </summary>
        [XmlElement("databaseConfiguration")]
        public required DatabaseConfigurations DatabaseConfigurations { get; set; }
        
        /// <summary>
        /// Gets or sets a value indicating whether to show the debug API controllers.
        /// </summary>
        [XmlElement("showDebugControllers")]
        public required bool ShowDebugControllers { get; set; }
        
        /// <summary>
        /// Gets or sets the URL of the thin client.
        /// </summary>
        [XmlElement("thinClientUrl")]
        public required string ThinClientUrl { get; set; }
    }
}