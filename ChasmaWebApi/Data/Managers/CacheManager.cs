using System.Collections.Concurrent;
using ChasmaWebApi.Data.Interfaces;
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
    public HashSet<string> WorkingDirectories { get; } = new();
}