namespace ChasmaWebApi.Data.Responses.Shell
{
    /// <summary>
    /// Class representing a response after executing a shell command.
    /// </summary>
    public class ExecuteShellCommandResponse : ResponseBase
    {
        /// <summary>
        /// Gets or sets the output message from the command execution.
        /// </summary>
        public List<string> OutputMessages { get; set; } = new();
    }
}
