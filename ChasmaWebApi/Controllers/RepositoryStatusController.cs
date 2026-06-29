using ChasmaWebApi.Core.Interfaces.Control;
using ChasmaWebApi.Core.Interfaces.Infrastructure;
using ChasmaWebApi.Data;
using ChasmaWebApi.Data.Models;
using ChasmaWebApi.Data.Objects.Application;
using ChasmaWebApi.Data.Objects.Git;
using ChasmaWebApi.Data.Requests.Configuration;
using ChasmaWebApi.Data.Requests.Status;
using ChasmaWebApi.Data.Responses.Configuration;
using ChasmaWebApi.Data.Responses.Status;
using ChasmaWebApi.Util;
using LibGit2Sharp;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;

namespace ChasmaWebApi.Controllers
{
    /// <summary>
    /// Class representing git repository status routes.
    /// </summary>
    [Authorize]
    [ApiController]
    [Route("api/[controller]")]
    [EnableRateLimiting(ChasmaWebApiConfigurations.RateLimiterPolicy)]
    public class RepositoryStatusController : ControllerBase
    {
        /// <summary>
        /// The internal API logger.
        /// </summary>
        private readonly ILogger<RepositoryStatusController> logger;

        /// <summary>
        /// The application control service for managing application-level operations.
        /// </summary>
        private readonly IApplicationControlService applicationControlService;

        /// <summary>
        /// The internal cache manager.
        /// </summary>
        private readonly ICacheManager cacheManager;

        /// <summary>
        /// The application database context for interacting with the database.
        /// </summary>
        private readonly ApplicationDbContext applicationDbContext;

        #region Constructor

        /// <summary>
        /// Instantiates a new <see cref="RepositoryStatusController"/> class.
        /// </summary>
        /// <param name="log">The internal API logger.</param>
        /// <param name="controlService">The application control service.</param>
        /// <param name="apiCacheManager">The internal API cache manager.</param>
        /// <param name="dbContext">The application database context.</param>
        public RepositoryStatusController(ILogger<RepositoryStatusController> log, IApplicationControlService controlService, ICacheManager apiCacheManager, ApplicationDbContext dbContext)
        {
            logger = log;
            applicationControlService = controlService;
            cacheManager = apiCacheManager;
            applicationDbContext = dbContext;
        }

        #endregion

        /// <summary>
        /// Gets the git repository status for the specified repo ID.
        /// </summary>
        /// <param name="request">The git status request.</param>
        /// <returns>The git status response.</returns>
        [HttpPost]
        [Route("gitStatus")]
        public ActionResult<GitStatusResponse> GetRepoStatus([FromBody] GitStatusRequest request)
        {
            GitStatusResponse response = new();
            if (request == null)
            {
                logger.LogError("Null git status request received.");
                response.IsErrorResponse = true;
                response.ErrorMessage = "Request must be populated.";
                return BadRequest(response);
            }

            string repoId = request.RepositoryId;
            if (string.IsNullOrEmpty(repoId))
            {
                logger.LogError("Cannot process git status request because the repository identifier is null or empty.");
                response.IsErrorResponse = true;
                response.ErrorMessage = "The repository identifier is null or empty.";
                return Ok(response);
            }

            int userId = request.UserId;
            if (!cacheManager.Users.TryGetValue(userId, out ApplicationUser user))
            {
                logger.LogError("No user found in cache for user ID: {userId}. Cannot get repository status. Sending error response.", userId);
                response.IsErrorResponse = true;
                response.ErrorMessage = $"No user found in cache for user ID: {userId}. Cannot get repository status.";
                return Ok(response);
            }

            if (!cacheManager.Repositories.TryGetValue(repoId, out LocalGitRepository repository))
            {
                logger.LogError("Repository not found in cache when getting status for repo with identifier {id}. Sending error response.", repoId);
                response.IsErrorResponse = true;
                response.ErrorMessage = "Cannot get status because the repository cannot be found in system cache.";
                return Ok(response);
            }

            ChasmaWebApiConfigurations webApiConfigurations = ChasmaWebApiConfigurations.GetApiConfig();
            string token = RemoteHelper.GetApiToken(repository.HostPlatform, webApiConfigurations);
            string username = RemoteHelper.GetRemoteHostUsername(repository);
            logger.LogDebug("Received request to run git status for repository ID: {repoId}", repoId);
            RepositorySummary summary = applicationControlService.GetRepositoryStatus(repoId, username, token);
            if (summary == null)
            {
                logger.LogError("Failed to get repository status for repo ID: {repoId}", repoId);
                response.IsErrorResponse = true;
                response.ErrorMessage = $"Failed to get repository status for repo ID: {repoId}";
                return Ok(response);
            }

            response.RepositoryId = summary.RepositoryId;
            response.StatusElements = summary.StatusElements;
            response.CommitsAhead = summary.CommitsAhead;
            response.CommitsBehind = summary.CommitsBehind;
            response.BranchName = summary.BranchName;
            response.RemoteUrl = summary.RemoteUrl;
            response.CommitHash = summary.CommitHash;
            response.PullRequests = summary.PullRequests;
            response.LastUpdated = summary.LastUpdated;
            return Ok(response);
        }

