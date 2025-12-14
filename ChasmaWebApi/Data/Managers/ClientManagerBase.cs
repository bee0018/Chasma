using Octokit;

namespace ChasmaWebApi.Data.Managers;

/// <summary>
/// Class representing the base functionality of a client manager.
/// </summary>
/// <param name="logger">The internal API logger.</param>
/// <typeparam name="T">The class type.</typeparam>
public class ClientManagerBase<T>(ILogger<T> logger)
{
    /// <summary>
    /// Gets or sets the GitHub API client.
    /// </summary>
    protected GitHubClient Client { get; set; }
    
    /// <summary>
    /// Gets or sets the internal API logger.
    /// </summary>
    protected ILogger ClientLogger { get; } = logger;
}