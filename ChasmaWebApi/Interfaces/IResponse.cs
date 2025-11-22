namespace ChasmaWebApi.Interfaces;

/// <summary>
/// Interface containing the members that will be inherited by Chasma responses.
/// </summary>
public interface IResponse
{
    /// <summary>
    /// Gets a flag indicating whether the response is an error response.
    /// </summary>
    bool IsErrorResponse { get; }
    
    /// <summary>
    /// Gets the error message.
    /// </summary>
    string? ErrorMessage { get; }
}