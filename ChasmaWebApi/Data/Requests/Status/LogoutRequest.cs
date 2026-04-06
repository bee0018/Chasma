using ChasmaWebApi.Util;

namespace ChasmaWebApi.Data.Requests.Status
{
    /// <summary>
    /// Class representing the request body to log out a user and invalidate their tokens.
    /// </summary>
    public class LogoutRequest : ChasmaXmlBase
    {
        /// <summary>
        /// Gets or sets the user identifier for the user to log out and invalidate tokens for.
        /// </summary>
        public int UserId { get; set; }
    }
}
