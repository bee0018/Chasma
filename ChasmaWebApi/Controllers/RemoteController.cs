using ChasmaWebApi.Core.Interfaces.Control;
using ChasmaWebApi.Core.Interfaces.Infrastructure;
using ChasmaWebApi.Data.Objects.Git;
using ChasmaWebApi.Data.Objects.Remote;
using ChasmaWebApi.Data.Requests.Remote;
using ChasmaWebApi.Data.Responses.Remote;
using Microsoft.AspNetCore.Mvc;

namespace ChasmaWebApi.Controllers
{
    /// <summary>
    /// Class representing the controller used to interact with remote git repositories.
    /// </summary>
    [Route("api/[controller]")]
    public class RemoteController : ControllerBase
    {
        /// <summary>
        /// The internal API logger.
        /// </summary>
        private readonly ILogger<RemoteController> logger;

        /// <summary>
        /// The internal API application control service for managing application-level operations.
        /// </summary>
        private readonly IApplicationControlService applicationControlService;

        /// <summary>
        /// The internal cache manager.
        /// </summary>
        private readonly ICacheManager cacheManager;

        /// <summary>
        /// The API configurations options.
        /// </summary>
        private readonly ChasmaWebApiConfigurations webApiConfigurations;

        /// <summary>
        /// Initializes a new instance of the <see cref="RemoteController"/> class.
        /// </summary>
        /// <param name="log">The internal logger.</param>
        /// <param name="controlService">The application orchestrator.</param>
        /// <param name="apiConfig">The API configurations.</param>
        /// <param name="apiCacheManager">The internal API cache manager.</param>
        public RemoteController(ILogger<RemoteController> log, IApplicationControlService controlService, ChasmaWebApiConfigurations apiConfig, ICacheManager apiCacheManager)
        {
            logger = log;
            applicationControlService = controlService;
            webApiConfigurations = apiConfig;
            cacheManager = apiCacheManager;
        }

        #region GitHub

        /// <summary>
        /// Gets the workflow results via the GitHub API.
        /// </summary>
        /// <param name="request">The request to get workflow run results.</param>
        /// <returns>The workflow results.</returns>
        [HttpPost]
        [Route("retrieveGitHubWorkflowRuns")]
        public ActionResult<GitHubWorkflowRunResponse> GetGitHubWorkflowResults([FromBody] GetWorkflowResultsRequest request)
        {
            GitHubWorkflowRunResponse response = new();
            if (request == null)
            {
                logger.LogError("Null request received to get workflow run results. Sending error response.");
                response.IsErrorResponse = true;
                response.ErrorMessage = "Null request received. Cannot get workflow runs.";
                return BadRequest(response);
            }

            if (string.IsNullOrEmpty(request.RepositoryName))
            {
                logger.LogError("Empty repository name received when attempting to get workflow run results. Sending error response.");
                response.IsErrorResponse = true;
                response.ErrorMessage = "Invalid request. Repository name is required.";
                return BadRequest(response);
            }

            if (string.IsNullOrEmpty(request.RepositoryOwner))
            {
                logger.LogError("Empty repository owner received when attempting to get workflow run results. Sending error response.");
                response.IsErrorResponse = true;
                response.ErrorMessage = "Invalid request. Repository owner is required.";
                return BadRequest(response);
            }

            logger.LogInformation("Attempting to get workflow data for the last {threshold} builds for {repoName}.", webApiConfigurations.WorkflowRunReportThreshold, request.RepositoryName);
            string token = webApiConfigurations.GitHubApiToken;
            string repoOwner = request.RepositoryOwner;
            string repoName = request.RepositoryName;
            int buildCount = webApiConfigurations.WorkflowRunReportThreshold;
            try
            {
                bool runsRetrieved = applicationControlService.TryGetWorkflowRunResults(repoName, repoOwner, token, buildCount, out List<WorkflowRunResult> runResults, out string errorMessage);
                if (!runsRetrieved && !string.IsNullOrEmpty(errorMessage))
                {
                    response.IsErrorResponse = true;
                    response.ErrorMessage = errorMessage;
                    return Ok(response);
                }

                response.RepositoryName = repoName;
                response.WorkflowRunResults.AddRange(runResults);
                logger.LogInformation("Retrieved latest {count} build runs from {repo}.", runResults.Count, repoName);
                return Ok(response);
            }
            catch
            {
                response.IsErrorResponse = true;
                response.ErrorMessage = $"Error fetching workflow runs from {repoName}. Check server logs for more information.";
                return BadRequest(response);
            }
        }

