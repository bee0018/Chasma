using ChasmaWebApi.Data.Objects;

namespace ChasmaWebApi.Messages;

/// <summary>
/// Class representing the components of a Heartbeat Message
/// </summary>
public class HeartbeatMessage
{
    /// <summary>
    /// Gets or sets the message of the heartbeat.
    /// </summary>
    public string Message { get;  set; }
    
    /// <summary>
    /// Gets or sets the status of the heartbeat of the message.
    /// </summary>
    public HeartbeatStatus Status { get;  set; }
}

