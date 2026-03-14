using ChasmaWebApi.Data.Models;
using ChasmaWebApi.Data.Objects.Git;
using LibGit2Sharp;

namespace ChasmaWebApi.Core.Interfaces.Git
{
    /// <summary>
    /// Defines methods for managing Git stash operations within a repository.
    /// </summary>
    public interface IGitStashService
    {
        /// <summary>
        /// Tries to add a stash to the specified repository.
        /// </summary>
        /// <param name="workingDirectory">The working directory.</param>
        /// <param name="user">The specified user.</param>
        /// <param name="stashMessage">The message to mark the stash.</param>
        /// <param name="stashOptions">The stash options.</param>
        /// <param name="errorMessage">The error message.</param>
        /// <returns>True if the changes were stashed successfully; false otherwise.</returns>
        bool TryAddStash(string workingDirectory, UserAccountModel user, string stashMessage, StashModifiers stashOptions, out string errorMessage);

        /// <summary>
        /// Gets the stash list for the specified repository.
        /// </summary>
        /// <param name="workingDirectory">The working directory.</param>
        /// <param name="errorMessage">The error message.</param>
        /// <returns>The list of stash entries of the repository.</returns>
        List<StashEntry>? GetStashList(string workingDirectory, out string errorMessage);

        /// <summary>
        /// Gets the stash details, including the diff of the stashed changes, for the specified stash index.
        /// </summary>
        /// <param name="workingDirectory">The working directory.</param>
        /// <param name="stashEntry">The stash entry.</param>
        /// <param name="errorMessage">The error message.</param>
        /// <returns>The list of patch entries that are included in the stash.</returns>
        List<PatchEntry>? GetStashDetails(string workingDirectory, StashEntry stashEntry, out string errorMessage);

        /// <summary>
        /// Tries to apply the stash with the specified index to the repository.
        /// Note: The stash will be applied with the provided options, such as whether to keep the stash after applying or not.
        /// </summary>
        /// <param name="workingDirectory">The working directory.</param>
        /// <param name="stashIndex">The stash index.</param>
        /// <param name="stashApplyOptions">The options for stashing operations.</param>
        /// <param name="errorMessage">The error message.</param>
        /// <returns>True if the stash was applied; false otherwise.</returns>
        bool TryApplyStash(string workingDirectory, int stashIndex, StashApplyModifiers stashApplyOptions, out string errorMessage);

        /// <summary>
        /// Tries to remove the stash with the specified index from the repository.
        /// </summary>
        /// <param name="workingDirectory">The working directory.</param>
        /// <param name="stashIndex">The stash index.</param>
        /// <param name="errorMessage">The error message.</param>
        /// <returns>True if the stash was removed; false otherwise.</returns>
        bool TryRemoveStash(string workingDirectory, int stashIndex, out string errorMessage);

        /// <summary>
        /// Tries to pop the latest stash from the repository.
        /// </summary>
        /// <param name="workingDirectory">The working directory.</param>
        /// <param name="stashApplyError">The error message when popping the stash.</param>
        /// <returns>True if the stash was popped; false otherwise.</returns>
        bool TryPopStash(string workingDirectory, out string stashApplyError);
    }
}
