using ChasmaWebApi.Core.Interfaces.Control;
using ChasmaWebApi.Core.Interfaces.Infrastructure;
using ChasmaWebApi.Data.Objects.Application;
using ChasmaWebApi.Data.Objects.Git;
using ChasmaWebApi.Data.Requests.Configuration;
using ChasmaWebApi.Data.Responses.Configuration;
using LibGit2Sharp;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ChasmaWebApi.Controllers
{
    /// <summary>
    /// Class representing the controller used to interact with Git stash entries in the repositories.
    /// </summary>
    [Authorize]
    [ApiController]
    [Route("api/[controller]")]
    public class StashController : ControllerBase
    {
        /// <summary>
        /// The internal repository configuration manager for managing git repository configurations and operations.
        /// </summary>
        private readonly ILogger<StashController> logger;

        /// <summary>
        /// The internal repository configuration manager for managing git repository configurations and operations.
        /// </summary>
        private readonly ICacheManager cacheManager;

        /// <summary>
        /// The internal API application control service for managing application-level operations.
        /// </summary>
        private readonly IApplicationControlService applicationControlService;

        /// <summary>
        /// Initializes a new instance of the <see cref="StashController"/> class.
        /// </summary>
        /// <param name="log">The internal API logger.</param>
        /// <param name="apiCacheManager">The internal API cache manager.</param>
        /// <param name="controlService">The application's control service.</param>
        public StashController(ILogger<StashController> log, ICacheManager apiCacheManager, IApplicationControlService controlService)
        {
            logger = log;
            cacheManager = apiCacheManager;
            applicationControlService = controlService;
        }

        /// <summary>
        /// Stashes the changes in the repository with the specified repository identifier.
        /// </summary>
        /// <param name="request">The request to stash changes.</param>
        /// <returns>The response to adding a new stash entry.</returns>
        [HttpPost]
        [Route("gitStash")]
        public ActionResult<AddStashResponse> GitStash([FromBody] AddStashRequest request)
        {
            AddStashResponse response = new();
            if (request == null)
            {
                logger.LogError("Received a null {request}. Sending error response.", nameof(AddStashRequest));
                response.IsErrorResponse = true;
                response.ErrorMessage = "Request must be populated.";
                return BadRequest(response);
            }

            string repoId = request.RepositoryId;
            if (string.IsNullOrEmpty(repoId))
            {
                logger.LogError("Invalid {request}. Repository identifier is required. Sending error response.", nameof(AddStashRequest));
                response.IsErrorResponse = true;
                response.ErrorMessage = "Invalid request. Repository identifier is required.";
                return Ok(response);
            }

            if (!cacheManager.WorkingDirectories.TryGetValue(repoId, out string workingDirectory))
            {
                logger.LogError("Invalid {request}. Working directory with identifier {id} does not exist. Sending error response.", nameof(AddStashRequest), repoId);
                response.IsErrorResponse = true;
                response.ErrorMessage = "Invalid request. Could not find working directory for selected repository.";
                return Ok(response);
            }

            int userId = request.UserId;
            if (!cacheManager.Users.TryGetValue(userId, out ApplicationUser user))
            {
                logger.LogError("Invalid {request}. User with identifier {id} does not exist. Sending error response.", nameof(AddStashRequest), userId);
                response.IsErrorResponse = true;
                response.ErrorMessage = "Invalid request. Could not find user.";
                return Ok(response);
            }

            string stashMessage = request.Message;
            StashModifiers stashOptions = request.StashModifier;
            if (!applicationControlService.TryAddStash(workingDirectory, user, stashMessage, stashOptions, out string errorMessage))
            {
                logger.LogError("Failed to create git stash for repository {repoId}. Reason: {reason}", repoId, errorMessage);
                response.IsErrorResponse = true;
                response.ErrorMessage = errorMessage;
            }

            logger.LogInformation("Successfully created git stash for repository {repoId}.", repoId);
            return Ok(response);
        }

        /// <summary>
        /// Gets the list of stash entries for the repository with the specified repository identifier.
        /// </summary>
        /// <param name="request">The request to get the stash list of the repository.</param>
        /// <returns>The response containing the stash list data.</returns>
        [HttpPost]
        [Route("getStashList")]
        public ActionResult<GetStashListResponse> GetStashList([FromBody] GetStashListRequest request)
        {
            GetStashListResponse response = new();
            string requestName = nameof(GetStashListRequest);
            if (request == null)
            {
                logger.LogError("Received a null {request}. Sending error response.", requestName);
                response.IsErrorResponse = true;
                response.ErrorMessage = "Request must be populated.";
                return BadRequest(response);
            }

            string repoId = request.RepositoryId;
            if (string.IsNullOrEmpty(repoId))
            {
                logger.LogError("Invalid {request}. Repository identifier is required. Sending error response.", requestName);
                response.IsErrorResponse = true;
                response.ErrorMessage = "Invalid request. Repository identifier is required.";
                return Ok(response);
            }

            if (!cacheManager.WorkingDirectories.TryGetValue(repoId, out string workingDirectory))
            {
                logger.LogError("Invalid {request}. Working directory with identifier {id} does not exist. Sending error response.", requestName, repoId);
                response.IsErrorResponse = true;
                response.ErrorMessage = "Invalid request. Could not find working directory for selected repository.";
                return Ok(response);
            }

            List<StashEntry>? stashEntries = applicationControlService.GetStashList(workingDirectory, out string errorMessage);
            if (stashEntries == null)
            {
                logger.LogError("Failed to retrieve stash list for repository {repoId}. Reason: {reason}", repoId, errorMessage);
                response.IsErrorResponse = true;
                response.ErrorMessage = errorMessage;
                return Ok(response);
            }

            response.StashList = stashEntries;
            logger.LogInformation("Successfully retrieved stash list for repository {repoId}.", repoId);
            return Ok(response);
        }

        /// <summary>
        /// Gets the details of the specified stash entry from the repository with the specified repository identifier.
        /// </summary>
        /// <param name="request">The request to get the stash details</param>
        /// <returns>The response to get the stash details</returns>
        [HttpPost]
        [Route("getStashDetails")]
        public ActionResult<GetStashDetailsResponse> GetStashDetails([FromBody] GetStashDetailsRequest request)
        {
            GetStashDetailsResponse response = new();
            string requestName = nameof(GetStashDetailsRequest);
            if (request == null)
            {
                logger.LogError("Received a null {request}. Sending error response.", requestName);
                response.IsErrorResponse = true;
                response.ErrorMessage = "Request must be populated.";
                return BadRequest(response);
            }

            string repoId = request.RepositoryId;
            if (string.IsNullOrEmpty(repoId))
            {
                logger.LogError("Invalid {request}. Repository identifier is required. Sending error response.", requestName);
                response.IsErrorResponse = true;
                response.ErrorMessage = "Invalid request. Repository identifier is required.";
                return Ok(response);
            }

            if (!cacheManager.WorkingDirectories.TryGetValue(repoId, out string workingDirectory))
            {
                logger.LogError("Invalid {request}. Working directory with identifier {id} does not exist. Sending error response.", requestName, repoId);
                response.IsErrorResponse = true;
                response.ErrorMessage = "Invalid request. Could not find working directory for selected repository.";
                return Ok(response);
            }

            StashEntry stashEntry = request.StashEntry;
            List<PatchEntry>? patchEntries = applicationControlService.GetStashDetails(workingDirectory, stashEntry, out string errorMessage);
            if (patchEntries == null)
            {
                logger.LogError("Failed to retrieve stash details for repository {repoId}. Reason: {reason}", repoId, errorMessage);
                response.IsErrorResponse = true;
                response.ErrorMessage = errorMessage;
                return Ok(response);
            }

            response.PatchEntries = patchEntries;
            logger.LogInformation("Successfully retrieved stash details for repository {repoId}.", repoId);
            return Ok(response);
        }

        /// <summary>
        /// Applies the specified stash entry to the repository with the specified repository identifier.
        /// </summary>
        /// <param name="request">The request to apply a stash.</param>
        /// <returns>A response to apply a stash.</returns>
        [HttpPost]
        [Route("applyStash")]
        public ActionResult<ApplyStashResponse> ApplyStash([FromBody] ApplyStashRequest request)
        {
            ApplyStashResponse response = new();
            string requestName = nameof(ApplyStashRequest);
            if (request == null)
            {
                logger.LogError("Received a null {request}. Sending error response.", requestName);
                response.IsErrorResponse = true;
                response.ErrorMessage = "Request must be populated.";
                return BadRequest(response);
            }

            string repoId = request.RepositoryId;
            if (string.IsNullOrEmpty(repoId))
            {
                logger.LogError("Invalid {request}. Repository identifier is required. Sending error response.", requestName);
                response.IsErrorResponse = true;
                response.ErrorMessage = "Invalid request. Repository identifier is required.";
                return Ok(response);
            }

            if (!cacheManager.WorkingDirectories.TryGetValue(repoId, out string workingDirectory))
            {
                logger.LogError("Invalid {request}. Working directory with identifier {id} does not exist. Sending error response.", requestName, repoId);
                response.IsErrorResponse = true;
                response.ErrorMessage = "Invalid request. Could not find working directory for selected repository.";
                return Ok(response);
            }

            int stashIndex = request.StashIndex;
            StashApplyModifiers applyOptions = request.ApplyStashModifier;
            if (!applicationControlService.TryApplyStash(workingDirectory, stashIndex, applyOptions, out string errorMessage))
            {
                logger.LogError("Failed to apply stash entry at index {index} for repository {repoId}. Reason: {reason}", stashIndex, repoId, errorMessage);
                response.IsErrorResponse = true;
                response.ErrorMessage = errorMessage;
                return Ok(response);
            }

            logger.LogInformation("Successfully applied stash entry at index {index} for repository {repoId}.", stashIndex, repoId);
            return Ok(response);
        }

        /// <summary>
        /// Deletes the specified stash entry from the repository with the specified repository identifier.
        /// </summary>
        /// <param name="request">The request to delete a stash.</param>
        /// <returns>The response to delete a stash.</returns>
        [HttpDelete]
        [Route("deleteStash")]
        public ActionResult<DeleteStashResponse> DeleteStash([FromBody] DeleteStashRequest request)
        {
            DeleteStashResponse response = new();
            string requestName = nameof(DeleteStashRequest);
            if (request == null)
            {
                logger.LogError("Received a null {request}. Sending error response.", requestName);
                response.IsErrorResponse = true;
                response.ErrorMessage = "Request must be populated.";
                return BadRequest(response);
            }

            string repoId = request.RepositoryId;
            if (string.IsNullOrEmpty(repoId))
            {
                logger.LogError("Invalid {request}. Repository identifier is required. Sending error response.", requestName);
                response.IsErrorResponse = true;
                response.ErrorMessage = "Invalid request. Repository identifier is required.";
                return Ok(response);
            }

            if (!cacheManager.WorkingDirectories.TryGetValue(repoId, out string workingDirectory))
            {
                logger.LogError("Invalid {request}. Working directory with identifier {id} does not exist. Sending error response.", requestName, repoId);
                response.IsErrorResponse = true;
                response.ErrorMessage = "Invalid request. Could not find working directory for selected repository.";
                return Ok(response);
            }

            int stashIndex = request.StashIndex;
            if (!applicationControlService.TryRemoveStash(workingDirectory, stashIndex, out string errorMessage))
            {
                logger.LogError("Failed to delete stash entry at index {index} for repository {repoId}. Reason: {reason}", stashIndex, repoId, errorMessage);
                response.IsErrorResponse = true;
                response.ErrorMessage = errorMessage;
                return Ok(response);
            }

            logger.LogInformation("Successfully deleted stash entry at index {index} for repository {repoId}.", stashIndex, repoId);
            return Ok(response);
        }
    }
}
