using ChasmaWebApi.Data.Objects;

namespace ChasmaWebApi.Data.Responses
{
    /// <summary>
    /// Class representing the response to a delete repository request.
    /// </summary>
    public class DeleteRepositoryResponse : ResponseBase
    {
        /// <summary>
        /// Gets or sets the list of existing repositories after the deletion operation.
        /// </summary>
        public List<LocalGitRepository> Repositories { get; set; }
    }
}
