using ChasmaWebApi.Util;

namespace ChasmaWebApi.Data.Requests.Status
{
    /// <summary>
    /// Class representing a login request from the client.
    /// </summary>
    public class LoginRequest : ChasmaXmlBase
    {
        /// <summary>
        /// Gets or sets the user name.
        /// </summary>
        public string UserName { get; set; }

        /// <summary>
        /// Gets or sets the password of the user.
        /// </summary>
        public string Password { get; set; }
    }
}
