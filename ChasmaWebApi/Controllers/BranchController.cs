using ChasmaWebApi.Core.Interfaces.Control;
using ChasmaWebApi.Core.Interfaces.Infrastructure;
using ChasmaWebApi.Data.Models;
using ChasmaWebApi.Data.Requests.Configuration;
using ChasmaWebApi.Data.Requests.Status;
using ChasmaWebApi.Data.Responses.Configuration;
using ChasmaWebApi.Data.Responses.Status;
using Microsoft.AspNetCore.Mvc;

namespace ChasmaWebApi.Controllers
{
    /// <summary>
    /// Class representing the branch controller for git branch related operations.
    /// </summary>
    [Route("api/[controller]")]
    public class BranchController : ControllerBase
    {
        /// <summary>
        /// The logger instance for logging diagnostic and operational information within the class.
        /// </summary>
        private readonly ILogger<BranchController> logger;

        /// <summary>
        /// The internal cache manager.
        /// </summary>
        private readonly ICacheManager cacheManager;

        /// <summary>
        /// The internal API application control service for managing application-level operations.
        /// </summary>
        private readonly IApplicationControlService applicationControlService;

        /// <summary>
        /// The Chasma Web API configurations.
        /// </summary>
        private readonly ChasmaWebApiConfigurations webApiConfigurations;

        /// <summary>
        /// Initializes a new instance of the <see cref="BranchController"/> class.
        /// </summary>
        /// <param name="log">The internal API logger.</param>
        /// <param name="controlService">The application control orchestrator.</param>
        /// <param name="apiCacheManager">The API cache manager.</param>
        /// <param name="apiConfig">The applications configuration.</param>
        public BranchController(ILogger<BranchController> log, IApplicationControlService controlService, ICacheManager apiCacheManager, ChasmaWebApiConfigurations apiConfig)
        {
            logger = log;
            applicationControlService = controlService;
            cacheManager = apiCacheManager;
            webApiConfigurations = apiConfig;
        }

        /// <summary>
        /// Adds the branch with the specified branch name to the repository with the specified repository identifier.
        /// </summary>
        /// <param name="request">The request to add a branch.</param>
        /// <returns>The response to adding a branch.</returns>
        [HttpPost]
        [Route("addBranch")]
        public ActionResult<AddNewBranchResponse> AddNewBranch([FromBody] AddNewBranchRequest request)
        {
            AddNewBranchResponse response = new();
            string requestName = nameof(AddNewBranchRequest);
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

            int userId = request.UserId;
            if (!cacheManager.Users.TryGetValue(userId, out UserAccountModel user))
            {
                logger.LogError("Invalid {request}. User with identifier {id} does not exist. Sending error response.", requestName, userId);
                response.IsErrorResponse = true;
                response.ErrorMessage = "Invalid request. Could not find user.";
                return Ok(response);
            }

            string branchName = request.BranchName;
            if (string.IsNullOrEmpty(branchName))
            {
                logger.LogError("Invalid {request}. Branch name is required. Sending error response.", requestName);
                response.IsErrorResponse = true;
                response.ErrorMessage = "Invalid request. Branch name is required.";
                return Ok(response);
            }

            string token = webApiConfigurations.GitHubApiToken;
            if (!applicationControlService.TryAddNewBranch(workingDirectory, branchName, user.UserName, token, out string errorMessage))
            {
                logger.LogError("Failed to create new branch {branchName} for repository {repoId}. Reason: {reason}", branchName, repoId, errorMessage);
                response.IsErrorResponse = true;
                response.ErrorMessage = errorMessage;
                return Ok(response);
            }

            if (request.IsCheckingOutNewBranch && !applicationControlService.TryCheckoutBranch(workingDirectory, branchName, out string checkoutErrorMessage))
            {
                logger.LogError("Successfully created branch but failed to checkout to new branch {branchName} for repository {repoId}. Reason: {reason}", branchName, repoId, checkoutErrorMessage);
                response.IsErrorResponse = true;
                response.ErrorMessage = $"Branch {branchName} was created successfully but failed to checkout. Reason: {checkoutErrorMessage}";
                return Ok(response);
            }

            logger.LogInformation("Successfully created new branch {branchName} for repository {repoId}.", branchName, repoId);
            return Ok(response);
        }

