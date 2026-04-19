using System.Collections.Concurrent;
using ChasmaWebApi.Data.Models;
using ChasmaWebApi.Data.Objects.Application;
using ChasmaWebApi.Data.Objects.Git;
using ChasmaWebApi.Data.Objects.Remote;

namespace ChasmaWebApi.Core.Interfaces.Infrastructure;

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
    ConcurrentDictionary<int, ApplicationUser> Users { get; }

    /// <summary>
    /// Gets the mapping of GitHub pull request numbers to pull request details.
    /// </summary>
    ConcurrentDictionary<long, RemotePullRequest> GitHubPullRequests { get; }

    /// <summary>
    /// Gets the mapping of GitLab merge request numbers to merge request details.
    /// </summary>
    ConcurrentDictionary<long, RemotePullRequest> GitLabMergeRequests { get; }
}