namespace ChasmaWebApi.Data.Objects
{
    /// <summary>
    /// Class representing a patch entry in a git repository.
    /// </summary>
    public class PatchEntry
    {
        /// <summary>
        /// Gets or sets the file path of the patch entry.
        /// </summary>
        public string FilePath { get; set; }

        /// <summary>
        /// Gets or sets the diff content of the patch entry.
        /// </summary>
        public string Diff { get; set; }
    }
}