        /// <summary>
        /// Applies the staging action to the specified repository and file.
        /// </summary>
        /// <param name="applyStagingActionRequest">The request to stage/unstage a file.</param>
        /// <returns>The updated status elements as a result of the operation.</returns>
        [HttpPost]
        [Route("applyStagingAction")]
        public ActionResult<ApplyStagingActionResponse> ApplyStagingAction([FromBody] ApplyStagingActionRequest applyStagingActionRequest)
        {
            ApplyStagingActionResponse response = new();
            if (applyStagingActionRequest == null)
            {
                logger.LogError("Null git add request received.");
                response.IsErrorResponse = true;
                response.ErrorMessage = "Request must be populated.";
                return BadRequest(response);
            }

            string repoId = applyStagingActionRequest.RepoKey;
            if (string.IsNullOrEmpty(repoId))
            {
                logger.LogError("Repository key must be populated.");
                response.IsErrorResponse = true;
                response.ErrorMessage = "Cannot process request because the repository key is not populated.";
                return Ok(response);
            }

            if (!cacheManager.Repositories.TryGetValue(repoId, out LocalGitRepository repository))
            {
                logger.LogError("Repository not found in cache when applying staging action for repo with identifier {id}. Sending error response.", repoId);
                response.IsErrorResponse = true;
                response.ErrorMessage = "Cannot process request because the repository cannot be found in cache.";
                return Ok(response);
            }

            if (string.IsNullOrEmpty(applyStagingActionRequest.FileName))
            {
                logger.LogError("File name must be populated.");
                response.IsErrorResponse = true;
                response.ErrorMessage = "Cannot process request because the file name is not populated.";
                return Ok(response);
            }

            int userId = applyStagingActionRequest.UserId;
            if (!cacheManager.Users.TryGetValue(userId, out ApplicationUser user))
            {
                logger.LogError("No user found in cache for user ID: {userId}. Cannot apply staging action. Sending error response.", userId);
                response.IsErrorResponse = true;
                response.ErrorMessage = $"No user found in cache for user ID: {userId}.";
                return Ok(response);
            }

            ChasmaWebApiConfigurations webApiConfigurations = ChasmaWebApiConfigurations.GetApiConfig();
            string token = RemoteHelper.GetApiToken(repository.HostPlatform, webApiConfigurations);
            string username = RemoteHelper.GetRemoteHostUsername(repository);
            string fileName = applyStagingActionRequest.FileName;
            bool isStaging = applyStagingActionRequest.IsStaging;
            List<RepositoryStatusElement>? statusElements = applicationControlService.ApplyStagingAction(repoId, fileName, isStaging, username, token);
            if (statusElements == null)
            {
                logger.LogError("Failed to apply staging action for repo ID: {repoId}", repoId);
                response.IsErrorResponse = true;
                response.ErrorMessage = $"Failed to apply staging action for repo ID: {repoId}. Check server logs for more information.";
                return Ok(response);
            }

            response.StatusElements = statusElements;
            return Ok(response);
        }

