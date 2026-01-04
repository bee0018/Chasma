using ChasmaWebApi.Data.Interfaces;
using Octokit;

namespace ChasmaWebApi.Data.Managers;

/// <summary>
/// Class representing the base functionality of a client manager.
/// </summary>
/// <param name="logger">The internal API logger.</param>
/// <param name="cacheManager">The internal cache manager.</param>
/// <param name="apiConfigurations">The web API configurations.</param>
/// <typeparam name="T">The class type.</typeparam>
public class ClientManagerBase<T>(ILogger<T> logger, ICacheManager cacheManager, ChasmaWebApiConfigurations apiConfigurations)
{
    /// <summary>
    /// Gets the GitHub API client.
    /// </summary>
    protected GitHubClient Client { get; set; }
    
    /// <summary>
    /// Gets the internal API logger.
    /// </summary>
    protected ILogger ClientLogger { get; } = logger;
    
    /// <summary>
    /// Gets the internal API Cache Manager.
    /// </summary>
    protected ICacheManager CacheManager { get; } = cacheManager;

    /// <summary>
    /// Gets the web API configurations.
    /// </summary>
    protected ChasmaWebApiConfigurations ApiConfigurations { get; } = apiConfigurations;

    /// <summary>
    /// Creates a GitHub client for the specified repository.
    /// </summary>
    /// <param name="repositoryName">The name of the repository.</param>
    /// <returns>The GitHub client for the specified repository.</returns>
    protected GitHubClient CreateGitHubClient(string repositoryName)
    {
        ProductHeaderValue productHeader = new(repositoryName);
        return new(productHeader) { Credentials = new(ApiConfigurations.GitHubApiToken) };
    }
}