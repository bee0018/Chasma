using ChasmaWebApi.Data.Objects;
using ChasmaWebApi.Util;

namespace ChasmaWebApi.Data.Requests.Shell
{
    public class ExecuteBatchShellCommandsRequest : ChasmaXmlBase
    {
        /// <summary>
        /// Gets or sets the list of batch command entries.
        /// </summary>
        public List<BatchCommandEntry> BatchCommands { get; set; } = new();
    }
}
