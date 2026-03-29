using ChasmaWebApi.Data.Objects.Application;

namespace ChasmaWebApi.Data.Responses.Configuration
{
    /// <summary>
    /// Class representing the components of a response to adding a user.
    /// </summary>
    public class AddUserResponse : ResponseBase
    {
        /// <summary>
        /// Gets or sets the newly added user.
        /// </summary>
        public ApplicationUser User { get; set; }

        /// <summary>
        /// Gets or sets the user's authentication token.
        /// </summary>
        public string Token { get; set; }
    }
}