        /// <summary>
        /// Creates a pull request in the specified repository.
        /// </summary>
        /// <param name="request">Request containing the details to create a PR.</param>
        /// <returns>Pull request response if successful.</returns>
        [HttpPost]
        [Route("createGitHubPullRequest")]
        public ActionResult<CreatePRResponse> CreatePullRequest([FromBody] CreatePRRequest request)
        {
            CreatePRResponse response = new();
            if (request == null)
            {
                response.IsErrorResponse = true;
                response.ErrorMessage = "Null request received. Cannot create pull request.";
                logger.LogError("CreatePRRequest received is null. Sending error response");
                return BadRequest(response);
            }

            if (string.IsNullOrEmpty(request.RepositoryId))
            {
                response.IsErrorResponse = true;
                response.ErrorMessage = "Repository identifier must be populated. Cannot get branches.";
                logger.LogError("Null or empty repository identifier received. Sending error response");
                return Ok(response);
            }

            string repoId = request.RepositoryId;
            if (!cacheManager.WorkingDirectories.TryGetValue(repoId, out string workingDirectory))
            {
                response.IsErrorResponse = true;
                response.ErrorMessage = $"No working directory found in cache for {repoId}. Cannot get branches.";
                logger.LogError("No working directory was found for repo identifier {repoId}. Sending error response", repoId);
                return Ok(response);
            }

            if (string.IsNullOrEmpty(request.PullRequestTitle))
            {
                response.IsErrorResponse = true;
                response.ErrorMessage = "Pull request title must be populated. Cannot create pull request.";
                logger.LogError("Null or empty pull request title received. Sending error response");
                return Ok(response);
            }

            if (string.IsNullOrEmpty(request.WorkingBranchName))
            {
                response.IsErrorResponse = true;
                response.ErrorMessage = "Working branch name must be populated. Cannot create pull request.";
                logger.LogError("Null or empty working branch name received. Sending error response");
                return Ok(response);
            }

            if (string.IsNullOrEmpty(request.DestinationBranchName))
            {
                response.IsErrorResponse = true;
                response.ErrorMessage = "Destination branch name must be populated. Cannot create pull request.";
                logger.LogError("Null or empty destination branch name received. Sending error response");
                return Ok(response);
            }

            if (string.IsNullOrEmpty(request.PullRequestBody))
            {
                response.IsErrorResponse = true;
                response.ErrorMessage = "Pull request body message must be populated. Cannot create pull request.";
                logger.LogError("Null or empty pull request body message received. Sending error response");
                return Ok(response);
            }

            string owner = cacheManager.Repositories.TryGetValue(repoId, out LocalGitRepository repo) ? repo.Owner : string.Empty;
            if (string.IsNullOrEmpty(owner))
            {
                response.IsErrorResponse = true;
                response.ErrorMessage = "Owner of repository not found. Cannot create pull request.";
                logger.LogError("Owner could be found when creating pull request. Sending error response");
                return Ok(response);
            }

            if (string.IsNullOrEmpty(request.RepositoryName))
            {
                response.IsErrorResponse = true;
                response.ErrorMessage = "Repository name must be populated. Cannot create pull request.";
                logger.LogError("Null or empty repository name received. Sending error response");
                return Ok(response);
            }

            try
            {
                string token = webApiConfigurations.GitHubApiToken;
                string title = request.PullRequestTitle;
                string headBranch = request.WorkingBranchName;
                string baseBranch = request.DestinationBranchName;
                string body = request.PullRequestBody;
                string repoName = request.RepositoryName;
                if (!applicationControlService.TryCreatePullRequest(workingDirectory, owner, repoName, title, headBranch, baseBranch, body, token, out int pullRequestId, out string prUrl, out string timestamp, out string errorMessage))
                {
                    response.IsErrorResponse = true;
                    response.ErrorMessage = $"Failed to create pull request for repo: {request.RepositoryName}. {errorMessage}";
                    logger.LogError("Failed to create pull request for repo: {repoName}. {errorMessage}", request.RepositoryName, errorMessage);
                    return Ok(response);
                }

                response.PullRequestId = pullRequestId;
                response.PullRequestUrl = prUrl;
                response.TimeStamp = timestamp;
                logger.LogInformation("Successfully created pull request for repo: {repoName}", request.RepositoryName);
                return Ok(response);
            }
            catch (Exception e)
            {
                response.IsErrorResponse = true;
                response.ErrorMessage = $"Error creating pull request for repo: {request.RepositoryName}. Check server logs for more information.";
                logger.LogError(e, "Error creating pull request for repo: {repoName}", request.RepositoryName);
                return Ok(response);
            }
        }

