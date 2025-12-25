using ChasmaWebApi.Data;
using ChasmaWebApi.Data.Interfaces;
using ChasmaWebApi.Data.Models;
using ChasmaWebApi.Data.Objects;
using ChasmaWebApi.Data.Requests;
using ChasmaWebApi.Data.Responses;
using Microsoft.AspNetCore.Mvc;

namespace ChasmaWebApi.Controllers
{
    /// <summary>
    /// Class representing git repository status routes.
    /// </summary>
    [Route("api/[controller]")]
    public class RepositoryStatusController : ControllerBase
    {
        /// <summary>
        /// The internal API logger.
        /// </summary>
        private readonly ILogger<RepositoryStatusController> logger;

        /// <summary>
        /// The internal repository status manager.
        /// </summary>
        private readonly IRepositoryStatusManager statusManager;

        /// <summary>
        /// The internal cache manager.
        /// </summary>
        private readonly ICacheManager cacheManager;

        /// <summary>
        /// The API configurations options.
        /// </summary>
        private readonly ChasmaWebApiConfigurations webApiConfigurations;

        /// <summary>
        /// The database context used for interacting with the database.
        /// </summary>
        private readonly ApplicationDbContext applicationDbContext;

        #region Constructor

        /// <summary>
        /// Instantiates a new <see cref="RepositoryStatusController"/> class.
        /// </summary>
        /// <param name="log">The internal API logger.</param>
        /// <param name="config">The API configurations.</param>
        /// <param name="manager">The repository status manager.</param>
        /// <param name="apiCacheManager">The internal API cache manager.</param>
        /// <param name="dbContext">The application database context.</param>
        public RepositoryStatusController(ILogger<RepositoryStatusController> log, ChasmaWebApiConfigurations config, IRepositoryStatusManager manager, ICacheManager apiCacheManager, ApplicationDbContext dbContext)
        {
            logger = log;
            webApiConfigurations = config;
            statusManager = manager;
            cacheManager = apiCacheManager;
            applicationDbContext = dbContext;
            PopulateCache();
        }

        #endregion

        /// <summary>
        /// Gets the workflow results via the GitHub API.
        /// </summary>
        /// <param name="request">The request to get workflow run results.</param>
        /// <returns>The workflow results.</returns>
        [HttpPost]
        [Route("workflowRuns")]
        public ActionResult<GitHubWorkflowRunResponse> GetChasmaWorkflowResults([FromBody] GetWorkflowResultsRequest request)
        {
            GitHubWorkflowRunResponse gitHubWorkflowRunResponse = new();
            if (request == null || string.IsNullOrEmpty(request.RepositoryName) || string.IsNullOrEmpty(request.RepositoryOwner))
            {
                logger.LogError("Invalid request received to get workflow run results.");
                gitHubWorkflowRunResponse.IsErrorResponse = true;
                gitHubWorkflowRunResponse.ErrorMessage = "Invalid request. Repository name and owner are required.";
                return BadRequest(gitHubWorkflowRunResponse);
            }

            logger.LogInformation("Attempting to get workflow data for the last {threshold} builds for {repoName}.", webApiConfigurations.WorkflowRunReportThreshold, request.RepositoryName);
            string token = webApiConfigurations.GitHubApiToken;
            string repoOwner = request.RepositoryOwner;
            string repoName = request.RepositoryName;
            int buildCount = webApiConfigurations.WorkflowRunReportThreshold;
            try
            {
                bool runsRetrieved = statusManager.TryGetWorkflowRunResults(repoName, repoOwner, token, buildCount, out List<WorkflowRunResult> runResults, out string errorMessage);
                if (!runsRetrieved && !string.IsNullOrEmpty(errorMessage))
                {
                    gitHubWorkflowRunResponse.IsErrorResponse = true;
                    gitHubWorkflowRunResponse.ErrorMessage = errorMessage;
                    return Ok(gitHubWorkflowRunResponse);
                }

                gitHubWorkflowRunResponse.RepositoryName = repoName;
                gitHubWorkflowRunResponse.WorkflowRunResults.AddRange(runResults);
                logger.LogInformation("Retrieved latest {count} build runs from {repo}.", runResults.Count, repoName);
                return Ok(gitHubWorkflowRunResponse);
            }
            catch
            {
                gitHubWorkflowRunResponse.IsErrorResponse = true;
                gitHubWorkflowRunResponse.ErrorMessage = $"Error fetching workflow runs from {repoName}. Check server logs for more information.";
                return BadRequest(gitHubWorkflowRunResponse);
            }
        }

