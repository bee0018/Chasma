using ChasmaWebApi.Core.Interfaces.Git;
using ChasmaWebApi.Data.Models;
using ChasmaWebApi.Data.Objects.Application;
using ChasmaWebApi.Data.Objects.Git;
using LibGit2Sharp;

namespace ChasmaWebApi.Core.Services.Git
{
    /// <summary>
    /// Provides operations for managing Git stashes, such as creating, listing, and applying stashes within a repository.
    /// </summary>
    public class GitStashService : IGitStashService
    {
        /// <summary>
        /// The logger instance for logging diagnostic and operational information within class.
        /// </summary>
        private readonly ILogger<GitStashService> Logger;

        /// <summary>
        /// Initializes a new instance of the <see cref="GitStashService"/> class.
        /// </summary>
        /// <param name="logger">The internal API logger.</param>
        public GitStashService(ILogger<GitStashService> logger)
        {
            Logger = logger;
        }

        // <inheritdoc/>
        public bool TryAddStash(string workingDirectory, ApplicationUser user, string stashMessage, StashModifiers stashOptions, out string errorMessage)
        {
            errorMessage = string.Empty;
            try
            {
                using Repository repository = new(workingDirectory);
                Signature author = new(user.Name, user.Email, DateTimeOffset.Now);
                Stash stash = repository.Stashes.Add(author, stashMessage, stashOptions);
                if (stash != null)
                {
                    Logger.LogInformation("Successfully created stash {stashName} in repository at {repoPath}.", stash.CanonicalName, workingDirectory);
                }

                return true;
            }
            catch (Exception e)
            {
                errorMessage = $"An error occurred while creating a stash in the repository at {workingDirectory}: {e.Message}";
                Logger.LogError("An error occurred while creating a stash in the repository at {repoPath}: {error}. Sending error response.", workingDirectory, e);
                return false;
            }
        }

        // <inheritdoc/>
        public List<StashEntry>? GetStashList(string workingDirectory, out string errorMessage)
        {
            errorMessage = string.Empty;
            try
            {
                using Repository repo = new(workingDirectory);
                List<StashEntry> stashEntries = new();
                int index = 0;
                foreach (Stash stash in repo.Stashes)
                {
                    // Stash index is not directly accessible, so we will have to keep track of it manually.
                    StashEntry stashEntry = new()
                    {
                        Index = index,
                        StashMessage = stash.Message,
                    };
                    stashEntries.Add(stashEntry);
                    index++;
                }

                return stashEntries;
            }
            catch (Exception e)
            {
                errorMessage = $"An error occurred while retrieving the stash list for the repository. Check internal server logs for more information.";
                Logger.LogError("An error occurred while retrieving the stash list for the repository at {repoPath}: {error}. Sending error response.", workingDirectory, e);
                return null;
            }
        }

        // <inheritdoc/>
        public List<PatchEntry>? GetStashDetails(string workingDirectory, StashEntry stashEntry, out string errorMessage)
        {
            errorMessage = string.Empty;
            if (stashEntry == null)
            {
                errorMessage = "Stash entry cannot be null.";
                Logger.LogError("Received a null stash entry. Cannot get stash details. Sending error response.");
                return null;
            }

            try
            {
                using Repository repo = new(workingDirectory);
                int index = stashEntry.Index;
                Stash? stash = repo.Stashes[index];
                if (stash == null)
                {
                    errorMessage = $"No stash found at index {index} in repository.";
                    Logger.LogError("Could not find stash at index {index} for working directory {workingDirectory}.", index, workingDirectory);
                    return null;
                }

                // Diff base → stash (working tree changes)
                List<PatchEntry> patchEntries = new();
                CompareOptions options = new()
                {
                    Similarity = SimilarityOptions.Renames,
                };
                Patch patches = repo.Diff.Compare<Patch>(stash.Base.Tree, stash.WorkTree.Tree, options);
                foreach (PatchEntryChanges entry in patches)
                {
                    PatchEntry patchEntry = new()
                    {
                        FilePath = entry.Path,
                        Diff = entry.Patch,
                    };
                    patchEntries.Add(patchEntry);
                }

                return patchEntries;
            }
            catch (Exception e)
            {
                errorMessage = $"An error occurred while retrieving the stash list for the repository. Check internal server logs for more information.";
                Logger.LogError("An error occurred while retrieving the stash list for the repository at {repoPath}: {error}. Sending error response.", workingDirectory, e);
                return null;
            }
        }

