using ChasmaWebApi.Data.Objects;
using ChasmaWebApi.Messages;
using Microsoft.AspNetCore.Mvc;

namespace ChasmaWebApi.Controllers;

/// <summary>
/// Controller representing the health monitor of the web API.
/// </summary>
[Route("api/[controller]")]
public class HealthController : ControllerBase
{
    /// <summary>
    /// The internal API logger.
    /// </summary>
    private readonly ILogger<HealthController> logger;
    
    /// <summary>
    /// Instantiates a new <see cref="HealthController"/> class.
    /// </summary>
    /// <param name="log">The internal API logger.</param>
    public HealthController(ILogger<HealthController> log)
    {
        logger = log;
    }
    
    /// <summary>
    /// Handles when a heartbeat request is received.
    /// </summary>
    /// <returns>A heartbeat message.</returns>
    [HttpGet]
    [Route("heartbeat")]
    public ActionResult<HeartbeatMessage> GetHeartbeat()
    {
        logger.LogTrace("Received request to get heartbeat at {}.", DateTimeOffset.Now);
        HeartbeatMessage heartbeatMessage = new()
        {
            Message = $"Heartbeat has been registered at {DateTimeOffset.Now}",
            Status = HeartbeatStatus.Ok,
        };
        return Ok(heartbeatMessage);
    }
}