        /// <summary>
        /// Commits the local changes.
        /// </summary>
        /// <param name="request">The request to commit local changes.</param>
        /// <returns>Response to committing the changes.</returns>
        [HttpPost]
        [Route("gitCommit")]
        public ActionResult<GitCommitResponse> CommitChanges([FromBody] GitCommitRequest request)
        {
            logger.LogInformation("Received request to commit changes");
            GitCommitResponse response = new();
            if (request == null)
            {
                response.IsErrorResponse = true;
                response.ErrorMessage = "Null request received. Cannot commit changes.";
                logger.LogError("GitCommitRequest received is null. Sending error response");
                return BadRequest(response);
            }

            if (string.IsNullOrEmpty(request.RepositoryId))
            {
                response.IsErrorResponse = true;
                response.ErrorMessage = "Repository identifier must be populated. Cannot commit changes.";
                logger.LogError("Null or empty repository identifier received. Sending error response");
                return BadRequest(response);
            }

            if (string.IsNullOrEmpty(request.Email))
            {
                response.IsErrorResponse = true;
                response.ErrorMessage = "Email must be populated for commit signature. Cannot commit changes.";
                logger.LogError("Null or empty email received. Sending error response");
                return BadRequest(response);
            }

            if (string.IsNullOrEmpty(request.CommitMessage))
            {
                response.IsErrorResponse = true;
                response.ErrorMessage = "Commit message cannot be empty. Cannot commit changes.";
                logger.LogError("Null or empty commit message received. Sending error response");
                return BadRequest(response);
            }

            string repoId = request.RepositoryId;
            if (!cacheManager.WorkingDirectories.TryGetValue(repoId, out string workingDirectory))
            {
                response.IsErrorResponse = true;
                response.ErrorMessage = $"No working directory found in cache for {repoId}. Cannot commit changes.";
                logger.LogError("No working directory was found for repo identifier {repoId}. Sending error response", repoId);
                return BadRequest(response);
            }

            int userId = request.UserId;
            if (!cacheManager.Users.TryGetValue(userId, out ApplicationUser user))
            {
                response.IsErrorResponse = true;
                response.ErrorMessage = $"No user found in cache for user ID: {userId}. Cannot commit changes.";
                logger.LogError("No user was found for user ID: {userId}. Sending error response", userId);
                return BadRequest(response);
            }

            try
            {
                applicationControlService.CommitChanges(workingDirectory, user.Name, request.Email, request.CommitMessage);
                logger.LogInformation("Successfully committed changes to repo: {repoId}", repoId);
                return Ok(response);
            }
            catch (Exception e)
            {
                response.IsErrorResponse = true;
                response.ErrorMessage = $"Error committing changes to repo: {repoId}. Check server logs for more information.";
                logger.LogError(e, "Error committing changes to repo: {repoId}", repoId);
                return Ok(response);
            }
        }

        /// <summary>
        /// Pushes the committed changes to the remote repository.
        /// </summary>
        /// <param name="request">The request to push changes to a remote repository.</param>
        /// <returns>Git push response.</returns>
        [HttpPost]
        [Route("gitPush")]
        public ActionResult<GitPushResponse> PushChanges([FromBody] GitPushRequest request)
        {
            logger.LogInformation("Received request to push changes");
            GitPushResponse response = new();
            if (request == null)
            {
                response.IsErrorResponse = true;
                response.ErrorMessage = "Null request received. Cannot push changes.";
                logger.LogError("GitPushRequest received is null. Sending error response");
                return BadRequest(response);
            }

            if (string.IsNullOrEmpty(request.RepositoryId))
            {
                response.IsErrorResponse = true;
                response.ErrorMessage = "Repository identifier must be populated. Cannot push changes.";
                logger.LogError("Null or empty repository identifier received. Sending error response");
                return BadRequest(response);
            }

            string repoId = request.RepositoryId;
            if (!cacheManager.WorkingDirectories.TryGetValue(repoId, out string workingDirectory))
            {
                response.IsErrorResponse = true;
                response.ErrorMessage = $"No working directory found in cache for {repoId}. Cannot push changes.";
                logger.LogError("No working directory was found for repo identifier {repoId}. Sending error response", repoId);
                return BadRequest(response);
            }

            if (!cacheManager.Repositories.TryGetValue(repoId, out LocalGitRepository repository))
            {
                logger.LogError("Repository not found in cache when pushing changes for repo with identifier {id}. Sending error response.", repoId);
                response.IsErrorResponse = true;
                response.ErrorMessage = "Cannot get push changes because the repository cannot be found in system cache.";
                return Ok(response);
            }

            ChasmaWebApiConfigurations webApiConfigurations = ChasmaWebApiConfigurations.GetApiConfig();
            string token = RemoteHelper.GetApiToken(repository.HostPlatform, webApiConfigurations);
            if (!applicationControlService.TryPushChanges(workingDirectory, token, out string errorMessage))
            {
                response.IsErrorResponse = true;
                response.ErrorMessage = $"Failed to push changes to repo: {repoId}. {errorMessage}";
                logger.LogError("Failed to push changes to repo: {repoId}. {errorMessage}", repoId, errorMessage);
                return Ok(response);
            }

            logger.LogInformation("Successfully pushed changes to repo: {repoId}", repoId);
            return Ok(response);
        }