        /// <summary>
        /// Gets the git repository status for the specified repo ID.
        /// </summary>
        /// <param name="gitStatusRequest">The git status request.</param>
        /// <returns>The git status response.</returns>
        [HttpPost]
        [Route("gitStatus")]
        public ActionResult<GitStatusResponse> GetRepoStatus([FromBody] GitStatusRequest gitStatusRequest)
        {
            GitStatusResponse gitStatusResponse = new();
            if (gitStatusRequest == null)
            {
                logger.LogError("Null git status request received.");
                gitStatusResponse.IsErrorResponse = true;
                gitStatusResponse.ErrorMessage = "Request must be populated.";
                return BadRequest(gitStatusResponse);
            }

            if (string.IsNullOrEmpty(gitStatusRequest.RepositoryId))
            {
                logger.LogError("Cannot process git status request because the repository identifier is null or empty.");
                gitStatusResponse.IsErrorResponse = true;
                gitStatusResponse.ErrorMessage = "The repository identifier is null or empty.";
                return BadRequest(gitStatusResponse);
            }

            string repoId = gitStatusRequest.RepositoryId;
            logger.LogInformation("Received request to run git status for repository ID: {repoId}", repoId);
            RepositorySummary summary = statusManager.GetRepositoryStatus(repoId);
            if (summary == null)
            {
                logger.LogError("Failed to get repository status for repo ID: {repoId}", repoId);
                gitStatusResponse.IsErrorResponse = true;
                gitStatusResponse.ErrorMessage = $"Failed to get repository status for repo ID: {repoId}";
                return BadRequest(gitStatusResponse);
            }

            gitStatusResponse.StatusElements = summary.StatusElements;
            gitStatusResponse.CommitsAhead = summary.CommitsAhead;
            gitStatusResponse.CommitsBehind = summary.CommitsBehind;
            gitStatusResponse.BranchName = summary.BranchName;
            gitStatusResponse.RemoteUrl = summary.RemoteUrl;
            gitStatusResponse.CommitHash = summary.CommitHash;
            return Ok(gitStatusResponse);
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

            if (string.IsNullOrEmpty(applyStagingActionRequest.RepoKey))
            {
                logger.LogError("Repository key must be populated.");
                response.IsErrorResponse = true;
                response.ErrorMessage = "Cannot process request because the repository key is not populated.";
                return BadRequest(response);
            }

            if (string.IsNullOrEmpty(applyStagingActionRequest.FileName))
            {
                logger.LogError("File name must be populated.");
                response.IsErrorResponse = true;
                response.ErrorMessage = "Cannot process request because the file name is not populated.";
                return BadRequest(response);
            }

            string repoKey = applyStagingActionRequest.RepoKey;
            string fileName = applyStagingActionRequest.FileName;
            bool isStaging = applyStagingActionRequest.IsStaging;
            List<RepositoryStatusElement>? statusElements = statusManager.ApplyStagingAction(repoKey, fileName, isStaging);
            if (statusElements == null)
            {
                logger.LogError("Failed to apply staging action for repo ID: {repoId}", repoKey);
                response.IsErrorResponse = true;
                response.ErrorMessage = $"Failed to apply staging action for repo ID: {repoKey}. Check server logs for more information.";
                return BadRequest(response);
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
            logger.LogInformation("Received request to commit changes for repo: {repoId}", request.RepositoryId);
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
            if (!cacheManager.Users.TryGetValue(userId, out UserAccountModel user))
            {
                response.IsErrorResponse = true;
                response.ErrorMessage = $"No user found in cache for user ID: {userId}. Cannot commit changes.";
                logger.LogError("No user was found for user ID: {userId}. Sending error response", userId);
                return BadRequest(response);
            }

            try
            {
                statusManager.CommitChanges(workingDirectory, user.Name, request.Email, request.CommitMessage);
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

        [HttpPost]
        [Route("gitPush")]
        public ActionResult<GitPushResponse> PushChanges([FromBody] GitPushRequest request)
        {
            logger.LogInformation("Received request to push changes for repo: {repoId}", request.RepositoryId);
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

            if (!statusManager.TryPushChanges(workingDirectory, webApiConfigurations.GitHubApiToken, out string errorMessage))
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
            logger.LogInformation("Received request to pull changes for repo: {repoId}", request.RepositoryId);
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
            if (!cacheManager.Users.TryGetValue(userId, out UserAccountModel user))
            {
                response.IsErrorResponse = true;
                response.ErrorMessage = $"No user found in cache for user ID: {userId}. Cannot pull changes.";
                logger.LogError("No user was found for user ID: {userId}. Sending error response", userId);
                return BadRequest(response);
            }

            string fullName = user.Name;
            string token = webApiConfigurations.GitHubApiToken;
            if (!statusManager.TryPullChanges(workingDirectory, fullName, email, token, out string errorMessage))
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
        /// Populates the cache from the database on controller instantiation.
        /// </summary>
        private void PopulateCache()
        {
            foreach (RepositoryModel repo in applicationDbContext.Repositories)
            {
                LocalGitRepository localRepo = new()
                {
                    Id = repo.Id,
                    UserId = repo.UserId,
                    Name = repo.Name,
                    Owner = repo.Owner,
                    Url = repo.Url,
                };
                cacheManager.Repositories.TryAdd(localRepo.Id, localRepo);
            }

            foreach (WorkingDirectoryModel workingDirectoryModel in applicationDbContext.WorkingDirectories)
            {
                cacheManager.WorkingDirectories.TryAdd(workingDirectoryModel.RepositoryId, workingDirectoryModel.WorkingDirectory);
            }

            foreach (UserAccountModel user in applicationDbContext.UserAccounts)
            {
                cacheManager.Users.TryAdd(user.Id, user);
            }
        }
    }
}
