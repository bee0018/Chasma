using ChasmaWebApi.Data.Objects.Application;

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

        /// <summary>
        /// Gets or sets the user associated with the response, if applicable.
        /// </summary>
        public ApplicationUser? User { get; set; }
    }
}
