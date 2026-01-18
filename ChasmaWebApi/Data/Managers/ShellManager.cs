using ChasmaWebApi.Data.Interfaces;
using System.Diagnostics;

namespace ChasmaWebApi.Data.Managers
{
    /// <summary>
    /// Provides functionality for executing shell commands, specifically Git commands, and managing related client
    /// operations within the application.
    /// </summary>
    /// <param name="logger">The internal API logger.</param>
    /// <param name="cacheManager">The internal cache manager.</param>
    public class ShellManager(ILogger<ShellManager> logger, ICacheManager cacheManager)
        : ClientManagerBase<ShellManager>(logger, cacheManager), IShellManager
    {
        // <inheritdoc/>
        public List<string> ExecuteShellCommands(string workingDirectory, IEnumerable<string> shellCommands)
        {
            List<string> outputMessages = [];
            try
            {
                foreach (string command in shellCommands)
                {
                    ProcessStartInfo processInfo = new("cmd.exe", $"/c {command}")
                    {
                        WorkingDirectory = workingDirectory,
                        RedirectStandardOutput = true,
                        RedirectStandardError = true,
                        UseShellExecute = false,
                        CreateNoWindow = true,
                    };
                    using Process process = new() { StartInfo = processInfo };
                    process.Start();
                    string output = process.StandardOutput.ReadToEnd();
                    string error = process.StandardError.ReadToEnd();
                    process.WaitForExit();
                    if (process.ExitCode != 0)
                    {
                        string errorMessage = $"Command '{command}' failed with error: {error}\n";
                        logger.LogError(errorMessage);
                        outputMessages.Add(errorMessage);
                    }
                    else
                    {
                        string successMessage = $"Command '{command}' executed successfully: {output}\n";
                        logger.LogInformation(successMessage);
                        outputMessages.Add(successMessage);
                    }
                }
            }
            catch (Exception ex)
            {
                string errorMessage = $"An exception occurred while executing commands: {ex.Message}";
                logger.LogError(ex, errorMessage);
                outputMessages.Add(errorMessage);
            }

            return outputMessages;
        }
    }
}
