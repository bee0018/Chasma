using ChasmaWebApi.Data.Objects;
using ChasmaWebApi.Responses;

namespace ChasmaWebApi.Messages;

/// <summary>
/// Class representing the components of a Heartbeat Message
/// </summary>
public class HeartbeatMessage : ResponseBase
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

