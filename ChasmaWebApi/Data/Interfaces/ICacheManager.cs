using System.Collections.Concurrent;
using ChasmaWebApi.Data.Models;
using ChasmaWebApi.Data.Objects;

namespace ChasmaWebApi.Data.Interfaces;

/// <summary>
/// Interface containing the members that this application stores resources for.
/// </summary>
public interface ICacheManager
{
    /// <summary>
    /// Gets the validated local git repositories found on the system.
    /// </summary>
    ConcurrentDictionary<string, LocalGitRepository> Repositories { get; }

    /// <summary>
    /// Gets the mapping of repository identifiers to working directories for the repos in the system.
    /// </summary>
    ConcurrentDictionary<string, string> WorkingDirectories { get; }

    /// <summary>
    /// Gets the mapping of user identifiers to users in the system.
    /// </summary>
    ConcurrentDictionary<int, UserAccountModel> Users { get; }

    /// <summary>
    /// Gets the mapping of GitHub pull request numbers to pull request details.
    /// </summary>
    ConcurrentDictionary<int, GitHubPullRequest> GitHubPullRequests { get; }
}