        /// <summary>
        /// Pulls the latest changes from the remote repository.
        /// </summary>
        /// <param name="request">Request to pull changes for the specified repository.</param>
        /// <returns>The git pull response.</returns>
        [HttpPost]
        [Route("gitPull")]
        public ActionResult<GitPullResponse> PullChanges([FromBody] GitPullRequest request)
        {
            logger.LogInformation("Received request to pull changes");
            GitPullResponse response = new();
            if (request == null)
            {
                response.IsErrorResponse = true;
                response.ErrorMessage = "Null request received. Cannot pull changes.";
                logger.LogError("GitPullRequest received is null. Sending error response");
                return BadRequest(response);
            }

            if (string.IsNullOrEmpty(request.RepositoryId))
            {
                response.IsErrorResponse = true;
                response.ErrorMessage = "Repository identifier must be populated. Cannot pull changes.";
                logger.LogError("Null or empty repository identifier received. Sending error response");
                return BadRequest(response);
            }

            string email = request.Email;
            if (string.IsNullOrEmpty(email))
            {
                response.IsErrorResponse = true;
                response.ErrorMessage = "User's email must be populated. Cannot pull changes.";
                logger.LogError("Null or empty user email received. Sending error response");
                return BadRequest(response);
            }

            string repoId = request.RepositoryId;
            if (!cacheManager.WorkingDirectories.TryGetValue(repoId, out string workingDirectory))
            {
                response.IsErrorResponse = true;
                response.ErrorMessage = $"No working directory found in cache for {repoId}. Cannot pull changes.";
                logger.LogError("No working directory was found for repo identifier {repoId}. Sending error response", repoId);
                return BadRequest(response);
            }

            int userId = request.UserId;
            if (!cacheManager.Users.TryGetValue(userId, out ApplicationUser user))
            {
                response.IsErrorResponse = true;
                response.ErrorMessage = $"No user found in cache for user ID: {userId}. Cannot pull changes.";
                logger.LogError("No user was found for user ID: {userId}. Sending error response", userId);
                return BadRequest(response);
            }

            if (!cacheManager.Repositories.TryGetValue(repoId, out LocalGitRepository repository))
            {
                logger.LogError("Repository not found in cache when pulling changes for repo with identifier {id}. Sending error response.", repoId);
                response.IsErrorResponse = true;
                response.ErrorMessage = "Cannot get pull changes because the repository cannot be found in system cache.";
                return Ok(response);
            }

            ChasmaWebApiConfigurations webApiConfigurations = ChasmaWebApiConfigurations.GetApiConfig();
            string token = RemoteHelper.GetApiToken(repository.HostPlatform, webApiConfigurations);
            string fullName = user.Name;
            if (!applicationControlService.TryPullChanges(workingDirectory, fullName, email, token, out string errorMessage))
            {
                response.IsErrorResponse = true;
                response.ErrorMessage = $"Failed to pull changes to repo: {repoId}. {errorMessage}";
                logger.LogError("Failed to pull changes to repo: {repoId}. {errorMessage}", repoId, errorMessage);
                return Ok(response);
            }

            logger.LogInformation("Successfully pulled changes to repo: {repoId}", repoId);
            return Ok(response);
        }

