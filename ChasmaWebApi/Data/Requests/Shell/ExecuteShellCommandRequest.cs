using ChasmaWebApi.Util;

namespace ChasmaWebApi.Data.Requests.Shell
{
    /// <summary>
    /// Class representing a request to execute a shell command.
    /// </summary>
    public class ExecuteShellCommandRequest : ChasmaXmlBase
    {
        /// <summary>
        /// Gets or sets the repository identifier.
        /// </summary>
        public string RepositoryId { get; set; }

        /// <summary>
        /// Gets or sets the list of shell commands to execute.
        /// </summary>
        public List<string> Commands { get; set; } = new();
    }
}