        // <inheritdoc/>
        public bool TryApplyStash(string workingDirectory, int stashIndex, StashApplyModifiers stashApplyOptions, out string errorMessage)
        {
            errorMessage = string.Empty;
            try
            {
                using Repository repository = new(workingDirectory);
                Stash stash = repository.Stashes.ElementAtOrDefault(stashIndex);
                if (stash == null)
                {
                    errorMessage = $"No stash found at index {stashIndex} in repository at {workingDirectory}.";
                    Logger.LogError(errorMessage);
                    return false;
                }

                StashApplyOptions applyOptions = new() { ApplyModifiers = stashApplyOptions };
                StashApplyStatus status = repository.Stashes.Apply(stashIndex, applyOptions);
                if (status != StashApplyStatus.Applied)
                {
                    errorMessage = $"Failed to apply stash at index {stashIndex} in repository at {workingDirectory}.";
                    Logger.LogError("Failed to apply stash at index {indexNumber} and finished with status code: {code}", stashIndex, status);
                    return false;
                }

                Logger.LogInformation("Successfully applied stash {stashName} in repository at {repoPath}.", stash.CanonicalName, workingDirectory);
                return true;
            }
            catch (Exception e)
            {
                errorMessage = $"An error occurred while applying a stash in the repository at {workingDirectory}: {e.Message}";
                Logger.LogError("An error occurred while applying a stash in the repository at {repoPath}: {error}. Sending error response.", workingDirectory, e);
                return false;
            }
        }

        // <inheritdoc/>
        public bool TryRemoveStash(string workingDirectory, int stashIndex, out string errorMessage)
        {
            errorMessage = string.Empty;
            try
            {
                using Repository repository = new(workingDirectory);
                Stash stash = repository.Stashes.ElementAtOrDefault(stashIndex);
                if (stash == null)
                {
                    errorMessage = $"No stash found at index {stashIndex} in repository at {workingDirectory}.";
                    Logger.LogError(errorMessage);
                    return false;
                }

                repository.Stashes.Remove(stashIndex);
                Logger.LogInformation("Successfully dropped stash {stashName} in repository at {repoPath}.", stash.CanonicalName, workingDirectory);
                return true;
            }
            catch (Exception e)
            {
                errorMessage = $"An error occurred while dropping a stash in the repository at {workingDirectory}: {e.Message}";
                Logger.LogError("An error occurred while dropping a stash in the repository at {repoPath}: {error}. Sending error response.", workingDirectory, e);
                return false;
            }
        }

        // <inheritdoc/>
        public bool TryPopStash(string workingDirectory, out string errorMessage)
        {
            errorMessage = string.Empty;
            try
            {
                using Repository repository = new(workingDirectory);
                if (!repository.Stashes.Any())
                {
                    errorMessage = $"No stashes found in repository at {workingDirectory}.";
                    Logger.LogError(errorMessage);
                    return false;
                }

                if (repository.Stashes.Count() == 0)
                {
                    errorMessage = $"No stashes found in repository at {workingDirectory}.";
                    Logger.LogError(errorMessage);
                    return false;
                }

                StashApplyStatus status = repository.Stashes.Pop(0);
                if (status != StashApplyStatus.Applied)
                {
                    errorMessage = $"Failed to pop the latest stash in repository at {workingDirectory}.";
                    Logger.LogError("Failed to pop the latest stash and finished with status code: {code}", status);
                    return false;
                }

                Logger.LogInformation("Successfully popped the latest stash in repository at {repoPath}.", workingDirectory);
                return true;
            }
            catch (Exception e)
            {
                errorMessage = $"An error occurred while popping a stash in the repository at {workingDirectory}: {e.Message}";
                Logger.LogError("An error occurred while popping a stash in the repository at {repoPath}: {error}. Sending error response.", workingDirectory, e);
                return false;
            }
        }
    }
}
