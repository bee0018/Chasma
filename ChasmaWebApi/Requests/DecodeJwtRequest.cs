namespace ChasmaWebApi.Requests
{
    /// <summary>
    /// Class representing the components to decode a JWT.
    /// </summary>
    public class DecodeJwtRequest
    {
        /// <summary>
        /// Gets or sets the secret key for the JWT.
        /// </summary>
        public string SecretKey { get; set; }

        /// <summary>
        /// Gets or sets the audience of the JWT.
        /// </summary>
        public string Audience { get; set; }

        /// <summary>
        /// Gets or sets the issuer for the JSON web token.
        /// </summary>
        public string Issuer { get; set; }

        /// <summary>
        /// Gets or sets the encoded token.
        /// </summary>
        public string EncodedToken { get; set; }
    }
}
