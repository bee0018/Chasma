namespace ChasmaWebApi.Data.Responses
{
    /// <summary>
    /// Class representing the components of a Git Branch Message
    /// </summary>
    public class GitBranchResponse : ResponseBase
    {
        /// <summary>
        /// Gets or sets the branch names of the specified repository.
        /// </summary>
        public List<string> BranchNames { get; set; } = new();
    }
}
