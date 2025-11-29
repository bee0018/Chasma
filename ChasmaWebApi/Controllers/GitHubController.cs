using ChasmaWebApi.Data.Objects;
using ChasmaWebApi.Messages;
using Microsoft.AspNetCore.Mvc;
using Octokit;

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
    /// Instantiates a new <see cref="GitHubController"/> class.
    /// </summary>
    /// <param name="log">The internal API logger.</param>
    /// <param name="config">The API configurations.</param>
    public GitHubController(ILogger<GitHubController> log, ChasmaWebApiConfigurations config)
    {
        logger = log;
        webApiConfigurations = config;
    }

    /// <summary>
    /// Gets the Chasma workflow results via the GitHub API.
    /// </summary>
    /// <returns>The workflow results.</returns>
    [HttpGet]
    [Route("workflowRuns")]
    public async Task<ActionResult<GitHubWorkflowRunMessage>> GetChasmaWorkflowResults()
    {
        logger.LogInformation("Getting Chasma workflow data for the last {threshold} builds.", webApiConfigurations.WorkflowRunReportThreshold);
        string token = webApiConfigurations.GitHubApiToken;
        ProductHeaderValue productHeader = new ProductHeaderValue(webApiConfigurations.GitHubRepoName);
        GitHubClient gitHubClient = new(productHeader) { Credentials = new Credentials(token) };
        string repoOwner = webApiConfigurations.GitHubRepoOwner;
        string repoName = webApiConfigurations.GitHubRepoName;
        GitHubWorkflowRunMessage gitHubWorkflowRunMessage = new() { RepositoryName = repoName };
        try
        {
            WorkflowRunsResponse response = await gitHubClient.Actions.Workflows.Runs.List(repoOwner, repoName);
            if (response.WorkflowRuns == null)
            {
                gitHubWorkflowRunMessage.IsErrorResponse = true;
                gitHubWorkflowRunMessage.ErrorMessage = "Failed to fetch workflow runs from GitHub API. Check server logs for more information.";
                return Ok(gitHubWorkflowRunMessage);
            }

            gitHubWorkflowRunMessage.BuildCount = webApiConfigurations.WorkflowRunReportThreshold;
            List<WorkflowRun> runs = response.WorkflowRuns.Take(webApiConfigurations.WorkflowRunReportThreshold).ToList();
            foreach (WorkflowRun run in runs)
            {
                WorkflowRunResult buildResult = new()
                {
                    BranchName = run.HeadBranch,
                    RunNumber = run.RunNumber,
                    BuildTrigger = run.Event,
                    CommitMessage = run.HeadCommit.Message,
                    BuildStatus = run.Status.StringValue,
                    BuildConclusion = run.Conclusion.HasValue ? run.Conclusion.Value.ToString() : "Unknown",
                    CreatedDate = run.CreatedAt.ToString(),
                    UpdatedDate = run.UpdatedAt.ToString(),
                    WorkflowUrl = run.HtmlUrl,
                    AuthorName = run.Actor.Login,
                };
                gitHubWorkflowRunMessage.WorkflowRunResults.Add(buildResult);
            }
            
            logger.LogInformation("Retrieved {count} build runs from {repo}.", runs.Count, repoName);
            return Ok(gitHubWorkflowRunMessage);
        }
        catch
        {
            gitHubWorkflowRunMessage.IsErrorResponse = true;
            gitHubWorkflowRunMessage.ErrorMessage = $"Error fetching workflow runs from {repoName}. Check server logs for more information.";
            return Ok(gitHubWorkflowRunMessage);
        }
    }
}