using ChasmaWebApi.Data.Interfaces;
using Octokit;

namespace ChasmaWebApi.Data.Managers;

/// <summary>
/// Class representing the base functionality of a client manager.
/// </summary>
/// <param name="logger">The internal API logger.</param>
/// <param name="cacheManager">The internal cache manager.</param>
/// <typeparam name="T">The class type.</typeparam>
public class ClientManagerBase<T>(ILogger<T> logger,  ICacheManager cacheManager)
{
    /// <summary>
    /// Gets or sets the GitHub API client.
    /// </summary>
    protected GitHubClient Client { get; set; }
    
    /// <summary>
    /// Gets or sets the internal API logger.
    /// </summary>
    protected ILogger ClientLogger { get; } = logger;
    
    /// <summary>
    /// Gets or sets the internal API Cache Manager.
    /// </summary>
    protected ICacheManager CacheManager { get; } = cacheManager;
}