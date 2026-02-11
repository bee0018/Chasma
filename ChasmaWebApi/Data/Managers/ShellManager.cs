using ChasmaWebApi.Data.Interfaces;
using ChasmaWebApi.Data.Objects;
using ChasmaWebApi.Util;
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
        public List<ShellCommandResult> ExecuteShellCommands(string workingDirectory, IEnumerable<string> shellCommands)
        {
            List<ShellCommandResult> commandResults = [];
            try
            {
                foreach (string command in shellCommands)
                {
                    using Process process = ShellUtility.GetStandardShell(command, workingDirectory);
                    process.Start();
                    string output = process.StandardOutput.ReadToEnd();
                    string error = process.StandardError.ReadToEnd();
                    process.WaitForExit();
                    ShellCommandResult commandResult = new() { ExecutedCommand = command };
                    if (process.ExitCode != 0)
                    {
                        commandResult.IsSuccess = false;
                        commandResult.OutputMessage = error;
                        string errorMessage = $"Command '{command}' failed with error: {error}\n";
                        logger.LogError(errorMessage);
                    }
                    else
                    {
                        commandResult.IsSuccess = true;
                        commandResult.OutputMessage = output;
                        string successMessage = $"Command '{command}' executed successfully: {output}\n";
                        logger.LogInformation(successMessage);
                    }

                    commandResults.Add(commandResult);
                }
            }
            catch (Exception ex)
            {
                string errorMessage = $"An exception occurred while executing commands: {ex.Message}";
                logger.LogError(ex, errorMessage);
                ShellCommandResult errorResult = new()
                {
                    IsSuccess = false,
                    OutputMessage = errorMessage
                };
                commandResults.Add(errorResult);
            }

            return commandResults;
        }

        // <inheritdoc/>
        public List<BatchCommandEntryResult> ExecuteShellCommandsInBatch(IEnumerable<BatchCommandEntry> entries)
        {
            List<BatchCommandEntryResult> results = [];
            foreach (BatchCommandEntry entry in entries)
            {
                string repoId = entry.RepositoryId;
                if (!CacheManager.WorkingDirectories.TryGetValue(repoId, out string workingDirectory))
                {
                    ClientLogger.LogWarning("Repository ID {repoId} not found in working directories cache.", repoId);
                    BatchCommandEntryResult result = new()
                    {
                        RepositoryName = repoId,
                        IsSuccess = false,
                        Message = $"Repository ID {repoId} not found in working directories cache."
                    };
                    results.Add(result);
                    continue;
                }

                List<ShellCommandResult> commandResults = ExecuteShellCommands(workingDirectory, entry.Commands);
                string repoName = CacheManager.Repositories.TryGetValue(repoId, out LocalGitRepository repository)
                    ? repository.Name
                    : repoId;
                foreach (ShellCommandResult commandResult in commandResults)
                {
                    BatchCommandEntryResult entryResult = new()
                    {
                        RepositoryName = repoName,
                        IsSuccess = commandResult.IsSuccess,
                        ExecutedCommand = commandResult.ExecutedCommand,
                        Message = commandResult.OutputMessage
                    };
                    results.Add(entryResult);
                }
            }

            return results;
        }

        // <inheritdoc/>
        public bool TryOpenFile(string filePath, string workingDirectory, out string errorMessage)
        {
            errorMessage = string.Empty;
            try
            {
                string openCommand = ShellUtility.GetFileOpenCommand();
                Process process = ShellUtility.GetFileProcessingShell(openCommand, filePath, workingDirectory);
                process.Start();
                string output = process.StandardOutput.ReadToEnd();
                string error = process.StandardError.ReadToEnd();
                process.WaitForExit();
                if (process.ExitCode != 0)
                {
                    errorMessage = $"Command '{openCommand}' failed with error: {error}\n";
                    logger.LogError("Command '{command} {file}' failed to open. Sending error response.", openCommand, filePath);
                    return false;
                }
                 
                string successMessage = $"Command '{openCommand} {filePath}' executed successfully: {output}\n";
                logger.LogInformation(successMessage);
                return true;
            }
            catch (Exception ex)
            {
                ClientLogger.LogError("Error opening file and sending error response The error is: {message}", ex.Message);
                errorMessage = $"Error opening {filePath}. Check server logs for more information.";
                return false;
            }
        }

        // <inheritdoc/>
        public bool TryDiscardFileChanges(string workingDirectory, string filePath, out string errorMessage)
        {
            errorMessage = string.Empty;
            try
            {
                string discardCommand = "git restore";
                Process process = ShellUtility.GetFileProcessingShell(discardCommand, filePath, workingDirectory);
                process.Start();
                string output = process.StandardOutput.ReadToEnd();
                string error = process.StandardError.ReadToEnd();
                process.WaitForExit();
                if (process.ExitCode != 0)
                {
                    errorMessage = $"Command '{discardCommand}' failed with error: {error}\n";
                    logger.LogError("Command '{command} {file}' failed to discard changes. Sending error response.", discardCommand, filePath);
                    return false;
                }

                string successMessage = $"Command '{discardCommand} {filePath}' executed successfully: {output}\n";
                logger.LogInformation(successMessage);
                return true;
            }
            catch (Exception ex)
            {
                ClientLogger.LogError("Error discarding file changes and sending error response. The error is: {message}", ex.Message);
                errorMessage = $"Error discarding file changes for file: {filePath}. Check server logs for more information.";
                return false;
            }
        }
    }
}
