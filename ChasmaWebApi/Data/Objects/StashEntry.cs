namespace ChasmaWebApi.Data.Objects
{
    /// <summary>
    /// Class representing a stash entry in a git repository.
    /// </summary>
    public class StashEntry
    {
        /// <summary>
        /// Gets or sets the stash index.
        /// </summary>
        public int Index { get; set; }

        /// <summary>
        /// Gets or sets the stash message.
        /// </summary>
        public string StashMessage { get; set; }
    }
}
