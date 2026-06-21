using ChasmaWebApi.Data.Objects.Application;

namespace ChasmaWebApi.Data.Requests.Infrastructure
{
    /// <summary>
    /// Class representing the request to apply a system update.+
    /// </summary>
    public class ApplyUpdateRequest
    {
        /// <summary>
        /// Gets or sets the system manifest to update the system with.
        /// </summary>
        public SystemManifest SystemManifest { get; set; }
    }
}