        /// <summary>
        /// Deletes the branch from the specified repository.
        /// </summary>
        /// <param name="request">The request to delete a branch.</param>
        /// <returns>Response to deleting a branch.</returns>
        [HttpDelete]
        [Route("deleteBranch")]
        public ActionResult<DeleteBranchResponse> DeleteBranch([FromBody] DeleteBranchRequest request)
        {
            DeleteBranchResponse response = new();
            if (request == null)
            {
                logger.LogError("Received a null {request}. Sending error response.", nameof(DeleteBranchRequest));
                response.IsErrorResponse = true;
                response.ErrorMessage = "Null request received. Request must be populated.";
                return BadRequest(response);
            }

            string repoId = request.RepositoryId;
            if (string.IsNullOrEmpty(repoId))
            {
                logger.LogError("Invalid request. Repository identifier is required. Sending error response.");
                response.IsErrorResponse = true;
                response.ErrorMessage = "Empty repository identifier received. Field must be populated.";
                return BadRequest(response);
            }

            string branchName = request.BranchName;
            if (string.IsNullOrEmpty(branchName))
            {
                logger.LogError("Invalid request. Branch name is required. Sending error response.");
                response.IsErrorResponse = true;
                response.ErrorMessage = "Empty branch name received. Field must be populated.";
                return BadRequest(response);
            }

            if (!applicationControlService.TryDeleteExistingBranch(repoId, branchName, out string errorMessage))
            {
                logger.LogError("Failure to delete branch: {reason}", errorMessage);
                response.IsErrorResponse = true;
                response.ErrorMessage = errorMessage;
                return Ok(response);
            }

            logger.LogInformation("Successfully deleted the branch {branchName}.", branchName);
            return Ok(response);
        }

        /// <summary>
        /// Checks out the specified branch for the repository.
        /// </summary>
        /// <param name="request">The request containing the details to checkout the branch.</param>
        /// <returns>The response to checking out a branch.</returns>
        [HttpPost]
        [Route("gitCheckout")]
        public ActionResult<GitCheckoutResponse> CheckoutBranch([FromBody] GitCheckoutRequest request)
        {
            logger.LogInformation("Received request to checkout branch");
            GitCheckoutResponse response = new();
            if (request == null)
            {
                response.IsErrorResponse = true;
                response.ErrorMessage = "Null request received. Cannot checkout branch.";
                logger.LogError("GitCheckoutBranchRequest received is null. Sending error response");
                return BadRequest(response);
            }

            if (string.IsNullOrEmpty(request.RepositoryId))
            {
                response.IsErrorResponse = true;
                response.ErrorMessage = "Repository identifier must be populated. Cannot checkout branch.";
                logger.LogError("Null or empty repository identifier received. Sending error response");
                return BadRequest(response);
            }

            string repoId = request.RepositoryId;
            if (!cacheManager.WorkingDirectories.TryGetValue(repoId, out string workingDirectory))
            {
                response.IsErrorResponse = true;
                response.ErrorMessage = $"No working directory found in cache for {repoId}. Cannot checkout branch.";
                logger.LogError("No working directory was found for repo identifier {repoId}. Sending error response", repoId);
                return BadRequest(response);
            }

            try
            {
                if (!applicationControlService.TryCheckoutBranch(workingDirectory, request.BranchName, out string errorMessage))
                {
                    response.IsErrorResponse = true;
                    response.ErrorMessage = $"Failed to checkout branch to repo: {repoId}. {errorMessage}";
                    logger.LogError("Failed to checkout branch to repo: {repoId}. {errorMessage}", repoId, errorMessage);
                    return Ok(response);
                }

                logger.LogInformation("Successfully checked out branch to repo: {repoId}", repoId);
                return Ok(response);
            }
            catch (Exception e)
            {
                response.IsErrorResponse = true;
                response.ErrorMessage = $"Error checking out branch to repo: {repoId}. Check server logs for more information.";
                logger.LogError(e, "Error checking out branch to repo: {repoId}", repoId);
                return Ok(response);
            }
        }