        /// <summary>
        /// Gets the git diff for the specified repository and file path.
        /// </summary>
        /// <param name="request">The request to run 'git diff' on the specified file.</param>
        /// <returns>The response to a 'git diff' request.</returns>
        [HttpPost]
        [Route("gitDiff")]
        public ActionResult<GitDiffResponse> GetGitDiff([FromBody] GitDiffRequest request)
        {
            GitDiffResponse response = new();
            if (request == null)
            {
                logger.LogError("Null {request} received. Sending error response", nameof(GitDiffRequest));
                response.IsErrorResponse = true;
                response.ErrorMessage = "Request must be populated.";
                return BadRequest(response);
            }

            string repoId = request.RepositoryId;
            if (string.IsNullOrEmpty(request.RepositoryId))
            {
                logger.LogError("Cannot process git diff request because the repository identifier is null or empty. Sending error response.");
                response.IsErrorResponse = true;
                response.ErrorMessage = "The repository identifier is null or empty.";
                return BadRequest(response);
            }

            if (!cacheManager.WorkingDirectories.TryGetValue(repoId, out string workingDirectory))
            {
                logger.LogError("There is no working directory found when trying to git diff for repository {id}. Sending error response.", repoId);
                response.IsErrorResponse = true;
                response.ErrorMessage = $"No working directory found in cache for {repoId}";
                return BadRequest(response);
            }

            string filePath = request.FilePath;
            if (string.IsNullOrEmpty(request.FilePath))
            {
                logger.LogError("Cannot process git diff request because the file path is null or empty.");
                response.IsErrorResponse = true;
                response.ErrorMessage = "The file path is null or empty.";
                return BadRequest(response);
            }

            logger.LogInformation("Received request to get git diff for repository ID: {repoId}, file path: {filePath}", repoId, filePath);
            bool isStaged = request.IsStaged;
            if (!applicationControlService.TryGetGitDiff(workingDirectory, filePath, isStaged, out string diffContent, out string errorMessage))
            {
                logger.LogError(errorMessage);
                response.IsErrorResponse = true;
                response.ErrorMessage = $"Failed to get git diff for repo ID: {repoId}, file path: {filePath}. Check server logs for more information.";
                return Ok(response);
            }

            response.DiffContent = diffContent;
            return Ok(response);
        }

        /// <summary>
        /// Resets the repository to the specified commit and updates the staging area.
        /// </summary>
        /// <param name="request">The request to reset changes.</param>
        /// <returns>The response to resetting changes.</returns>
        [HttpPost]
        [Route("gitReset")]
        public ActionResult<GitResetResponse> ResetChanges([FromBody] GitResetRequest request)
        {
            string requestName = nameof(GitResetRequest);
            logger.LogInformation("Received a {request}", requestName);
            GitResetResponse response = new();
            if (request == null)
            {
                response.IsErrorResponse = true;
                response.ErrorMessage = "Null request received. Cannot reset changes.";
                logger.LogError("{request} received is null. Sending error response", requestName);
                return BadRequest(response);
            }

            string repoId = request.RepositoryId;
            if (string.IsNullOrEmpty(repoId))
            {
                response.IsErrorResponse = true;
                response.ErrorMessage = "Repository identifier must be populated. Cannot reset changes.";
                logger.LogError("Null or empty repository identifier received when trying to reset changes. Sending error response");
                return Ok(response);
            }

            if (!cacheManager.WorkingDirectories.TryGetValue(repoId, out string workingDirectory))
            {
                response.IsErrorResponse = true;
                response.ErrorMessage = $"No working directory found. Cannot reset changes.";
                logger.LogError("No working directory was found for repo identifier {repoId} when trying to reset changes. Sending error response", repoId);
                return Ok(response);
            }

            string revParseSpec = request.RevParseSpec;
            ResetMode resetMode = request.ResetMode;
            if (!applicationControlService.TryResetRepository(workingDirectory, revParseSpec, resetMode, out string commitMessage, out string errorMessage))
            {
                response.IsErrorResponse = true;
                response.ErrorMessage = $"Failed to reset changes for repo: {errorMessage}";
                logger.LogError("Failed to reset changes for repo: {repoId}. {errorMessage}", repoId, errorMessage);
                return Ok(response);
            }

            logger.LogInformation("Successfully reset changes for repo: {repoId}", repoId);
            response.CommitMessage = commitMessage;
            return Ok(response);
        }

