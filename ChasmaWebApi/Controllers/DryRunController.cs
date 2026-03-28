using ChasmaWebApi.Core.Interfaces.Control;
using ChasmaWebApi.Core.Interfaces.Infrastructure;
using ChasmaWebApi.Data.Objects.DryRun;
using ChasmaWebApi.Data.Requests.DryRun;
using ChasmaWebApi.Data.Responses.DryRun;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ChasmaWebApi.Controllers
{
    /// <summary>
    /// Controller responsible for handling dry run operations, which are typically used for testing and validation purposes without making actual changes to the system.
    /// </summary>
    [Authorize]
    [ApiController]
    [Route("api/[controller]")]
    public class DryRunController : ControllerBase
    {
        /// <summary>
        /// The logger instance for logging diagnostic and operational information within the class.
        /// </summary>
        private readonly ILogger<DryRunController> logger;

        /// <summary>
        /// The internal cache manager.
        /// </summary>
        private readonly ICacheManager cacheManager;

        /// <summary>
        /// The internal API application control service for managing application-level operations.
        /// </summary>
        private readonly IApplicationControlService applicationControlService;

        /// <summary>
        /// Initializes a new instance of the <see cref="DryRunController"/> class.
        /// </summary>
        /// <param name="log">The internal logging service.</param>
        /// <param name="apiCacheManager">The API cache manager.</param>
        /// <param name="controlService">The application control orchestrator.</param>
        public DryRunController(ILogger<DryRunController> log, ICacheManager apiCacheManager, IApplicationControlService controlService)
        {
            logger = log;
            cacheManager = apiCacheManager;
            applicationControlService = controlService;
        }

        /// <summary>
        /// Performs a simulated git pull operation based on the provided request parameters.
        /// </summary>
        /// <param name="request">The request to perform a git pull dry run.</param>
        /// <returns>The response of a git pull dry run.</returns>
        [HttpPost]
        [Route("simulateGitPull")]
        public ActionResult<SimulateGitPullResponse> SimulateGitPull([FromBody] SimulateGitPullRequest request)
        {
            SimulateGitPullResponse response = new();
            string requestName = nameof(SimulateGitPullRequest);
            if (request == null)
            {
                logger.LogError("Received a null {request}. Sending error response.", requestName);
                response.IsErrorResponse = true;
                response.ErrorMessage = "Request must be populated.";
                return BadRequest(response);
            }

            List<PullSimulationEntry> entries = request.Entries;
            List<string> repositoryIds = entries.Select(i => i.RepositoryId).ToList();
            if (repositoryIds.Any(string.IsNullOrEmpty))
            {
                logger.LogError("Received a {request} with empty repository IDs. Sending error response.", requestName);
                response.IsErrorResponse = true;
                response.ErrorMessage = "Received a an empty repository identifier. Cannot process dry run.";
                return Ok(response);
            }

            response.PullResults = applicationControlService.PerformGitPullDryRun(entries);
            logger.LogInformation("Successfully performed git pull simulation for {request}", requestName);
            return Ok(response);
        }

        /// <summary>
        /// Simulates adding a branch to the specified repositories and returns the results of the simulation without making any actual changes to the repositories.
        /// </summary>
        /// <param name="request">The request to simulate branch additions.</param>
        /// <returns>The branch addition simulation response.</returns>
        [HttpPost]
        [Route("simulateAddBranch")]
        public ActionResult<SimulateAddBranchResponse> SimulateAddBranch([FromBody] SimulateAddBranchRequest request)
        {
            string requestName = nameof(SimulateAddBranchRequest);
            SimulateAddBranchResponse response = new();
            if (request == null)
            {
                response.IsErrorResponse = true;
                response.ErrorMessage = "Request must be populated. Cannot simulate branch addition.";
                logger.LogError("Null {request} was received. Sending error response", requestName);
                return BadRequest(response);
            }

            List<string> branchesToAdd = request.Entries.Select(i => i.BranchToAdd).ToList();
            if (branchesToAdd.Any(string.IsNullOrEmpty))
            {
                response.IsErrorResponse = true;
                response.ErrorMessage = "All branch names must be populated. Cannot simulate branch addition.";
                logger.LogError("Null or empty branch name was received in {request}. Sending error response", requestName);
                return Ok(response);
            }

            List<AddBranchSimulationEntry> entries = request.Entries;
            response.SimulationResults = applicationControlService.PerformAddBranchDryRun(entries);
            logger.LogInformation("Successfully performed add branch simulation for {request}", requestName);
            return Ok(response);
        }

        /// <summary>
        /// Simulates merging one branch into another for the specified repositories and returns the results of the simulation without making any actual changes to the repositories.
        /// </summary>
        /// <param name="request">The request to simulate branch merges in the specified repositories.</param>
        /// <returns>The response to simulate branch merges.</returns>
        [HttpPost]
        [Route("simulateMergeBranches")]
        public ActionResult<SimulateBranchMergeResponse> SimulateMergeBranches([FromBody] SimulateBranchMergeRequest request)
        {
            string requestName = nameof(SimulateBranchMergeRequest);
            SimulateBranchMergeResponse response = new();
            if (request == null)
            {
                response.IsErrorResponse = true;
                response.ErrorMessage = "Request must be populated. Cannot simulate branch merge.";
                logger.LogError("Null {request} was received. Sending error response", requestName);
                return BadRequest(response);
            }

            List<MergeSimulationEntry> entries = request.MergeEntries;
            response.SimulationResults = applicationControlService.PerformMergeBranchDryRun(entries);
            logger.LogInformation("Successfully performed merge branches simulation for {request}", requestName);
            return Ok(response);
        }
    }
}
