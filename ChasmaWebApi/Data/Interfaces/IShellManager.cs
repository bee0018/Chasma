using ChasmaWebApi.Data.Objects;

namespace ChasmaWebApi.Data.Interfaces
{
    /// <summary>
    /// Class defining the members required for a shell manager.
    /// </summary>
    public interface IShellManager
    {
        /// <summary>
        /// Executes the list of git command in the system shell.
        /// </summary>
        /// <param name="workingDirectory">The working directory to execute the commands in.</param>
        /// <param name="shellCommands">The shell commands to execute.</param>
        /// <returns>The list of output messages from the executed commands.</returns>
        List<string> ExecuteShellCommands(string workingDirectory, IEnumerable<string> shellCommands);

        /// <summary>
        /// Executes the list of shell commands in batch for multiple working directories.
        /// </summary>
        /// <param name="entries">The entries to execute batch commands for.</param>
        /// <returns>The results of the batch commands.</returns>
        List<BatchCommandEntryResult> ExecuteShellCommandsInBatch(IEnumerable<BatchCommandEntry> entries);
    }
}