        /// <summary>
        /// Applies the staging action to the specified repository and its corresponding files.
        /// </summary>
        /// <param name="request">The request to stage/unstage multiple file.</param>
        /// <returns>The updated status elements as a result of the operation.</returns>
        [HttpPost]
        [Route("applyBulkStagingAction")]
        public ActionResult<ApplyBulkStagingActionResponse> ApplyBulkStagingAction([FromBody] ApplyBulkStagingActionRequest request)
        {
            ApplyStagingActionResponse response = new();
            if (request == null)
            {
                logger.LogError("Null git add request received when trying to apply bulk staging action. Sending error response.");
                response.IsErrorResponse = true;
                response.ErrorMessage = "Request must be populated.";
                return BadRequest(response);
            }

            string repoId = request.RepositoryId;
            if (string.IsNullOrEmpty(repoId))
            {
                logger.LogError("Repository identifier must be populated when applying bulk staging action. Sending error response.");
                response.IsErrorResponse = true;
                response.ErrorMessage = "Cannot process request because the repository key is not populated.";
                return Ok(response);
            }

            List<string> fileNames = request.FileNames;
            if (fileNames.Any(string.IsNullOrEmpty))
            {
                logger.LogError("Files name must be populated when applying bulk staging action. Sending error response.");
                response.IsErrorResponse = true;
                response.ErrorMessage = "Cannot process request because the one of the file names are not populated.";
                return Ok(response);
            }

            bool isStaging = request.IsStaging;
            List<RepositoryStatusElement>? statusElements = applicationControlService.ApplyBulkStagingAction(repoId, fileNames, isStaging);
            if (statusElements == null)
            {
                logger.LogError("Failed to apply staging actions for repo ID: {repoId}. Sending error response.", repoId);
                response.IsErrorResponse = true;
                response.ErrorMessage = $"Failed to apply staging actions for repo ID: {repoId}. Check server logs for more information.";
                return Ok(response);
            }

            response.StatusElements = statusElements;
            return Ok(response);
        }

        /// <summary>
        /// Gets the branch synchronization status for the specified branch.
        /// </summary>
        /// <param name="request">The request to get the branch sync status.</param>
        /// <returns>The response containing the branch sync status response.</returns>
        [HttpPost]
        [Route("getBranchSyncStatus")]
        public ActionResult<GetBranchSyncStatusResponse> GetBranchSyncStatus([FromBody] GetBranchSyncStatusRequest request)
        {
            string requestName = nameof(GetBranchSyncStatusRequest);
            logger.LogInformation("Received a {request}", requestName);
            GetBranchSyncStatusResponse response = new();
            if (request == null)
            {
                logger.LogError("{request} received is null. Sending error response", requestName);
                response.IsErrorResponse = true;
                response.ErrorMessage = "Null request received. Cannot get branch sync status.";
                return BadRequest(response);
            }

            int userId = request.UserId;
            if (!cacheManager.Users.TryGetValue(userId, out ApplicationUser user))
            {
                logger.LogError("Invalid {request}. Cannot get branch synchronization status because the user with {id} is unknown to the system. Sending error response.", requestName, userId);
                response.IsErrorResponse = true;
                response.ErrorMessage = "Cannot get branch sync status because the user is unknown";
                return Ok(response);
            }

            string branchName = request.BranchName;
            if (string.IsNullOrEmpty(branchName))
            {
                logger.LogError("Invalid {request} because the branch name was empty. Sending error response.", requestName);
                response.IsErrorResponse = true;
                response.ErrorMessage = "Cannot get branch sync status for an empty branch name.";
                return Ok(response);
            }

            List<BranchSyncStatus> branchSyncStatuses = applicationControlService.GetBranchSyncStatuses(branchName, cacheManager.Repositories.Values, cacheManager.WorkingDirectories);
            logger.LogInformation("Successfully retrieved branch sync status for branch: {branch}", branchName);
            response.BranchSyncStatuses = branchSyncStatuses;
            return Ok(response);
        }

        /// <summary>
        /// Restores the specified file to the last committed state, discarding any local changes.
        /// </summary>
        /// <param name="request">The request to restore a file.</param>
        /// <returns>The response to restoring a file.</returns>
        [HttpPost]
        [Route("restoreFile")]
        public ActionResult<GitRestoreResponse> RestoreFile([FromBody] GitRestoreRequest request)
        {
            GitRestoreResponse response = new();
            string requestName = nameof(GitRestoreRequest);
            if (request == null)
            {
                logger.LogError("Null {request} received. Sending error response.", requestName);
                response.IsErrorResponse = true;
                response.ErrorMessage = "Request must be populated.";
                return BadRequest(response);
            }

            RepositoryStatusElement selectedFile = request.SelectedFile;
            if (selectedFile == null)
            {
                logger.LogError("Selected file in {request} is null. Sending error response.", requestName);
                response.IsErrorResponse = true;
                response.ErrorMessage = "Selected file must be populated.";
                return Ok(response);
            }

            if (!applicationControlService.TryRestoringFile(selectedFile, out string errorMessage))
            {
                logger.LogError("Failed to restore file: {filePath} in repo: {repoId} because {error}. Sending error response.", selectedFile.FilePath, selectedFile.RepositoryId, errorMessage);
                response.IsErrorResponse = true;
                response.ErrorMessage = errorMessage;
                return Ok(response);
            }

            logger.LogInformation("Successfully restored file: {filePath} in repo: {repoId}", selectedFile.FilePath, selectedFile.RepositoryId);
            return Ok(response);
        }

