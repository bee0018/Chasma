using ChasmaWebApi.Data.Objects;
using ChasmaWebApi.Util;

namespace ChasmaWebApi.Data.Messages;

/// <summary>
/// Class representing the git repository information found on the local filesystem.
/// </summary>
public class LocalRepositoriesInfoMessage : ChasmaXmlBase
{
    /// <summary>
    /// Gets or sets the timestamp at which the repositories were retrieved on the filesystem.
    /// </summary>
    public DateTimeOffset Timestamp { get; set; }
    
    /// <summary>
    /// Gets or sets the validated local git repositories on the filesystem.
    /// </summary>
    public List<LocalGitRepository> Repositories { get; set; }
}