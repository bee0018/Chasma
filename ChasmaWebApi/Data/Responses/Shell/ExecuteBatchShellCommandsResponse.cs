using ChasmaWebApi.Data.Objects;

namespace ChasmaWebApi.Data.Responses.Shell
{
    /// <summary>
    /// Class representing the response for executing batch shell commands.
    /// </summary>
    public class ExecuteBatchShellCommandsResponse : ResponseBase
    {
        /// <summary>
        /// Gets or sets the list of results for each batch command entry.
        /// </summary>
        public List<BatchCommandEntryResult> Results { get; set; } = new();
    }
}
