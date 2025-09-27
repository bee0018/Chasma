using ChasmaWebApi.Requests;
using System.Xml.Serialization;

namespace ChasmaWebApi.Responses
{
    /// <summary>
    /// Class representing the response to a <see cref="LoginRequest"/>.
    /// </summary>
    [XmlRoot("loginRequest")]
    public class LoginResponse : ChasmaResponse
    {
        /// <summary>
        /// Gets or sets the username of the user account.
        /// </summary>
        [XmlElement("username")]
        public required string UserName { get; set; }

        /// <summary>
        /// Gets or sets the name of the user.
        /// </summary>
        [XmlElement("name")]
        public required string Name { get; set; }
    }
}
