using ChasmaWebApi.Data.Messages;
using ChasmaWebApi.Data.Objects.Status;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Diagnostics;

namespace ChasmaWebApi.Controllers;

/// <summary>
/// Controller representing the health monitor of the web API.
/// </summary>
[Authorize]
[ApiController]
[Route("api/[controller]")]
public class HealthController : ControllerBase
{
    /// <summary>
    /// The internal API logger.
    /// </summary>
    private readonly ILogger<HealthController> logger;

    /// <summary>
    /// The application lifetime manager.
    /// </summary>
    private readonly IHostApplicationLifetime applicationLifetime;

    /// <summary>
    /// Instantiates a new <see cref="HealthController"/> class.
    /// </summary>
    /// <param name="log">The internal API logger.</param>
    /// <param name="lifetimeManager">The application lifetime manager.</param>
    public HealthController(ILogger<HealthController> log, IHostApplicationLifetime lifetimeManager)
    {
        logger = log;
        applicationLifetime = lifetimeManager;
    }
    
    /// <summary>
    /// Handles when a heartbeat request is received.
    /// </summary>
    /// <returns>A heartbeat message.</returns>
    [HttpGet]
    [Route("heartbeat")]
    [AllowAnonymous]
    public ActionResult<HeartbeatMessage> GetHeartbeat()
    {
        logger.LogTrace("Received request to get heartbeat at {now}.", DateTimeOffset.Now);
        HeartbeatMessage heartbeatMessage = new()
        {
            Message = $"Heartbeat has been registered at {DateTimeOffset.Now}",
            Status = HeartbeatStatus.Ok,
        };
        return Ok(heartbeatMessage);
    }

    /// <summary>
    /// Handles the request to restart the application.
    /// </summary>
    /// <returns>The message to restarting the application.</returns>
    [HttpPost]
    [Route("restart")]
    [AllowAnonymous]
    public ActionResult<HeartbeatMessage> RestartApplication()
    {
        HeartbeatMessage heartbeatMessage;
        Process currentProcess = Process.GetCurrentProcess();
        string executablePath = currentProcess.MainModule?.FileName;
        if (string.IsNullOrEmpty(executablePath))
        {
            heartbeatMessage = new()
            {
                Message = "Unable to determine the executable path for restarting.",
                Status = HeartbeatStatus.Error,
            };
            return Ok(heartbeatMessage);
        }

        try
        {
            ChasmaWebApiConfigurations apiConfiguration = ChasmaWebApiConfigurations.GetApiConfig();
            ProcessStartInfo startInfo = new()
            {
                FileName = executablePath,
                WorkingDirectory = Directory.GetCurrentDirectory(),
                UseShellExecute = true
            };
            Process.Start(startInfo);
            applicationLifetime.StopApplication();

            heartbeatMessage = new()
            {
                Message = "Restarting the Emryce software...",
                Status = HeartbeatStatus.Ok,
            };
            return Ok(heartbeatMessage);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "An error occurred while trying to restart the application at {now}.", DateTimeOffset.Now);
            heartbeatMessage = new()
            {
                Message = "An error occurred while trying to restart the application.",
                Status = HeartbeatStatus.Error,
            };
            return Ok(heartbeatMessage);
        }
    }

    /// <summary>
    /// Handles the request to stop the application.
    /// </summary>
    /// <returns>The message to stopping the application.</returns>
    [HttpPost]
    [Route("stopApplication")]
    public ActionResult<HeartbeatMessage> StopApplication()
    {
        HeartbeatMessage heartbeatMessage;
        try
        {
            applicationLifetime.StopApplication();
            heartbeatMessage = new()
            {
                Message = "Stopping the Emryce software...",
                Status = HeartbeatStatus.Error,
            };
            return Ok(heartbeatMessage);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "An error occurred while trying to stop the application at {now}.", DateTimeOffset.Now);
            heartbeatMessage = new()
            {
                Message = "An error occurred while trying to stop the application.",
                Status = HeartbeatStatus.Error,
            };
            return Ok(heartbeatMessage);
        }
    }
}