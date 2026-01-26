using ChasmaWebApi.Util;

namespace ChasmaWebApi.Data.Objects
{
    /// <summary>
    /// Class representing the result of a batch command entry.
    /// </summary>
    public class BatchCommandEntryResult : ChasmaXmlBase
    {
        /// <summary>
        /// Gets or sets the name of the repository.
        /// </summary>
        public string RepositoryName { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether the command execution was successful.
        /// </summary>
        public bool IsSuccess { get; set; }

        /// <summary>
        /// Gets or sets the message associated with the command execution.
        /// </summary>
        public string Message { get; set; }
    }
}