        /// <summary>
        /// Adds a new workspace context snapshot based on the provided blueprints, which include the repository ID and optional intent note for each repository to be snapshotted.
        /// </summary>
        /// <param name="request">The add work context snapshot request.</param>
        /// <returns>The response to adding a workspace context.</returns>
        [HttpPost]
        [Route("addSnapshot")]
        public async Task<ActionResult<AddWorkContextSnapshotResponse>> AddWorkspaceSnapshot([FromBody] AddWorkContextSnapshotRequest request)
        {
            AddWorkContextSnapshotResponse response = new();
            string requestName = nameof(AddWorkContextSnapshotRequest);
            if (request == null)
            {
                logger.LogError("Null {request} received. Sending error response.", requestName);
                response.IsErrorResponse = true;
                response.ErrorMessage = "Request must be populated.";
                return BadRequest(response);
            }

            string snapshotDisplayName = request.SnapshotDisplayName;
            if (string.IsNullOrEmpty(snapshotDisplayName))
            {
                logger.LogError("Snapshot display name in {request} is empty. Sending error response.", requestName);
                response.IsErrorResponse = true;
                response.ErrorMessage = "Snapshot display name must be populated.";
                return Ok(response);
            }

            List<RepositorySnapshotBlueprint> blueprints = request.Blueprints;
            if (blueprints.Count == 0)
            {
                logger.LogError("There are no blueprints to process to create a workspace snapshot. Sending error response.");
                response.IsErrorResponse = true;
                response.ErrorMessage = "There is nothing to do.";
                return Ok(response);
            }

            int userId = request.UserId;
            if (!cacheManager.Users.TryGetValue(userId, out _))
            {
                logger.LogError("No user found in cache for user ID: {userId}. Cannot create workspace context. Sending error response.", userId);
                response.IsErrorResponse = true;
                response.ErrorMessage = $"No user found in cache for user ID: {request.UserId}. Cannot create workspace context.";
                return Ok(response);
            }

            string? snapshotNote = request.SnapshotNote;
            List<RepositorySnapshotAdditionResult> additionResults = applicationControlService.AddWorkContextSnapshot(userId, snapshotDisplayName, blueprints, snapshotNote, out WorkContextSnapshot snapshot);
            if (additionResults.Count == 0 || snapshot == null)
            {
                logger.LogError("Failed to create workspace snapshot for user ID: {userId}. Sending error response.", userId);
                response.IsErrorResponse = true;
                response.ErrorMessage = $"Failed to create workspace snapshot for user ID: {userId}. Check server logs for more information.";
                return Ok(response);
            }

            WorkContextSnapshotModel workContextSnapshotModel = new()
            {
                DisplayName = snapshot.DisplayName,
                SnapshotNote = snapshot.SnapshotNote,
                UserId = snapshot.UserId,
            };
            await applicationDbContext.WorkContextSnapshots.AddAsync(workContextSnapshotModel);
            await applicationDbContext.SaveChangesAsync();

            int snapshotId = workContextSnapshotModel.SnapshotId;
            snapshot.SnapshotId = snapshotId;
            foreach (RepsoitoryWorkContextSnapshotEntry repoSnapshot in snapshot.RepositorySnapshots)
            {
                repoSnapshot.SnapshotId = snapshot.SnapshotId;
                RepositoryWorkContextSnapshotModel repoWorkContextSnapshotModel = new()
                {
                    SnapshotId = repoSnapshot.SnapshotId,
                    RepositoryId = repoSnapshot.RepositoryId,
                    BranchName = repoSnapshot.BranchName,
                    CommitHash = repoSnapshot.CommitHash ?? string.Empty,
                    CreatedAt = repoSnapshot.CreatedAt,
                    StashMessage = repoSnapshot.StashMessage,
                    IntentNote = repoSnapshot.IntentNote
                };
                await applicationDbContext.RepositorySnapshots.AddAsync(repoWorkContextSnapshotModel);
            }

            await applicationDbContext.SaveChangesAsync();
            cacheManager.WorkContextSnapshots.TryAdd(snapshot.SnapshotId, snapshot);


            response.WorkContextSnapshot = snapshot;
            response.AdditionResults = additionResults;
            logger.LogInformation("Successfully created workspace context snapshot with identifier: {id}.", snapshot.SnapshotId);
            return Ok(response);
        }