        /// <summary>
        /// Creates an issue on GitHub in the specified repository.
        /// </summary>
        /// <param name="request">Request containing the details to create a GitHub issue.</param>
        /// <returns>GitHub issue response if successful.</returns>
        [HttpPost]
        [Route("createGitHubIssue")]
        public ActionResult<CreateGitHubIssueResponse> CreateGitHubIssue([FromBody] CreateGitHubIssueRequest request)
        {
            logger.LogInformation("Received request to create a GitHub issue.");
            CreateGitHubIssueResponse response = new();
            if (request == null)
            {
                logger.LogError("Failed to create because of null request. Sending error response.");
                response.IsErrorResponse = true;
                response.ErrorMessage = "Request is null. Cannot create issue.";
                return BadRequest(response);
            }

            if (string.IsNullOrEmpty(request.RepositoryName))
            {
                logger.LogError("Repository name must be populated. Sending error response.");
                response.IsErrorResponse = true;
                response.ErrorMessage = "Repository name must be populated. Cannot create issue.";
                return BadRequest(response);
            }

            if (string.IsNullOrEmpty(request.RepositoryOwner))
            {
                logger.LogError("Repository owner must be populated. Sending error response.");
                response.IsErrorResponse = true;
                response.ErrorMessage = "Repository owner must be populated. Cannot create issue.";
                return BadRequest(response);
            }

            if (string.IsNullOrEmpty(request.Title))
            {
                logger.LogError("Issue title must be populated. Sending error response.");
                response.IsErrorResponse = true;
                response.ErrorMessage = "Issue title must be populated. Cannot create issue.";
                return BadRequest(response);
            }

            if (string.IsNullOrEmpty(request.Body))
            {
                logger.LogError("Issue description must be populated. Sending error response.");
                response.IsErrorResponse = true;
                response.ErrorMessage = "Issue body description must be populated. Cannot create issue.";
                return BadRequest(response);
            }

            try
            {
                string repoName = request.RepositoryName;
                string repoOwner = request.RepositoryOwner;
                string title = request.Title;
                string body = request.Body;
                string token = webApiConfigurations.GitHubApiToken;
                bool issueIsCreated = applicationControlService.TryCreateIssue(repoName, repoOwner, title, body, token, out int issueId, out string issueUrl, out string errorMessage);
                if (!issueIsCreated)
                {
                    logger.LogError("Failed to create issue for {repoName}. Sending error response.", repoName);
                    response.IsErrorResponse = true;
                    response.ErrorMessage = errorMessage;
                    return Ok(response);
                }

                logger.LogInformation("Successfully created issue {issueId} at {issueUrl}.", issueId, issueUrl);
                response.IssueUrl = issueUrl;
                response.IssueId = issueId;
                return Ok(response);
            }
            catch (Exception ex)
            {
                logger.LogError("Cannot create issue because of the following exception: {message}", ex.Message);
                response.IsErrorResponse = true;
                response.ErrorMessage = "Exception occurred when creating GitHub issue. Check server logs for more information.";
                return BadRequest(response);
            }
        }

