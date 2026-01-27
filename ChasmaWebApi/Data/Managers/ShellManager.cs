using ChasmaWebApi.Data.Interfaces;
using ChasmaWebApi.Data.Objects;
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
        /// <summary>
        /// The console separator to delimit commands.
        /// </summary>
        private const string ConsoleSeparator = "\n-------------------------------------------------------------------------------------------------------\n\n";

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

                List<string> commandOutputs = ExecuteShellCommands(workingDirectory, entry.Commands);
                string repoName = CacheManager.Repositories.TryGetValue(repoId, out LocalGitRepository repository)
                    ? repository.Name
                    : repoId;
                BatchCommandEntryResult entryResult = new()
                {
                    RepositoryName = repoName,
                    IsSuccess = true,
                    Message = string.Join(ConsoleSeparator, commandOutputs)
                };
                results.Add(entryResult);
            }

            return results;
        }
    }
}
