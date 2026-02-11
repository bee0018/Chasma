using ChasmaWebApi.Data.Interfaces;
using ChasmaWebApi.Data.Objects;
using ChasmaWebApi.Data.Requests.Shell;
using ChasmaWebApi.Data.Responses.Shell;
using Microsoft.AspNetCore.Mvc;

namespace ChasmaWebApi.Controllers
{
    /// <summary>
    /// Class describing the controller used to interact with the system shell.
    /// </summary>
    /// <remarks>
    /// Initializes a new instance of the <see cref="ShellController"/> class.
    /// </remarks>
    /// <param name="internalLogger">The internal API logger.</param>
    /// <param name="manager">The internal API shell manager.</param>
    /// <param name="internalCacheManager">The internal API cache manager.</param>
    [Route("api/[controller]")]
    public class ShellController(ILogger<ShellController> internalLogger, IShellManager manager, ICacheManager internalCacheManager) : ControllerBase
    {
        /// <summary>
        /// The internal API logger.
        /// </summary>
        private readonly ILogger<ShellController> logger = internalLogger;

        /// <summary>
        /// The internal API shell manager.
        /// </summary>
        private readonly IShellManager shellManager = manager;

        /// <summary>
        /// The internal API cache manager.
        /// </summary>
        private readonly ICacheManager cacheManager = internalCacheManager;

        /// <summary>
        /// Executes custom shell commands on the current system's shell.
        /// </summary>
        /// <param name="request">The execute shell commands request.</param>
        /// <returns>The response summary of executing shell commands.</returns>
        [HttpPost]
        [Route("executeShellCommands")]
        public ActionResult<ExecuteShellCommandResponse> ExecuteShellCommands([FromBody] ExecuteShellCommandRequest request)
        {
            logger.LogInformation("Received {request} to execute shell commands.", nameof(ExecuteShellCommandRequest));
            ExecuteShellCommandResponse response = new();
            if (request == null)
            {
                logger.LogError("Received null request when attempting to execute shell commands. Sending error response.");
                response.IsErrorResponse = true;
                response.ErrorMessage = "Request is null. Cannot execute commands.";
                return BadRequest(response);
            }

            string repositoryId = request.RepositoryId;
            if (string.IsNullOrEmpty(repositoryId))
            {
                logger.LogError("RepositoryId is missing in the request. Sending error response.");
                response.IsErrorResponse = true;
                response.ErrorMessage = "RepositoryId is required to execute shell commands.";
                return BadRequest(response);
            }

            if (!cacheManager.WorkingDirectories.TryGetValue(repositoryId, out string workingDirectory))
            {
                logger.LogError("No working directory was found for repository {repoId}, so shell commands could not be sent. Sending error response.", repositoryId);
                response.IsErrorResponse = true;
                response.ErrorMessage = "No working directory was found for the specified repository.";
                return BadRequest(response);
            }

            List<string> commands = request.Commands;
            if (commands.Count == 0)
            {
                response.IsErrorResponse = true;
                response.ErrorMessage = "No shell commands were provided to execute.";
                logger.LogError("No shell commands provided in the request. Sending error response.");
                return BadRequest(response);
            }

            try
            {
                response.Results = shellManager.ExecuteShellCommands(workingDirectory, commands);
                logger.LogInformation("Successfully executed shell commands without any exceptions. Sending response.");
                return Ok(response);
            }
            catch (Exception ex)
            {
                response.IsErrorResponse = true;
                response.ErrorMessage = "Error executing shell commands. Check internal server logs for more information.";
                logger.LogError("An exception occurred while executing commands: {errorMessage}", ex.Message);
                return Ok(response);
            }
        }

        /// <summary>
        /// Executes batch shell commands on the current system's shell.
        /// </summary>
        /// <param name="request">The request to execute shell commands.</param>
        /// <returns>The output result as a result of the batch commands.</returns>
        [HttpPost]
        [Route("executeBatchShellCommands")]
        public ActionResult<ExecuteBatchShellCommandsResponse> ExecuteBatchShellCommands([FromBody] ExecuteBatchShellCommandsRequest request)
        {
            string requestName = nameof(ExecuteBatchShellCommandsRequest);
            logger.LogInformation("Received {request} to execute batch shell commands.", requestName);
            ExecuteBatchShellCommandsResponse response = new();
            if (request == null)
            {
                logger.LogError("Received null {request} when attempting to execute batch shell commands. Sending error response.", requestName);
                response.IsErrorResponse = true;
                response.ErrorMessage = "Request is null. Cannot execute batch commands.";
                return BadRequest(response);
            }

            if (request.BatchCommands.Count == 0)
            {
                logger.LogError("No batch shell commands provided in the {request}. Sending error response.", requestName);
                response.IsErrorResponse = true;
                response.ErrorMessage = "No batch shell commands were provided to execute.";
                return BadRequest(response);
            }

            try
            {
                List<BatchCommandEntry> batchCommands = request.BatchCommands;
                response.Results = shellManager.ExecuteShellCommandsInBatch(batchCommands);
                logger.LogInformation("Successfully executed shell commands without any exceptions. Sending successful response.");
                return Ok(response);
            }
            catch (Exception ex)
            {
                response.IsErrorResponse = true;
                response.ErrorMessage = "Error executing shell commands. Check internal server logs for more information.";
                logger.LogError("An exception occurred while executing commands: {errorMessage}", ex.Message);
                return Ok(response);
            }
        }

