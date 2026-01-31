import React, { useState } from "react";
import {GitCommand} from "../../types/CustomTypes";

/** The git commands to display. **/
const gitCommands: GitCommand[] = [
    // Status / Inspection
    { command: "git status", description: "Shows the current state of the working directory and staging area." },
    { command: "git diff", description: "Shows unstaged changes between working directory and last commit." },
    { command: "git diff --staged", description: "Shows differences between staged changes and the last commit." },
    { command: "git log --oneline", description: "Displays a compact history of commits." },
    { command: "git log --graph --oneline --all", description: "Displays a visual commit history graph of all branches." },
    { command: "git log -p", description: "Shows patch (diff) introduced in each commit." },
    { command: "git log --author='<name>'", description: "Filters commits by author." },
    { command: "git log --since='2.weeks'", description: "Shows commits from the last 2 weeks." },
    { command: "git shortlog", description: "Summarizes commits by author." },
    { command: "git show <commit>", description: "Displays details of a specific commit." },
    { command: "git show-branch", description: "Shows branches and their commits in a compact view." },
    { command: "git describe --tags", description: "Shows the most recent tag reachable from a commit." },
    { command: "git blame <file>", description: "Shows who last modified each line of a file." },

    // Staging / Committing
    { command: "git add .", description: "Stages all changes in the working directory for the next commit." },
    { command: "git commit -m '<message>'", description: "Records staged changes with a descriptive message." },
    { command: "git reset <file>", description: "Unstages a file while keeping its changes in the working directory." },
    { command: "git reset --hard", description: "Resets staging area and working directory to last commit (destructive!)." },
    { command: "git reset --soft <commit>", description: "Resets HEAD to a commit but leaves changes staged." },
    { command: "git rm <file>", description: "Removes a file from the working directory and stages the deletion." },
    { command: "git mv <old> <new>", description: "Renames a file and stages the change." },

    // Branching / Merging
    { command: "git branch", description: "Lists all local branches in the repository." },
    { command: "git branch -r", description: "Lists remote-tracking branches." },
    { command: "git branch -d <branch>", description: "Deletes a local branch." },
    { command: "git branch -m <old> <new>", description: "Renames a branch." },
    { command: "git checkout -b <branch>", description: "Creates and switches to a new branch." },
    { command: "git merge <branch>", description: "Merges another branch into the current branch." },
    { command: "git merge --no-ff <branch>", description: "Merges a branch creating a merge commit even if fast-forward possible." },
    { command: "git merge --abort", description: "Aborts a merge in progress and resets to pre-merge state." },
    { command: "git rebase main", description: "Reapplies commits on top of the latest main branch." },
    { command: "git cherry-pick <commit>", description: "Applies a specific commit from another branch to the current branch." },

    // Remote / Syncing
    { command: "git remote -v", description: "Shows all configured remote repositories." },
    { command: "git remote show origin", description: "Displays detailed information about a remote repository." },
    { command: "git remote add origin <url>", description: "Adds a new remote repository." },
    { command: "git fetch", description: "Downloads changes from the remote without modifying local files." },
    { command: "git fetch --all", description: "Fetches updates from all remotes." },
    { command: "git pull", description: "Fetches and merges changes from the remote repository." },
    { command: "git pull --rebase", description: "Fetches changes and rebases instead of merging." },
    { command: "git push -u origin <branch>", description: "Pushes the branch to the remote repository and sets upstream tracking." },
    { command: "git push --force", description: "Forces push to a remote branch (can overwrite history)." },
    { command: "git push --tags", description: "Pushes all local tags to the remote." },
    { command: "git tag <tagname>", description: "Creates a tag for the current commit." },

    //  Undo / History / Logging
    { command: "git revert <commit>", description: "Creates a new commit that undoes changes from a previous commit." },
    { command: "git reflog", description: "Shows a log of all recent actions, including commits, resets, and checkouts." },
    { command: "git reflog expire --expire=now --all", description: "Expires reflog entries immediately." },

    //  Stash / Cleanup
    { command: "git stash", description: "Temporarily saves changes that are not ready to commit." },
    { command: "git stash pop", description: "Applies stashed changes and removes them from the stash list." },
    { command: "git clean -f", description: "Removes untracked files from the working directory." },
    { command: "git clean -fd", description: "Removes untracked files and directories." },
    { command: "git submodule add <repo> <path>", description: "Adds a submodule repository." },
    { command: "git submodule update --init --recursive", description: "Initializes and updates submodules." },

    // Advanced / Maintenance / Misc
    { command: "git bisect start", description: "Starts a bisect session to find the commit that introduced a bug." },
    { command: "git bisect bad", description: "Marks the current commit as bad in a bisect session." },
    { command: "git bisect good <commit>", description: "Marks a commit as good in a bisect session." },
    { command: "git archive --format=zip HEAD > repo.zip", description: "Creates a zip archive of the current repository state." },
    { command: "git fsck", description: "Verifies the integrity of the repository." },
    { command: "git gc", description: "Cleans up unnecessary files and optimizes the repository." },
    { command: "git bundle create repo.bundle --all", description: "Creates a bundle of the repository for offline transfer." },
    { command: "git verify-commit <commit>", description: "Verifies the GPG signature of a commit." },
];

