using ChasmaWebApi.Interfaces;
using ChasmaWebApi.Util;

namespace ChasmaWebApi.Responses;

/// <summary>
/// Class representing the base behavior and components of a response that is sent to a client.
/// </summary>
public class ResponseBase : ChasmaXmlBase, IResponse
{
    // <inheritdoc/>
    public bool IsErrorResponse { get; set; }
    
    // <inheritdoc/>
    public string? ErrorMessage { get; set; }
}