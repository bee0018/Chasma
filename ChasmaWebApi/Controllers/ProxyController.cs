using ChasmaWebApi.Core.Interfaces.Infrastructure;
using ChasmaWebApi.Data.Objects.Application;
using ChasmaWebApi.Data.Requests.Configuration;
using ChasmaWebApi.Data.Responses.Configuration;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using System.Reflection;
using System.Text;
using System.Text.Json;

namespace ChasmaWebApi.Controllers
{
    /// <summary>
    /// Class representing the operations for interacting with remote hosts through Proxy workers.
    /// </summary>
    [Authorize]
    [ApiController]
    [Route("api/[controller]")]
    [EnableRateLimiting(ChasmaWebApiConfigurations.RateLimiterPolicy)]
    public class ProxyController : ControllerBase
    {
        /// <summary>
        /// The logger instance for logging diagnostic and operational information within the class.
        /// </summary>
        private readonly ILogger<ProxyController> logger;

        /// <summary>
        /// The internal cache manager.
        /// </summary>
        private readonly ICacheManager cacheManager;

        #region Constructor

        /// <summary>
        /// Initializes a new instance of the <see cref="ProxyController"/> class.
        /// </summary>
        /// <param name="apiLogger">The internal API logger.</param>
        /// <param name="apiCacheManager">The internal API cache manager.</param>
        public ProxyController(ILogger<ProxyController> apiLogger, ICacheManager apiCacheManager)
        {
            logger = apiLogger;
            cacheManager = apiCacheManager;
        }

        #endregion

        /// <summary>
        /// Reports the bug to the Cloudflare worker proxy.
        /// </summary>
        /// <param name="request">The request to submit a bug request.</param>
        /// <returns>The response to reporting bugs to the Cloudflare worker.</returns>
        [HttpPost]
        [Route("reportBugs")]
        public async Task<ActionResult<ReportBugsResponse>> ReportBugToCloudflareWorker([FromBody] ReportBugsRequest request)
        {
            string requestName = nameof(ReportBugsRequest);
            ReportBugsResponse response = new();
            if (request == null)
            {
                logger.LogError("Recieved a {request} that is not populated. Sending error response.", requestName);
                response.IsErrorResponse = true;
                response.ErrorMessage = "Null request recieved. Cannot report bug";
                return BadRequest(response);
            }

            int userId = request.UserId;
            if (!cacheManager.Users.TryGetValue(userId, out ApplicationUser user))
            {
                logger.LogError("User does not exist in cache. Could not report the bug, sending error response.");
                response.IsErrorResponse = true;
                response.ErrorMessage = "User could not be found.";
                return Ok(response);
            }

            string title = request.IssueTitle;
            if (string.IsNullOrEmpty(title))
            {
                logger.LogError("There is no bug title. Sending error response.");
                response.IsErrorResponse = true;
                response.ErrorMessage = "Title must be provided.";
                return Ok(response);
            }

            string description = request.BugDescription;
            if (string.IsNullOrEmpty(description))
            {
                logger.LogError("There is no bug description. Sending error response.");
                response.IsErrorResponse = true;
                response.ErrorMessage = "Description must be provided.";
                return Ok(response);
            }

            Assembly assembly = Assembly.GetEntryAssembly();
            AssemblyName assemblyName = assembly?.GetName();
            string systemVersion = assemblyName?.Version?.ToString() ?? "Unknown";
            using HttpClient client = new();
            var payload = new
            {
                issueTitle = title,
                userEmail = user.Email,
                bugDescription = description,
                appVersion = systemVersion,
            };
            string json = JsonSerializer.Serialize(payload);
            StringContent requestContent = new(json, Encoding.UTF8, "application/json");
            try
            {
                string cloudflareWorker = "https://emryce-issues-reporter.raspy-hill-4be7.workers.dev";
                HttpResponseMessage gitHubResponse = await client.PostAsync(cloudflareWorker, requestContent);
                if (gitHubResponse.IsSuccessStatusCode)
                {
                    response.IsErrorResponse = false;
                    response.ErrorMessage = string.Empty;
                }
                else
                {
                    response.IsErrorResponse = true;
                    response.ErrorMessage = await gitHubResponse.Content.ReadAsStringAsync();
                }

                return Ok(response);
            }
            catch (Exception e)
            {
                logger.LogError("Error when attemtping to submit error report: {error}", e.Message);
                response.IsErrorResponse = true;
                response.ErrorMessage = "Error submitting error report. Review server logs for more information.";
                return Ok(response);
            }
        }
    }
}
