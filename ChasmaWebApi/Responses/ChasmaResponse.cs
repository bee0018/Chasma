using ChasmaWebApi.Util;

namespace ChasmaWebApi.Responses
{
    /// <summary>
    /// Class representing the metadata of any Chasma response.
    /// </summary>
    public class ChasmaResponse : ChasmaXmlBase, IChasmaResponse
    {
        /// <summary>
        /// Gets or sets whether the response is an error or not.
        /// </summary>
        public required bool IsErrorMessage { get; set; }

        /// <summary>
        /// Gets or sets the message (if provided) of the response.
        /// Note: This will typically be provided when there is an error response.
        /// </summary>
        public required string Message { get; set; }
    }
}
