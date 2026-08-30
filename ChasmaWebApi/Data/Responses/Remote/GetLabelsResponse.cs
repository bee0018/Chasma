namespace ChasmaWebApi.Data.Responses.Remote
{
    /// <summary>
    /// Represents a response to a request for GitHub labels for a repository.
    /// </summary>
    public class GetLabelsResponse : ResponseBase
    {
        public List<string> Labels { get; set; } = [];
    }
}
