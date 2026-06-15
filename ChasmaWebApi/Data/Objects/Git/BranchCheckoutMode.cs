namespace ChasmaWebApi.Data.Objects.Git
{
    /// <summary>
    /// The branch checkout mode to determine how to handle uncommitted changes when checking out a branch.
    /// </summary>
    public enum BranchCheckoutMode
    {
        /// <summary>
        /// The default branch behavior.
        /// </summary>
        Default,

        /// <summary>
        /// Stashes any uncommitted changes before checking out a new branch.
        /// </summary>
        StashOnly,

        /// <summary>
        /// Stashes any uncommitted changes before checking out a new branch and applies the stashed changes after the checkout is successful.
        /// </summary>
        KeepChanges,

        /// <summary>
        /// Discard all changes before checking out a new branch.
        /// </summary>
        DiscardAll,
    }
}
