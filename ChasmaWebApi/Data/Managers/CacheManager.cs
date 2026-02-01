using System.Collections.Concurrent;
using ChasmaWebApi.Data.Interfaces;
using ChasmaWebApi.Data.Models;
using ChasmaWebApi.Data.Objects;

namespace ChasmaWebApi.Data.Managers;

/// <summary>
/// The concrete implementation of the <see cref="ICacheManager"/>.
/// </summary>
public class CacheManager : ICacheManager
{
    // <inheritdoc />
    public ConcurrentDictionary<string, LocalGitRepository> Repositories { get; } = new();

    // <inheritdoc />
    public ConcurrentDictionary<string, string> WorkingDirectories { get; } = new();

    // <inheritdoc />
    public ConcurrentDictionary<int, UserAccountModel> Users { get; } = new();

    // <inheritdoc />
    public ConcurrentDictionary<int, GitHubPullRequest> GitHubPullRequests { get; } = new();
}