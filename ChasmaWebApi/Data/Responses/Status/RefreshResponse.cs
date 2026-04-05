namespace ChasmaWebApi.Data.Responses.Status
{
    /// <summary>
    /// Class representing the response to the refresh the user tokens.
    /// </summary>
    public class RefreshResponse : ResponseBase
    {
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
