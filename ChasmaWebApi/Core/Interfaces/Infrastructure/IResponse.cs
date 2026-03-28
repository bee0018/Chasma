namespace ChasmaWebApi.Core.Interfaces.Infrastructure;

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

    /// <summary>
    /// Gets the status code of the response.
    /// </summary>
    int StatusCode { get; }
}