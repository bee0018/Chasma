using ChasmaWebApi.Util;

namespace ChasmaWebApi.Data.Requests.Configuration
{
    /// <summary>
    /// Class representing a request to modify an existing user in the application.
    /// </summary>
    public class ModifyUserRequest : ChasmaXmlBase
    {
        /// <summary>
        /// Gets or sets the user identifier.
        /// </summary>
        public int UserId { get; set; }

        /// <summary>
        /// Gets or sets the username of the user to be modified.
        /// </summary>
        public string Username { get; set; }

        /// <summary>
        /// Gets or sets the email of the user to be modified.
        /// </summary>
        public string Email { get; set; }

        /// <summary>
        /// Gets or sets the name of the user to be modified.
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Gets or sets the password of the user to be modified.
        /// </summary>
        public string Password { get; set; }
    }
}
