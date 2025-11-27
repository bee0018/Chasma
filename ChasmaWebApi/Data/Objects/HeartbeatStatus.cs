namespace ChasmaWebApi.Data.Objects;

/// <summary>
/// Enumeration representing the different status of a heartbeat check.
/// </summary>
public enum HeartbeatStatus
{
    /// <summary>
    /// Status indicating that the connection is OK.
    /// </summary>
    Ok,
    
    /// <summary>
    /// Status indicating that the connection is in ERROR.
    /// </summary>
    Error,
}