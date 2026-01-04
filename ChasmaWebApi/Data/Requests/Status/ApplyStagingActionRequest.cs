using ChasmaWebApi.Util;

namespace ChasmaWebApi.Data.Requests.Status;

/// <summary>
/// Class representing components for staging/unstaging a file.
/// </summary>
public class ApplyStagingActionRequest : ChasmaXmlBase
{
    /// <summary>
    /// Gets or sets the repository key.
    /// </summary>
    public string RepoKey { get; set; }
    
    /// <summary>
    /// Gets or sets the file name.
    /// </summary>
    public string FileName { get; set; }
    
    /// <summary>
    /// Gets or sets a value indicating whether the file is being staged.
    /// </summary>
    public bool IsStaging { get; set; }
}