        /// <summary>
        /// Opens the specified file in the request.
        /// </summary>
        /// <param name="request">The open file request.</param>
        /// <returns>The response to opening a file.</returns>
        [HttpPost]
        [Route("openFile")]
        public ActionResult<OpenFileResponse> OpenFile([FromBody] OpenFileRequest request)
        {
            string requestName = nameof(OpenFileRequest);
            OpenFileResponse response = new();
            if (request == null)
            {
                logger.LogError("Received null {request}. Sending error response.", requestName);
                response.IsErrorResponse = true;
                response.ErrorMessage = "Request must be populated.";
                return BadRequest(response);
            }

            string repoId = request.RepositoryId;
            if (string.IsNullOrEmpty(repoId))
            {
                logger.LogError("Invalid {request}. Cannot open file because the repository identifier is null or empty. Sending error response.", requestName);
                response.IsErrorResponse = true;
                response.ErrorMessage = "The repository identifier is null or empty.";
                return Ok(response);
            }

            if (!cacheManager.WorkingDirectories.TryGetValue(repoId, out string workingDirectory))
            {
                logger.LogError("Invalid {request}. There is no working directory found when trying to open file for repository {id}. Sending error response.", requestName, repoId);
                response.IsErrorResponse = true;
                response.ErrorMessage = $"No working directory found in cache for {repoId}";
                return Ok(response);
            }

            string filePath = request.FilePath;
            if (string.IsNullOrEmpty(request.FilePath))
            {
                logger.LogError("Cannot process {request} because the file path is null or empty.", requestName);
                response.IsErrorResponse = true;
                response.ErrorMessage = "The file path is null or empty.";
                return Ok(response);
            }

            if (!shellManager.TryOpenFile(filePath, workingDirectory, out string errorMessage))
            {
                logger.LogError("Failed to open {file}. Sending error response.", filePath);
                response.IsErrorResponse = true;
                response.ErrorMessage = errorMessage;
                return Ok(response);
            }

            logger.LogInformation("Successfully opened file {file}.", filePath);
            return Ok(response);
        }

        /// <summary>
        /// Restores changes of the specified file.
        /// </summary>
        /// <param name="request">The request to discard changes.</param>
        /// <returns>The response to discarding file changes.</returns>
        [HttpPost]
        [Route("gitRestore")]
        public ActionResult<DiscardFileChangesResponse> GitRestore([FromBody] DiscardFileChangesRequest request)
        {
            string requestName = nameof(DiscardFileChangesRequest);
            DiscardFileChangesResponse response = new();
            if (request == null)
            {
                logger.LogError("Received null {request}. Sending error response.", requestName);
                response.IsErrorResponse = true;
                response.ErrorMessage = "Request must be populated.";
                return BadRequest(response);
            }

            string repoId = request.RepositoryId;
            if (string.IsNullOrEmpty(repoId))
            {
                logger.LogError("Invalid {request}. Cannot restore file because the repository identifier is null or empty. Sending error response.", requestName);
                response.IsErrorResponse = true;
                response.ErrorMessage = "The repository identifier is null or empty.";
                return Ok(response);
            }

            if (!cacheManager.WorkingDirectories.TryGetValue(repoId, out string workingDirectory))
            {
                logger.LogError("Invalid {request}. There is no working directory found when trying to open file for repository {id}. Sending error response.", requestName, repoId);
                response.IsErrorResponse = true;
                response.ErrorMessage = $"No working directory found in cache for {repoId}";
                return Ok(response);
            }

            string filePath = request.FilePath;
            if (string.IsNullOrEmpty(request.FilePath))
            {
                logger.LogError("Cannot restore {request} because the file path is null or empty.", requestName);
                response.IsErrorResponse = true;
                response.ErrorMessage = "The file path is null or empty.";
                return Ok(response);
            }

            if (!shellManager.TryDiscardFileChanges(workingDirectory, filePath, out string errorMessage))
            {
                logger.LogError("Failed to restore {file}. Sending error response.", filePath);
                response.IsErrorResponse = true;
                response.ErrorMessage = errorMessage;
                return Ok(response);
            }

            logger.LogInformation("Successfully restored file {file}.", filePath);
            return Ok(response);
        }
    }
}
