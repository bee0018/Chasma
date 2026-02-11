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
        /// <returns>The list of output results from the executed commands.</returns>
        List<ShellCommandResult> ExecuteShellCommands(string workingDirectory, IEnumerable<string> shellCommands);

        /// <summary>
        /// Executes the list of shell commands in batch for multiple working directories.
        /// </summary>
        /// <param name="entries">The entries to execute batch commands for.</param>
        /// <returns>The results of the batch commands.</returns>
        List<BatchCommandEntryResult> ExecuteShellCommandsInBatch(IEnumerable<BatchCommandEntry> entries);

        /// <summary>
        /// Tries to open the file in the default text editor.
        /// </summary>
        /// <param name="filePath">The file path to open.</param>
        /// <param name="workingDirectory">The working directory the file is in.</param>
        /// <param name="errorMessage">The error message.</param>
        /// <returns>True if the file is open, false otherwise.</returns>
        bool TryOpenFile(string filePath, string workingDirectory, out string errorMessage);

        /// <summary>
        /// Tries to discard the changes for the specified file.
        /// </summary>
        /// <param name="workingDirectory">The working directory.</param>
        /// <param name="filePath">The file path to discard changes for.</param>
        /// <param name="errorMessage">The error message.</param>
        /// <returns>True if the file changes are discarded; false otherwise.</returns>
        bool TryDiscardFileChanges(string workingDirectory, string filePath, out string errorMessage);
    }
}
