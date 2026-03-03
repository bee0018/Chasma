using ChasmaWebApi.Data.Objects;

namespace ChasmaWebApi.Core.Interfaces.Infrastructure
{
    /// <summary>
    /// Interface containing the members on the shell execution service, which is responsible for executing shell commands, specifically Git commands, and managing related operations within the application.
    /// </summary>
    public interface IShellExecutionService
    {
        /// <summary>
        /// Executes the list of git command in the system shell.
        /// </summary>
        /// <param name="workingDirectory">The working directory to execute the commands in.</param>
        /// <param name="shellCommands">The shell commands to execute.</param>
        /// <returns>The list of output results from the executed commands.</returns>
        List<ShellCommandResult> ExecuteShellCommands(string workingDirectory, IEnumerable<string> shellCommands);

        /// <summary>
        /// Executes the list of shell commands in batch for multiple working directories.
        /// </summary>
        /// <param name="entries">The entries to execute batch commands for.</param>
        /// <returns>The results of the batch commands.</returns>
        List<BatchCommandEntryResult> ExecuteShellCommandsInBatch(IEnumerable<BatchCommandEntry> entries);
    }
}
