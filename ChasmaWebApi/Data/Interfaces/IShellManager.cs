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
    }
}
