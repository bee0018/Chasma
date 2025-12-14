using LibGit2Sharp;

namespace ChasmaWebApi.Data.Objects;

/// <summary>
/// Class representing the properties of a local git repository on the local machine.
/// </summary>
public class LocalGitRepository
{
    /// <summary>
    /// Gets or sets the local repository identifier.
    /// </summary>
    public string Id { get; set; }
    
    /// <summary>
    /// Gets or sets the name of the repository.
    /// </summary>
    public string Name { get; set; }
    
    /// <summary>
    /// Gets or sets the name of the repository owner.
    /// </summary>
    public string Owner { get; set; }
    
    /// <summary>
    /// Gets or sets the url of the git repository.
    /// </summary>
    public string Url { get; set; }
}