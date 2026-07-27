namespace ChasmaWebApi.Data.Requests.Status
{
    /// <summary>
    /// Class representing the request body to refresh user tokens.
    /// </summary>
    public class RefreshRequest
    {
        /// <summary>
        /// Gets or sets the refresh token to evaluate.
        /// </summary>
        public string RefreshToken { get; set; }
    }
}