        /// <summary>
        /// Gets the branches for the specified repository.
        /// </summary>
        /// <param name="request">The request containing the repository details.</param>
        /// <returns>Response containing all the branches.</returns>
        [HttpPost]
        [Route("gitBranch")]
        public ActionResult<GitBranchResponse> GetBranches([FromBody] GitBranchRequest request)
        {
            GitBranchResponse response = new();
            if (request == null)
            {
                response.IsErrorResponse = true;
                response.ErrorMessage = "Null request received. Cannot get branches.";
                logger.LogError("GitBranchRequest received is null. Sending error response");
                return BadRequest(response);
            }

            if (string.IsNullOrEmpty(request.RepositoryId))
            {
                response.IsErrorResponse = true;
                response.ErrorMessage = "Repository identifier must be populated. Cannot get branches.";
                logger.LogError("Null or empty repository identifier received. Sending error response");
                return BadRequest(response);
            }

            string repoId = request.RepositoryId;
            if (!cacheManager.WorkingDirectories.TryGetValue(repoId, out string workingDirectory))
            {
                response.IsErrorResponse = true;
                response.ErrorMessage = $"No working directory found in cache for {repoId}. Cannot get branches.";
                logger.LogError("No working directory was found for repo identifier {repoId}. Sending error response", repoId);
                return BadRequest(response);
            }

            try
            {
                List<string> branchNames = applicationControlService.GetAllBranchesForRepository(workingDirectory);
                response.BranchNames.AddRange(branchNames);
                logger.LogInformation("Successfully retrieved branches for repo: {repoId}", repoId);
                return Ok(response);
            }
            catch (Exception e)
            {
                response.IsErrorResponse = true;
                response.ErrorMessage = $"Error getting branches for repo: {repoId}. Check server logs for more information.";
                logger.LogError(e, "Error getting branches for repo: {repoId}", repoId);
                return Ok(response);
            }
        }

        /// <summary>
        /// Merges the specified branch into the current branch for the repository.
        /// </summary>
        /// <param name="request">The request to merge one branch into another.</param>
        /// <returns>The response to the merge.</returns>
        [HttpPost]
        [Route("gitMerge")]
        public ActionResult<GitMergeResponse> MergeBranch([FromBody] GitMergeRequest request)
        {
            string requestName = nameof(GitMergeRequest);
            logger.LogInformation("Received a {request}", requestName);
            GitMergeResponse response = new();
            if (request == null)
            {
                response.IsErrorResponse = true;
                response.ErrorMessage = "Null request received. Cannot merge branch.";
                logger.LogError("{request} received is null. Sending error response", requestName);
                return BadRequest(response);
            }

            string repoId = request.RepositoryId;
            if (string.IsNullOrEmpty(repoId))
            {
                response.IsErrorResponse = true;
                response.ErrorMessage = "Repository identifier must be populated. Cannot merge branch.";
                logger.LogError("Null or empty repository identifier received when trying to merge branch. Sending error response");
                return Ok(response);
            }

            if (!cacheManager.WorkingDirectories.TryGetValue(repoId, out string workingDirectory))
            {
                response.IsErrorResponse = true;
                response.ErrorMessage = $"No working directory found in cache for {repoId}. Cannot merge branch.";
                logger.LogError("No working directory was found for repo identifier {repoId} when trying to merge branch. Sending error response", repoId);
                return Ok(response);
            }

            int userId = request.UserId;
            if (!cacheManager.Users.TryGetValue(userId, out UserAccountModel user))
            {
                response.IsErrorResponse = true;
                response.ErrorMessage = $"No user found in cache for user ID: {request.UserId}. Cannot merge branch.";
                logger.LogError("No user was found for user ID: {userId} when trying to merge branch. Sending error response", request.UserId);
                return Ok(response);
            }

            try
            {
                string sourceBranchName = request.SourceBranch;
                string destinationBranchName = request.DestinationBranch;
                string token = webApiConfigurations.GitHubApiToken;
                if (!applicationControlService.TryMergeChanges(workingDirectory, sourceBranchName, destinationBranchName, user.Name, user.Email, token, out string errorMessage))
                {
                    response.IsErrorResponse = true;
                    response.ErrorMessage = errorMessage;
                    logger.LogError("Failed to merge branch to repo: {repoId}. {errorMessage}", repoId, errorMessage);
                    return Ok(response);
                }

                logger.LogInformation("Successfully merged branch to repo: {repoId}", repoId);
                return Ok(response);
            }
            catch (Exception e)
            {
                response.IsErrorResponse = true;
                response.ErrorMessage = $"Error merging branch to repo: {repoId}. Check server logs for more information.";
                logger.LogError(e, "Error merging branch to repo: {repoId}", repoId);
                return Ok(response);
            }
        }
    }
}
