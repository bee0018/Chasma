using ChasmaWebApi.Data.Objects.Shell;

namespace ChasmaWebApi.Data.Requests.Shell
{
    public class ExecuteBatchShellCommandsRequest
    {
        /// <summary>
        /// Gets or sets the list of batch command entries.
        /// </summary>
        public List<BatchCommandEntry> BatchCommands { get; set; } = new();
    }
}
