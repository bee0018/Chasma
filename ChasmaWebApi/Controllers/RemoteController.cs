using ChasmaWebApi.Core.Interfaces.Control;
using ChasmaWebApi.Core.Interfaces.Infrastructure;
using ChasmaWebApi.Data.Messages;
using ChasmaWebApi.Data.Objects.Application;
using ChasmaWebApi.Data.Objects.Git;
using ChasmaWebApi.Data.Objects.Remote;
using ChasmaWebApi.Data.Requests.Remote;
using ChasmaWebApi.Data.Responses.Remote;
using ChasmaWebApi.Util;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace ChasmaWebApi.Controllers
{
    /// <summary>
    /// Class representing the controller used to interact with remote git repositories.
    /// </summary>
    [Authorize]
    [ApiController]
    [Route("api/[controller]")]
    [EnableRateLimiting(ChasmaWebApiConfigurations.RateLimiterPolicy)]
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
        /// The internal API encryption service.
        /// </summary>
        private readonly IEncryptionService encryptionService;

        /// <summary>
        /// Initializes a new instance of the <see cref="RemoteController"/> class.
        /// </summary>
        /// <param name="log">The internal logger.</param>
        /// <param name="controlService">The application orchestrator.</param>
        /// <param name="apiCacheManager">The internal API cache manager.</param>
        public RemoteController(ILogger<RemoteController> log, IApplicationControlService controlService, ICacheManager apiCacheManager, IEncryptionService apiEncryptionService)
        {
            logger = log;
            applicationControlService = controlService;
            cacheManager = apiCacheManager;
            encryptionService = apiEncryptionService;
        }

        /// <summary>
        /// Gets the global pull requests that is tracked in the web API.
        /// </summary>
        /// <returns>The message containing all the tracked pull requests.</returns>
        [HttpGet]
        [Route("getGlobalPullRequestStatuses")]
        public ActionResult<GetPullRequestStatusMessage> GetGlobalPullRequests()
        {
            logger.LogInformation("Received request to get global pull requests.");
            List<RemotePullRequest> pullRequests = [.. cacheManager.GitHubPullRequests.Values, .. cacheManager.GitLabMergeRequests.Values];
            GetPullRequestStatusMessage message = new() { PullRequests = pullRequests };
            return Ok(message);
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

            string repoName = request.RepositoryName;
            if (string.IsNullOrEmpty(repoName))
            {
                logger.LogError("Empty repository name received when attempting to get workflow run results. Sending error response.");
                response.IsErrorResponse = true;
                response.ErrorMessage = "Invalid request. Repository name is required.";
                return BadRequest(response);
            }

            string repoOwner = request.RepositoryOwner;
            if (string.IsNullOrEmpty(repoOwner))
            {
                logger.LogError("Empty repository owner received when attempting to get workflow run results. Sending error response.");
                response.IsErrorResponse = true;
                response.ErrorMessage = "Invalid request. Repository owner is required.";
                return BadRequest(response);
            }

            ChasmaWebApiConfigurations webApiConfigurations = ChasmaWebApiConfigurations.GetApiConfig();
            logger.LogInformation("Attempting to get workflow data for the last {threshold} builds for {repoName}.", webApiConfigurations.WorkflowRunReportThreshold ?? 30, repoName);
            string token = webApiConfigurations.GitHubApiToken;
            string decryptedToken = encryptionService.DecryptString(token);
            if (string.IsNullOrEmpty(decryptedToken))
            {
                logger.LogError("GitHub API token is not configured. Cannot retrieve workflow run results. Sending error response.");
                response.IsErrorResponse = true;
                response.ErrorMessage = "GitHub API token is not configured. Cannot retrieve workflow run results.";
                return Ok(response);
            }

            try
            {
                bool runsRetrieved = applicationControlService.TryGetWorkflowRunResults(repoName, repoOwner, decryptedToken, out List<WorkflowRunResult> runResults, out string errorMessage);
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

            if (!cacheManager.Repositories.TryGetValue(repoId, out LocalGitRepository repo))
            {
                logger.LogError("Invalid {request}. Repository not found in cache with identifier {id} when trying to create pull request. Sending error response.", nameof(CreatePRRequest), repoId);
                response.IsErrorResponse = true;
                response.ErrorMessage = "Could not create pull request. Repository could not be found.";
                return Ok(response);
            }

            string owner = repo.Owner;
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
                string token = RemoteHelper.GetApiToken(repo.HostPlatform);
                string decryptedToken = encryptionService.DecryptString(token);
                string title = request.PullRequestTitle;
                string headBranch = request.WorkingBranchName;
                string baseBranch = request.DestinationBranchName;
                string body = request.PullRequestBody ?? string.Empty;
                string repoName = request.RepositoryName;
                PreparedGitHubPullRequest pullRequest = new()
                {
                    RepositoryId = repoId,
                    WorkingDirectory = workingDirectory,
                    RepositoryOwner = owner,
                    RepositoryName = repoName,
                    PullRequestTitle = title,
                    HeadBranch = headBranch,
                    BaseBranch = baseBranch,
                    Description = body,
                    Token = decryptedToken,
                };
                if (!applicationControlService.TryCreatePullRequest(pullRequest, out int pullRequestId, out string prUrl, out string timestamp, out string errorMessage))
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

            string repoName = request.RepositoryName;
            if (string.IsNullOrEmpty(repoName))
            {
                logger.LogError("Repository name must be populated. Sending error response.");
                response.IsErrorResponse = true;
                response.ErrorMessage = "Repository name must be populated. Cannot create issue.";
                return BadRequest(response);
            }

            string repoOwner = request.RepositoryOwner;
            if (string.IsNullOrEmpty(repoOwner))
            {
                logger.LogError("Repository owner must be populated. Sending error response.");
                response.IsErrorResponse = true;
                response.ErrorMessage = "Repository owner must be populated. Cannot create issue.";
                return BadRequest(response);
            }

            string title = request.Title;
            if (string.IsNullOrEmpty(title))
            {
                logger.LogError("Issue title must be populated. Sending error response.");
                response.IsErrorResponse = true;
                response.ErrorMessage = "Issue title must be populated. Cannot create issue.";
                return BadRequest(response);
            }

            try
            {
                IssueOutline outline = new()
                {
                    RepoOwner = repoOwner,
                    RepoName = repoName,
                    Title = title,
                    Description = request.Body ?? string.Empty,
                    AdditionalAssignees = request.Assignees,
                    Platform = RemoteHostPlatform.GitHub,
                    Labels = request.Labels,
                };

                if (!applicationControlService.TryCreateIssue(outline, out RemoteIssueResult createdIssue, out string errorMessage))
                {
                    logger.LogError("Failed to create issue for {repoName}. Sending error response.", repoName);
                    response.IsErrorResponse = true;
                    response.ErrorMessage = errorMessage;
                    return Ok(response);
                }

                logger.LogInformation("Successfully created issue {issueId} at {issueUrl}.", createdIssue.IssueId, createdIssue.Url);
                response.IssueUrl = createdIssue.Url;
                response.IssueId = createdIssue.IssueId;
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

        /// <summary>
        /// Retrieves the labels for a cloud hosted repository.
        /// </summary>
        /// <param name="request">The request to retrieve cloud hosted repository labels.</param>
        /// <returns>The response to the request.</returns>
        [HttpPost]
        [Route("retrieveRemoteLabels")]
        public ActionResult<GetLabelsResponse> RetrieveLabels([FromBody] GetLabelsRequest request)
        {
            GetLabelsResponse response = new();
            if (request == null)
            {
                logger.LogError("Failed to get GitHub labels because the request was null. Sending error response.");
                response.IsErrorResponse = true;
                response.ErrorMessage = "Request is null. Cannot get GitHub labels.";
                return BadRequest(response);
            }

            string repoId = request.RepositoryId;
            if (string.IsNullOrEmpty(repoId))
            {
                logger.LogError("Failed to get GitHub labels because the repository identifier is empty. Sending error response.");
                response.IsErrorResponse = true;
                response.ErrorMessage = "Repository identifier was empty. Cannot get GitHub labels.";
                return Ok(response);
            }

            if (!cacheManager.Repositories.TryGetValue(repoId, out LocalGitRepository repository))
            {
                logger.LogError("Failed to get GitHub labels because the repository cannot be found. Sending error response.");
                response.IsErrorResponse = true;
                response.ErrorMessage = "Repository cannot be found in cache. Cannot get GitHub labels.";
                return Ok(response);
            }

            if (!applicationControlService.TryGetRepositoryLabels(repository, out List<string> labels, out string errorMessage))
            {
                logger.LogError("Failed to get GitHub labels: {error}", errorMessage);
                response.IsErrorResponse = true;
                response.ErrorMessage = errorMessage;
                return Ok(response);
            }

            logger.LogInformation("Successfully retrieved labels for repository {repo}.", repository.GetDisplayName());
            response.Labels.AddRange(labels);
            return Ok(response);
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

            IssueOutline outline = new()
            {
                RepoOwner = repository.Owner,
                RepoName = repository.Name,
                MainAssignee = request.MainAssignee,
                AdditionalAssignees = request.Contacts,
                Title = request.Title,
                Description = request.Description ?? string.Empty,
                Confidential = request.Confidential,
                Platform = RemoteHostPlatform.GitLab,
                Labels = request.Labels,
            };
            try
            {
                if (!applicationControlService.TryCreateIssue(outline, out RemoteIssueResult createdIssue, out string errorMessage))
                {
                    logger.LogError("Failed to create GitLab issue for {repo} because: {error}", repository.GetDisplayName(), errorMessage);
                    response.IsErrorResponse = true;
                    response.ErrorMessage = errorMessage;
                    return Ok(response);
                }

                logger.LogInformation("Successfully created GitLab issue for {repo} with number {number}", repository.GetDisplayName(), createdIssue.IssueId);
                response.Issue = createdIssue;
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
        [Route("getRemoteProjectMembers")]
        public ActionResult<GetRemoteProjectMembersResponse> GetRemoteProjectMembers([FromBody] GetRemoteProjectMembersRequest request)
        {
            string requestName = nameof(GetRemoteProjectMembersRequest);
            GetRemoteProjectMembersResponse response = new();
            if (request == null)
            {
                logger.LogError("Could not get remote project members because {request} was null", requestName);
                response.IsErrorResponse = true;
                response.ErrorMessage = "Request must be populated.";
                return BadRequest(response);
            }

            string repoId = request.RepositoryId;
            if (string.IsNullOrEmpty(repoId))
            {
                logger.LogError("Could not get remote project members because {request}'s repository identifier null or empty", requestName);
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
                if (!applicationControlService.TryGetMembers(repository, out List<RemoteProjectMember> projectMembers, out long projectId, out string errorMessage))
                {
                    logger.LogError("Failed to get {hostPlatform} project members for {repo} because: {error}", repository.HostPlatform, repository.GetDisplayName(), errorMessage);
                    response.IsErrorResponse = true;
                    response.ErrorMessage = errorMessage;
                    return Ok(response);
                }

                logger.LogInformation("Successfully retrieved members for {repo}.", repository.GetDisplayName());
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
        [HttpPost]
        [Route("createGitLabMergeRequest")]
        public ActionResult<CreateMergeRequestResponse> CreateGitLabMergeRequest([FromBody] CreateMergeRequest request)
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
                    RepositoryId = repoId,
                    RepoOwner = repository.Owner,
                    RepoName = repository.Name,
                    SourceBranch = sourceBranch,
                    TargetBranch = targeBranch,
                    Title = title,
                    TargetProjectId = projectId,
                    Assignee = request.Assignee,
                    AdditonalAssignees = request.AdditionalAssignees,
                    Reviewers = request.Reviewers,
                    Description = request.Description ?? string.Empty,
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
