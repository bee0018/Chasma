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
        /// Gets the host name of the database.
        /// </summary>
        [XmlElement("host")]
        public required string Host { get; set; }

        /// <summary>
        /// Gets the port number of the database.
        /// </summary>
        [XmlElement("port")]
        public required int Port { get; set; }

        /// <summary>
        /// Gets the name of the database.
        /// </summary>
        [XmlElement("databaseName")]
        public required string DatabaseName { get; set; }

        /// <summary>
        /// Gets the user name of the database.
        /// </summary>
        [XmlElement("username")]
        public required string Username { get; set; }

        /// <summary>
        /// Gets the encrypted password of the database.
        /// </summary>
        [XmlElement("password")]
        public required string Password { get; set; }

        /// <summary>
        /// Gets the connection string in the format that is expected of PostgresSQL database connections.
        /// </summary>
        /// <returns>The formatted database connection string.</returns>
        public string GetConnectionString()
        {
            return $"Host={Host};Port={Port};Database={DatabaseName};Username={Username};Password={Password}";
        }
    }
}
