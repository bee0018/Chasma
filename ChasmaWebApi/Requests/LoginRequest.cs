using ChasmaWebApi.Util;
using System.Xml.Serialization;

namespace ChasmaWebApi.Requests
{
    /// <summary>
    /// Class representing the login request details.
    /// </summary>
    [XmlRoot("loginRequest")]
    public class LoginRequest : ChasmaXmlBase
    {
        /// <summary>
        /// Gets or sets the user name.
        /// </summary>
        [XmlElement("username")]
        public required string UserName { get; set; }

        /// <summary>
        /// Gets or sets the password of the user.
        /// </summary>
        [XmlElement("password")]
        public required string Password { get; set; }
    }
}
