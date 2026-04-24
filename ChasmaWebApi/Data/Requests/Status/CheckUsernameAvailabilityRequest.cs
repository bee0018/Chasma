using ChasmaWebApi.Util;

namespace ChasmaWebApi.Data.Requests.Status
{
    /// <summary>
    /// Represents a request to check whether a specified username is available for a given user.
    /// </summary>
    public class CheckUsernameAvailabilityRequest : ChasmaXmlBase
    {
        /// <summary>
        /// Gets or sets the username to check for.
        /// </summary>
        public string UserName { get; set; }
    }
}
