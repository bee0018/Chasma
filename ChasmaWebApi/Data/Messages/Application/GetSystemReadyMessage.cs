using ChasmaWebApi.Util;

namespace ChasmaWebApi.Data.Messages.Application
{
    /// <summary>
    /// Class representing the message to indicate that the system is ready.
    /// </summary>
    public class GetSystemReadyMessage : ChasmaXmlBase
    {
        /// <summary>
        /// Gets or sets a value indicating whether the system is ready.
        /// Note: This is determined by if all the required XML elements have been populated.
        /// </summary>
        public bool IsReady { get; set; }

        /// <summary>
        /// Gets or sets a comma separated list of the XML elements that are invalid or missing.
        /// </summary>
        public string InvalidElements { get; set; }
    }
}
