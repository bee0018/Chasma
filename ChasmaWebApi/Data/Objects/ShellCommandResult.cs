namespace ChasmaWebApi.Data.Objects
{
    /// <summary>
    /// Class representing the result of executing a shell command.
    /// </summary>
    public class ShellCommandResult
    {
        /// <summary>
        /// Gets or sets the shell command that was executed.
        /// </summary>
        public string ExecutedCommand { get; set; }

        /// <summary>
        /// Gets or sets the output message from executing the shell command.
        /// </summary>
        public string OutputMessage { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether the shell command executed successfully.
        /// </summary>
        public bool IsSuccess { get; set; }
    }
}