        #endregion

        #region GitLab

        /// <summary>
        /// Gets the pipeline jobs for the specific repository.
        /// </summary>
        /// <param name="request">The request to get pipeline jobs.</param>
        /// <returns>The response to getting the pipeline jobs.</returns>
        [HttpPost]
        [Route("getPipelineJobs")]
        public ActionResult<GetPipelineJobsResponse> GetPipelineJobs([FromBody] GetPipelineJobsRequest request)
        {
            GetPipelineJobsResponse response = new();
            if (request == null)
            {
                logger.LogError("Failed to get pipeline jobs because the request was null. Sending error response.");
                response.IsErrorResponse = true;
                response.ErrorMessage = "Request is null. Cannot get pipeline builds.";
                return BadRequest(response);
            }

            string repoId = request.RepositoryId;
            if (string.IsNullOrEmpty(repoId))
            {
                logger.LogError("Failed to get pipeline jobs because the repository identifier is empty. Sending error response.");
                response.IsErrorResponse = true;
                response.ErrorMessage = "Repository identifier was empty. Cannot get pipeline builds.";
                return Ok(response);
            }

            if (!cacheManager.Repositories.TryGetValue(repoId, out LocalGitRepository repository))
            {
                logger.LogError("Failed to get pipeline jobs because the repository cannot be found. Sending error response.");
                response.IsErrorResponse = true;
                response.ErrorMessage = "Repository cannot be found in cache. Cannot get pipeline builds.";
                return Ok(response);
            }

            bool resultsRetrieved = applicationControlService.TryGetPipelineJobResults(repository, out List<WorkflowRunResult> buildResults, out string errorMessage);
            if (!resultsRetrieved)
            {
                logger.LogError("Failed to get pipeline jobs {error}", errorMessage);
                response.IsErrorResponse = true;
                response.ErrorMessage = errorMessage;
                return Ok(response);
            }

            response.Results = buildResults;
            return Ok(response);
        }

        /// <summary>
        /// Creates an issue on GitLab in the specified repository.
        /// </summary>
        /// <param name="request">Request containing details to create a GitLab issue.</param>
        /// <returns>GitLab issue response if successful.</returns>
        [HttpPost]
        [Route("createGitLabIssue")]
        public ActionResult<CreateGitLabIssueResponse> CreateGitLabIssue([FromBody] CreateGitLabIssueRequest request)
        {
            string requestName = nameof(CreateGitLabIssueRequest);
            CreateGitLabIssueResponse response = new();
            if (request == null)
            {
                logger.LogError("Could not create GitLab issue because {request} was null", requestName);
                response.IsErrorResponse = true;
                response.ErrorMessage = "Request must be populated.";
                return BadRequest(response);
            }

            if (string.IsNullOrEmpty(request.Title))
            {
                logger.LogError("Could not create GitLab issue because {request} title was null or empty", requestName);
                response.IsErrorResponse = true;
                response.ErrorMessage = "Title must be populated.";
                return Ok(response);
            }

            string repoId = request.RepositoryId;
            if (string.IsNullOrEmpty(repoId))
            {
                logger.LogError("Could not create GitLab issue because {request}'s repository identifier null or empty.", requestName);
                response.IsErrorResponse = true;
                response.ErrorMessage = "Repository must be populated.";
                return Ok(response);
            }

            if (!cacheManager.Repositories.TryGetValue(repoId, out LocalGitRepository repository))
            {
                logger.LogError("Could not create GitLab issue because repo identifier {id} does not have a matching repository in cache", repoId);
                response.IsErrorResponse = true;
                response.ErrorMessage = "Title must be populated.";
                return Ok(response);
            }

            PreparedGitLabIssue outline = new()
            {
                RepoOwner = repository.Owner,
                RepoName = repository.Name,
                MainAssignee = request.MainAssignee,
                Contacts = request.Contacts,
                Title = request.Title,
                Description = request.Description,
                Confidential = request.Confidential,
            };
            try
            {
                if (!applicationControlService.TryCreateIssue(outline, out GitLabIssueResult issue, out string errorMessage))
                {
                    logger.LogError("Failed to create GitLab issue for {repo} because: {error}", repository.Name, errorMessage);
                    response.IsErrorResponse = true;
                    response.ErrorMessage = errorMessage;
                    return Ok(response);
                }

                logger.LogInformation("Successfully created GitLab issue for {repo} with number {number}", repository.Name, issue.IssueId);
                response.Issue = issue;
                return Ok(response);
            }
            catch (Exception ex)
            {
                logger.LogError("Cannot create Gitlab issue because of the following exception: {message}", ex.Message);
                response.IsErrorResponse = true;
                response.ErrorMessage = "Exception occurred when creating GitLab issue. Check server logs for more information.";
                return Ok(response);
            }
        }

