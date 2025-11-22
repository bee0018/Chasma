using System.Collections.Concurrent;

namespace ChasmaWebApi.Data;

/// <summary>
/// Class containing the data repositories in this application. 
/// </summary>
public static class CacheManager
{
    /// <summary>
    /// The mapping of user identifiers to its corresponding encoded token.
    /// </summary>
    public static readonly ConcurrentDictionary<string, string> EncodedTokenMappings = new();
}