using System.Collections.Concurrent;
using ChasmaWebApi.Core.Interfaces.Infrastructure;
using ChasmaWebApi.Data.Models;
using ChasmaWebApi.Data.Objects.Git;
using ChasmaWebApi.Data.Objects.Remote;

namespace ChasmaWebApi.Core.Services.Infrastructure;

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