        /// <summary>
        /// Gets the members of the specified repository.
        /// </summary>
        /// <param name="request">The request to get members of a repository.</param>
        /// <returns>The response containing project members of a repository.</returns>
        [HttpPost]
        [Route("getGitLabProjectMembers")]
        public ActionResult<GetGitLabProjectMembersResponse> GetGitLabProjectMembers([FromBody] GetGitLabProjectMembersRequest request)
        {
            string requestName = nameof(GetGitLabProjectMembersRequest);
            GetGitLabProjectMembersResponse response = new();
            if (request == null)
            {
                logger.LogError("Could not get GitLab project members because {request} was null", requestName);
                response.IsErrorResponse = true;
                response.ErrorMessage = "Request must be populated.";
                return BadRequest(response);
            }

            string repoId = request.RepositoryId;
            if (string.IsNullOrEmpty(repoId))
            {
                logger.LogError("Could not get GitLab project members because {request}'s repository identifier null or empty", requestName);
                response.IsErrorResponse = true;
                response.ErrorMessage = "Repository must be populated.";
                return Ok(response);
            }

            if (!cacheManager.Repositories.TryGetValue(repoId, out LocalGitRepository repository))
            {
                logger.LogError("Could not get project members because the repo identifier {id} does not have a matching repository in cache", repoId);
                response.IsErrorResponse = true;
                response.ErrorMessage = "Repository does not exist in cache.";
                return Ok(response);
            }

            try
            {
                if (!applicationControlService.TryGetMembers(repository, out List<GitLabProjectMember> projectMembers, out long projectId, out string errorMessage))
                {
                    logger.LogError("Failed to get GitLab project members for {repo} because: {error}", repository.Name, errorMessage);
                    response.IsErrorResponse = true;
                    response.ErrorMessage = errorMessage;
                    return Ok(response);
                }

                logger.LogInformation("Successfully retrieved members for {repo}.", repository.Name);
                response.ProjectMembers = projectMembers;
                response.ProjectId = projectId;
                return Ok(response);
            }
            catch (Exception ex)
            {
                logger.LogError("Could not get project members because of the following exception: {message}", ex.Message);
                response.IsErrorResponse = true;
                response.ErrorMessage = "Exception occurred when getting project members. Check server logs for more information.";
                return Ok(response);
            }
        }