        /// <summary>
        /// Loads the specified workspace context snapshot.
        /// </summary>
        /// <param name="request">The request to load the snapshot.</param>
        /// <returns>The response to loading a snapshot.</returns>
        [HttpPost]
        [Route("loadSnapshot")]
        public ActionResult<ApplyWorkContextSnapshotResponse> LoadWorkspaceSnapshot([FromBody] ApplyWorkContextSnapshotRequest request)
        {
            ApplyWorkContextSnapshotResponse response = new();
            string requestName = nameof(ApplyWorkContextSnapshotRequest);
            if (request == null)
            {
                logger.LogError("Null {request} received. Sending error response.", requestName);
                response.IsErrorResponse = true;
                response.ErrorMessage = "Request must be populated.";
                return BadRequest(response);
            }

            int snapshotId = request.SnapshotId;
            if (!cacheManager.WorkContextSnapshots.TryGetValue(snapshotId, out WorkContextSnapshot snapshot))
            {
                logger.LogError("No workspace context snapshot found in cache for snapshot ID: {snapshotId}. Sending error response.", snapshotId);
                response.IsErrorResponse = true;
                response.ErrorMessage = $"No workspace context snapshot found for snapshot ID: {snapshotId}.";
                return Ok(response);
            }

            response.AdditionResults = applicationControlService.ApplyWorkspaceContextSnapshot(snapshot);
            logger.LogInformation("Successfully loaded workspace context snapshot with ID: {snapshotId}.", snapshotId);
            return Ok(response);
        }

        /// <summary>
        /// Deletes the list of snapshots from the cache and database.
        /// </summary>
        /// <param name="request">The request to delete a list of snapshots from the system.</param>
        /// <returns>The response to deleting multiple snapshots.</returns>
        [HttpDelete]
        [Route("deleteSnapshot")]
        public async Task<ActionResult<DeleteWorkspaceSnapshotResponse>> DeleteWorkspaceSnapshots([FromBody] DeleteWorkspaceSnapshotRequest request)
        {
            DeleteWorkspaceSnapshotResponse response = new();
            string requestName = nameof(DeleteWorkspaceSnapshotRequest);
            if (request == null)
            {
                logger.LogError("{request} received is null. Sending error response.", requestName);
                response.IsErrorResponse = true;
                response.ErrorMessage = "Request must be populated.";
                return BadRequest(response);
            }

            List<int> snapshotIds = request.SnapshotIds;
            if (snapshotIds.Count == 0)
            {
                logger.LogError("No snapshot IDs were provided in the {request}. Sending error response.", requestName);
                response.IsErrorResponse = true;
                response.ErrorMessage = "No snapshot IDs were provided.";
                return Ok(response);
            }

            List<int> deletedSnapshotIds = [];
            foreach (int snapshotId in snapshotIds)
            {
                if (!cacheManager.WorkContextSnapshots.TryRemove(snapshotId, out _))
                {
                    continue;
                }

                WorkContextSnapshotModel snapshotModel = await applicationDbContext.WorkContextSnapshots.FirstOrDefaultAsync(i => i.SnapshotId == snapshotId);
                if (snapshotModel == null)
                {
                    continue;
                }

                List<RepositoryWorkContextSnapshotModel> repoSnapshots = await applicationDbContext.RepositorySnapshots
                    .Where(i => i.SnapshotId == snapshotId)
                    .ToListAsync();
                applicationDbContext.RepositorySnapshots.RemoveRange(repoSnapshots);
                await applicationDbContext.SaveChangesAsync();

                applicationDbContext.WorkContextSnapshots.Remove(snapshotModel);
                await applicationDbContext.SaveChangesAsync();
                deletedSnapshotIds.Add(snapshotId);
            }


            logger.LogInformation("Successfully deleted workspace context snapshot(s): {snapshotId}.", string.Join(", ", deletedSnapshotIds));
            response.SnapshotIds = deletedSnapshotIds;
            return Ok(response);
        }
    }
}
