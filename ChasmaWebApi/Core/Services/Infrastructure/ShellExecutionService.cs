using ChasmaWebApi.Core.Interfaces.Infrastructure;
using ChasmaWebApi.Data.Objects;
using ChasmaWebApi.Util;
using System.Diagnostics;

namespace ChasmaWebApi.Core.Services.Infrastructure
{
    /// <summary>
    /// Class responsible for executing shell commands, specifically Git commands, and managing related operations within the application.
    /// </summary>
    public class ShellExecutionService : IShellExecutionService
    {
        /// <summary>
        /// The logger instance for logging diagnostic and operational information within class.
        /// </summary>
        private readonly ILogger<ShellExecutionService> Logger;

        /// <summary>
        /// The cache manager instance for accessing cached data such as repository information and working directories within the system.
        /// </summary>
        private readonly ICacheManager CacheManager;

        #region Constructor

        /// <summary>
        /// Initializes a new instance of the <see cref="ShellExecutionService"/> class.
        /// </summary>
        /// <param name="logger">The internal API logger.</param>
        /// <param name="cacheManager">The internal cache manager.</param>
        public ShellExecutionService(ILogger<ShellExecutionService> logger, ICacheManager cacheManager)
        {
            Logger = logger;
            CacheManager = cacheManager;
        }

        #endregion

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
                        Logger.LogError(errorMessage);
                    }
                    else
                    {
                        commandResult.IsSuccess = true;
                        commandResult.OutputMessage = output;
                        string successMessage = $"Command '{command}' executed successfully: {output}\n";
                        Logger.LogInformation(successMessage);
                    }

                    commandResults.Add(commandResult);
                }
            }
            catch (Exception ex)
            {
                string errorMessage = $"An exception occurred while executing commands: {ex.Message}";
                Logger.LogError(ex, errorMessage);
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
                    Logger.LogWarning("Repository ID {repoId} not found in working directories cache.", repoId);
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
    }
}