        /// <summary>
        /// Creates a merge request via the GitLab API.
        /// </summary>
        /// <param name="request">Request to create a merge request.</param>
        /// <returns>Response to creating a merge request from GitLab API.</returns>
        public ActionResult<CreateMergeRequestResponse> CreateMergeRequest([FromBody] CreateMergeRequest request)
        {
            string requestName = nameof(CreateMergeRequest);
            CreateMergeRequestResponse response = new();
            if (request == null)
            {
                logger.LogError("Could not create GitLab merge request because {request} was null", requestName);
                response.IsErrorResponse = true;
                response.ErrorMessage = "Request must be populated.";
                return BadRequest(response);
            }

            string repoId = request.RepositoryId;
            if (string.IsNullOrEmpty(repoId))
            {
                logger.LogError("Could not create GitLab merge request because {request}'s repository identifier null or empty.", requestName);
                response.IsErrorResponse = true;
                response.ErrorMessage = "Repository must be populated.";
                return Ok(response);
            }

            if (!cacheManager.Repositories.TryGetValue(repoId, out LocalGitRepository repository))
            {
                logger.LogError("Could not create GitLab merge request because the repository cannot be found. Sending error response.");
                response.IsErrorResponse = true;
                response.ErrorMessage = "Repository cannot be found in cache. Cannot creat merge request.";
                return Ok(response);
            }

            string sourceBranch = request.SourceBranch;
            if (string.IsNullOrEmpty(sourceBranch))
            {
                logger.LogError("Could not create GitLab merge request because {request}'s source branch null or empty.", requestName);
                response.IsErrorResponse = true;
                response.ErrorMessage = "Source branch must be populated.";
                return Ok(response);
            }

            string targeBranch = request.TargetBranch;
            if (string.IsNullOrEmpty(targeBranch))
            {
                logger.LogError("Could not create GitLab merge request because {request}'s target branch null or empty.", requestName);
                response.IsErrorResponse = true;
                response.ErrorMessage = "Target branch must be populated.";
                return Ok(response);
            }

            string title = request.Title;
            if (string.IsNullOrEmpty(title))
            {
                logger.LogError("Could not create GitLab merge request because {request}'s merge request title null or empty.", requestName);
                response.IsErrorResponse = true;
                response.ErrorMessage = "Merge request title must be populated.";
                return Ok(response);
            }

            long? projectId = request.TargetProjectId;
            if (projectId == null)
            {
                logger.LogError("Could not create GitLab merge request because {request}'s target project was null.", requestName);
                response.IsErrorResponse = true;
                response.ErrorMessage = "Project must be selected.";
                return Ok(response);
            }

            try
            {
                PreparedGitLabMergeRequest preparedRequest = new()
                {
                    RepoOwner = repository.Owner,
                    RepoName = repository.Name,
                    SourceBranch = sourceBranch,
                    TargetBranch = targeBranch,
                    Title = title,
                    TargetProjectId = projectId,
                    Assignee = request.Assignee,
                    AdditonalAssignees = request.AdditionalAssignees,
                    Reviewers = request.Reviewers,
                    Description = request.Description,
                    RemoveSourceBranch = request.RemoveSourceBranch,
                    Squash = request.Squash,
                    AllowCollaboration = request.AllowCollaboration,
                };
                if (!applicationControlService.TryCreateMergeRequest(preparedRequest, out MergeRequestResult mergeResult, out string errorMessage))
                {
                    logger.LogError("Failed to create GitLab merge request because {error}. Sending error response.", errorMessage);
                    response.IsErrorResponse = true;
                    response.ErrorMessage = errorMessage;
                    return Ok(response);
                }

                logger.LogInformation("Successfully created merge result with number: {number}", mergeResult.Id);
                response.MergeRequest = mergeResult;
                return Ok(response);
            }
            catch (Exception ex)
            {
                logger.LogError("Exception creating GitLab merge request because {error}. Sending error response.", ex);
                response.IsErrorResponse = true;
                response.ErrorMessage = "Error creating merge request. Review server logs for more information.";
                return Ok(response);
            }
        }

        #endregion
    }
}
