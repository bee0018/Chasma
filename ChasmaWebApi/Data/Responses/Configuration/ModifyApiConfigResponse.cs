namespace ChasmaWebApi.Data.Responses.Configuration
{
    /// <summary>
    /// Class representing a response to a request to modify the API configuration.
    /// </summary>
    public class ModifyApiConfigResponse : ResponseBase
    {
        /// <summary>
        /// Gets or sets a value indicating whether the static configurations have changed since the last update.
        /// </summary>
        public bool StaticConfigurationsChanged { get; set; }
    }
}
