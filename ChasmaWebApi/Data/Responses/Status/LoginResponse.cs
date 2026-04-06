using ChasmaWebApi.Data.Objects.Application;

namespace ChasmaWebApi.Data.Responses.Status
{
    /// <summary>
    /// Class representing the components of a response to a login request.
    /// </summary>
    public class LoginResponse : ResponseBase
    {
        /// <summary>
        /// Gets or sets the logged in user.
        /// </summary>
        public ApplicationUser User { get; set; }

        /// <summary>
        /// Gets or sets the user's authentication token.
        /// </summary>
        public string Token { get; set; }

        /// <summary>
        /// Gets or sets the user's refresh token.
        /// </summary>
        public string RefreshToken { get; set; }
    }
}
