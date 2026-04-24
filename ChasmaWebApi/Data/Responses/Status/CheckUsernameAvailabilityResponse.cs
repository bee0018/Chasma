namespace ChasmaWebApi.Data.Responses.Status
{
    /// <summary>
    /// Class representing a response to checking whether a username is available.
    /// </summary>
    public class CheckUsernameAvailabilityResponse : ResponseBase
    {
        /// <summary>
        /// Gets or sets a value indicating whether the requested username is available.
        /// </summary>
        public bool IsAvailable { get; set; }
    }
}
