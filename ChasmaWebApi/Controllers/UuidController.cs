using Microsoft.AspNetCore.Mvc;

namespace ChasmaWebApi.Controllers;

/// <summary>
/// Class containing the routes for interacting and performing operations with UUIDs.
/// </summary>
[Route("api")]
public class UuidController : ControllerBase
{
    /// <summary>
    /// The internal logger for logging status.
    /// </summary>
    private readonly ILogger<UuidController> logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="UuidController"/> class.
    /// </summary>
    /// <param name="log">The internal server logger.</param>
    public UuidController(ILogger<UuidController> log)
    {
        logger = log;
    }

    /// <summary>
    /// Generates the UUID upon request.
    /// </summary>
    /// <returns>A response with the generate UUID.</returns>
    [HttpGet]
    [Route("uuid")]
    public ActionResult<string> GenerateUuid()
    {
        logger.LogInformation("Received a GET request to generate a UUID.");
        string uuid = Guid.NewGuid().ToString();
        logger.LogInformation("Successfully Generated UUID.");
        return Ok(uuid);
    }
}