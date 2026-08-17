namespace ChasmaWebApi.Data.Requests.Configuration
{
    /// <summary>
    /// Class representing a request to modify the API configuration.
    /// </summary>
    public class ModifyApiConfigRequest
    {
        /// <summary>
        /// Gets or sets the newly modified API configurations to be applied.
        /// </summary>
        public ChasmaWebApiConfigurations ApiConfiguration { get; set; }

        /// <summary>
        /// Gets or sets the identifier of the user making the request.
        /// </summary>
        public int UserId { get; set; }
    }
}
