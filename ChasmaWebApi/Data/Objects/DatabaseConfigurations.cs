using ChasmaWebApi.Util;
using System.Xml.Serialization;

namespace ChasmaWebApi.Data.Objects
{
    /// <summary>
    /// The database configurations of the Chasma database.
    /// </summary>
    public class DatabaseConfigurations : ChasmaXmlBase
    {
        /// <summary>
        /// Gets the server name of the database.
        /// </summary>
        [XmlElement("server")]
        public required string Server { get; set; }

        /// <summary>
        /// Gets the name of the database.
        /// </summary>
        [XmlElement("databaseName")]
        public required string DatabaseName { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether the connection is trusted.
        /// </summary>
        [XmlElement("trustedConnection")]
        public required bool TrustedConnection { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether the certificate is trusted.
        /// </summary>
        [XmlElement("trustedCertificate")]
        public required bool TrustedCertificate{ get; set; }

        /// <summary>
        /// Gets the connection string in the format that is expected of SQLite3 database connections.
        /// </summary>
        /// <returns>The formatted database connection string.</returns>
        public string GetConnectionString()
        {
            return $"Data Source=Chasma.db";
        }
    }
}