/**
 * Initializes a new instance of the HelpCommonGitCommandsPage component.
 * @constructor
 */
const HelpCommonGitCommandsPage: React.FC = () => {
    /** Gets or sets the expanded indexes. **/
    const [expandedIndexes, setExpandedIndexes] = useState<number[]>([]);

    /** Gets or sets the search query. **/
    const [searchQuery, setSearchQuery] = useState("");

    /** Gets or sets the index of the command row that is copied. **/
    const [copiedIndex, setCopiedIndex] = useState<number | null>(null);

    /**
     * Toggles the row to expand the description.
     * @param index The row index.
     */
    const toggleExpand = (index: number) => {
        setExpandedIndexes(prev =>
            prev.includes(index) ? prev.filter(i => i !== index) : [...prev, index]
        );
    };

    /**
     * Copies the command to the clipboard.
     * @param command The git command to copy.
     * @param index The row index.
     */
    const copyToClipboard = async (command: string, index: number) => {
        await navigator.clipboard.writeText(command);
        setCopiedIndex(index);
        setTimeout(() => setCopiedIndex(null), 1500);
    };

    /** The filtered git commands by search query. **/
    const filteredCommands = gitCommands.filter(cmd =>
        cmd.command.toLowerCase().includes(searchQuery.toLowerCase())
    );

    return (
        <section id="git-commands" className="panel-card">
            <h2>Common Git Commands</h2>

            <p className="help-intro">
                Frequently used Git commands and what they do. Click a command to expand/collapse its description.
            </p>

            <input
                type="text"
                placeholder="Search commands..."
                value={searchQuery}
                onChange={e => setSearchQuery(e.target.value)}
                className="input-field"
            />

            <div className="git-command-list">
                {filteredCommands.length === 0 ? (
                    <p>No commands match your search.</p>
                ) : (
                    filteredCommands.map((cmd, index) => (
                        <div
                            key={cmd.command}
                            className={`git-command ${
                                expandedIndexes.includes(index) ? "expanded" : ""
                            }`}
                        >
                            <div
                                className="git-command-header"
                                onClick={() => toggleExpand(index)}
                                style={{ cursor: "pointer" }}
                            >
                                <code>{cmd.command}</code>
                                <span style={{ fontSize: "0.85rem", color: "#999" }}>
            {expandedIndexes.includes(index) ? "▾" : "▸"}
          </span>
                            </div>

                            {/* Description + Copy Button */}
                            <div className="git-command-description">
                                <p>{cmd.description}</p>
                                <button
                                    className="copy-button"
                                    onClick={(e) => {
                                        e.stopPropagation(); // prevent toggling expand
                                        copyToClipboard(cmd.command, index);
                                    }}
                                >
                                    {copiedIndex === index ? "Command Copied!" : "Copy Command"}
                                </button>
                            </div>
                        </div>
                    ))
                )}
            </div>
        </section>
    );
};

export default HelpCommonGitCommandsPage;