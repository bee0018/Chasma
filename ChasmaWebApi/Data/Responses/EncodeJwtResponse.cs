namespace ChasmaWebApi.Data.Responses;

/// <summary>
/// Class representing the encoded JSON web token details.
/// </summary>
public class EncodeJwtResponse : ResponseBase
{
    /// <summary>
    /// Gets or sets the encoded JSON web token.
    /// </summary>
    public string Token { get; set; }

    /// <summary>
    /// Gets or sets the time the token expires.
    /// </summary>
    public DateTime ExpirationTime { get; set; }
}