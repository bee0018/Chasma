using ChasmaWebApi.Data.Objects.Application;

namespace ChasmaWebApi.Data.Responses.Configuration
{
    /// <summary>
    /// Class representing a response to a request to modify an existing user in the application.
    /// </summary>
    public class ModifyUserResponse : ResponseBase
    {
        /// <summary>
        /// Gets or sets the modified user.
        /// </summary>
        public ApplicationUser User { get; set; }

        /// <summary>
        /// Gets or sets the user access token.
        /// </summary>
        public string Token { get; set; }

        /// <summary>
        /// Gets or sets the user's refresh token.
        /// </summary>
        public string RefreshToken { get; set; }
    }
}
