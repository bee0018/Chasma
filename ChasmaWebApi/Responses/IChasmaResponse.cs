namespace ChasmaWebApi.Responses
{
    /// <summary>
    /// Interface containing the members of all responses in the Chasma Web API.
    /// </summary>
    public interface IChasmaResponse
    {
        /// <summary>
        /// Gets the flag indicating whether the response is an error or not.
        /// </summary>
        bool IsErrorMessage { get; }

        /// <summary>
        /// Gets the message of the response.
        /// </summary>
        string Message { get; }
    }
}
