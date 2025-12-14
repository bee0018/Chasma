using ChasmaWebApi.Data.Interfaces;
using ChasmaWebApi.Data.Objects;
using ChasmaWebApi.Data.Requests;
using ChasmaWebApi.Data.Responses;
using Microsoft.AspNetCore.Mvc;

namespace ChasmaWebApi.Controllers;

/// <summary>
/// Class representing the GitHub integrated functions.
/// </summary>
[Route("api/[controller]")]
public class GitHubController : ControllerBase
{
    /// <summary>
    /// The internal API logger.
    /// </summary>
    private readonly ILogger<GitHubController> logger;
    
    /// <summary>
    /// The API configurations options.
    /// </summary>
    private readonly ChasmaWebApiConfigurations webApiConfigurations;

    /// <summary>
    /// The internal workflow run manager.
    /// </summary>
    private readonly IWorkFlowManager workflowManager;

    #region Constructor

    /// <summary>
    /// Instantiates a new <see cref="GitHubController"/> class.
    /// </summary>
    /// <param name="log">The internal API logger.</param>
    /// <param name="config">The API configurations.</param>
    /// <param name="manager">The workflow run manager.</param>
    public GitHubController(ILogger<GitHubController> log, ChasmaWebApiConfigurations config, IWorkFlowManager manager)
    {
        logger = log;
        webApiConfigurations = config;
        workflowManager = manager;
    }

    #endregion

    /// <summary>
    /// Gets the Chasma workflow results via the GitHub API.
    /// </summary>
    /// <param name="request">The request to get workflow run results.</param>
    /// <returns>The workflow results.</returns>
    [HttpGet]
    [Route("workflowRuns")]
    public ActionResult<GitHubWorkflowRunResponse> GetChasmaWorkflowResults([FromQuery] GetWorkflowResultsRequest request)
    {
        logger.LogInformation("Attempting to get workflow data for the last {threshold} builds for {repoName}.", webApiConfigurations.WorkflowRunReportThreshold, request.RepositoryName);
        string token = webApiConfigurations.GitHubApiToken;
        string repoOwner = request.RepositoryOwner;
        string repoName = request.RepositoryName;
        int buildCount = webApiConfigurations.WorkflowRunReportThreshold;
        GitHubWorkflowRunResponse gitHubWorkflowRunResponse = new() { RepositoryName = repoName };
        try
        {
            bool runsRetrieved = workflowManager.TryGetWorkflowRunResults(repoName, repoOwner, token, buildCount, out List<WorkflowRunResult> runResults, out string errorMessage);
            if (!runsRetrieved && !string.IsNullOrEmpty(errorMessage))
            {
                gitHubWorkflowRunResponse.IsErrorResponse = true;
                gitHubWorkflowRunResponse.ErrorMessage = errorMessage;
                return Ok(gitHubWorkflowRunResponse);
            }

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
    /// Gets the valid git repositories found on this system.
    /// </summary>
    /// <returns>The validated, local git repositories.</returns>
    [HttpGet]
    [Route("findLocalGitRepositories")]
    public List<string> GetLocalGitRepositories()
    {
        logger.LogInformation("Attempting to get local git repositories on this filesystem.");
        List<string> repositories = workflowManager.FindLocalGitRepositories();
        if (repositories.Count > 0)
        {
            logger.LogInformation("Found {count} repositories on this machine.", repositories.Count);
        }
        else
        {
            logger.LogInformation("No local git repositories found.");
        }
        
        return repositories;
    }
}