using ChasmaWebApi.Data.Interfaces;
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
        /// The API configurations options.
        /// </summary>
        private readonly ChasmaWebApiConfigurations webApiConfigurations;

        #region Constructor

        /// <summary>
        /// Instantiates a new <see cref="RepositoryStatusController"/> class.
        /// </summary>
        /// <param name="log">The internal API logger.</param>
        /// <param name="config">The API configurations.</param>
        /// <param name="manager">The repository status manager.</param>
        public RepositoryStatusController(ILogger<RepositoryStatusController> log, ChasmaWebApiConfigurations config, IRepositoryStatusManager manager)
        {
            logger = log;
            webApiConfigurations = config;
            statusManager = manager;
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
            List<RepositoryStatusElement>? statusElements = statusManager.GetRepositoryStatus(repoId);
            if (statusElements == null)
            {
                logger.LogError("Failed to get repository status for repo ID: {repoId}", repoId);
                gitStatusResponse.IsErrorResponse = true;
                gitStatusResponse.ErrorMessage = $"Failed to get repository status for repo ID: {repoId}";
                return BadRequest(gitStatusResponse);
            }

            gitStatusResponse.StatusElements = statusElements;
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
    }
}
