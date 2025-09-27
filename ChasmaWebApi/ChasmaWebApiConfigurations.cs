using ChasmaWebApi.Data.Objects;
using ChasmaWebApi.Util;
using System.Xml.Serialization;

namespace ChasmaWebApi
{
    [XmlRoot("configurations")]
    public class ChasmaWebApiConfigurations : ChasmaXmlBase
    {
        /// <summary>
        /// Gets the Chasma web API url.
        /// </summary>
        [XmlElement("webApiUrl")]
        public required string WebApiUrl { get; set; }

        /// <summary>
        /// Gets or sets the thin-client URL.
        /// Note: This will also be used for Cross-Origin Sharing (CORS) to allow Access-Control-Headers in preflight responses.
        /// </summary>
        [XmlElement("thinClientUrl")]
        public required string ThinClientUrl { get; set; }

        /// <summary>
        /// Gets the database configuration options.
        /// </summary>
        [XmlElement("databaseConfiguration")]
        public required DatabaseConfigurations DatabaseConfigurations { get; set; }
    }
}
