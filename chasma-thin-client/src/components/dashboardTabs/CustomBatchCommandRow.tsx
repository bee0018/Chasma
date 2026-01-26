import React from "react";
import { useCacheStore } from "../../managers/CacheManager";
import { LocalGitRepository } from "../../API/ChasmaWebApiClient";
import {CommandMode, Row} from "../types/CustomTypes";

/** Interface for the members on the batch command row. **/
interface BatchCommandRowProps {
    /** The row identifier. **/
    id: string;

    /** Selected repository ID **/
    repositoryId?: string;

    /** Commands for this row **/
    commands?: Row[];

    /** The action to perform when the user deletes a row. **/
    onDelete: (id: string) => void;

    /** The action to perform when the row changes **/
    onUpdate: (id: string, field: "repositoryId" | "commands", value: string | Row[]) => void;

    /** The current command mode. **/
    commandMode: CommandMode
}

/**
 * A single batch command row that allows selection of a repository and entry of multiple shell commands.
 * @param id The row identifier.
 * @param repositoryId The repository identifier.
 * @param commands The shell commands to execute.
 * @param onDelete The action to fire when the row is deleted.
 * @param onUpdate The action to fire when the row is updated.
 * @param commandMode The current command mode.
 * @constructor
 */
const CustomBatchCommandRow: React.FC<BatchCommandRowProps> = (
    {
        id,
        repositoryId,
        commands = [],
        onDelete,
        onUpdate,
        commandMode,
    }) => {
    /** The cached repositories belonging to the logged-in user. **/
    const repositories = useCacheStore((state) => state.repositories);

    /** Currently selected repository **/
    const selectedRepo: LocalGitRepository | null = repositories?.find(r => r.id === repositoryId) || null;

    /**
     * Handles the event when the repository selection changes.
     * @param repoId The repository identifier.
     */
    const handleRepoChange = (repoId: string) => {
        onUpdate(id, "repositoryId", repoId);
    };

    /** Adds a shell command row to the form. **/
    const addCustomShellCommandRow = () => {
        const newCommands = [...commands, { id: crypto.randomUUID(), first: "", second: "" }];
        onUpdate(id, "commands", newCommands);
    };

    /** Handles custom shell command row changes in the form. **/
    const handleShellCommandChange = (cmdId: string, value: string) => {
        const newCommands = commands.map(c => c.id === cmdId ? { ...c, first: value } : c);
        onUpdate(id, "commands", newCommands);
    };

    /** Deletes the row with the specified command identifier. **/
    const deleteShellCommandRow = (cmdId: string) => {
        const newCommands = commands.filter(c => c.id !== cmdId);
        onUpdate(id, "commands", newCommands);
    };

    return (
        <div className="batch-command-row">
            <button
                className="remove-button"
                title="Remove Repository"
                onClick={() => onDelete(id)}
            >
                x
            </button>
            <button
                className="add-button"
                type="button"
                onClick={addCustomShellCommandRow}
                hidden={commandMode === "uniform"}
            >
                +
            </button>
            <div className="repo-section">
                <select
                    className="repo-dropdown"
                    value={repositoryId ?? ""}
                    onChange={(e) => handleRepoChange(e.target.value)}
                >
                    <option value="">Select Repository</option>
                    {repositories?.map(repo => (
                        <option key={repo.id} value={repo.id}>
                            {repo.name}
                        </option>
                    ))}
                </select>
                {selectedRepo && (
                    <div className="repo-metadata">
                        <div className="repo-meta-line">
                            <strong>Repo Title:</strong> {selectedRepo.name}
                        </div>
                        <div className="repo-meta-line">
                            <strong>Repo ID:</strong> {selectedRepo.id}
                        </div>
                        <div className="repo-meta-line">
                            <strong>Repo Owner:</strong> {selectedRepo.owner}
                        </div>
                    </div>
                )}
            </div>
            {commandMode === "custom" && (
                <div className="command-input-wrapper">
                    {commands.map(cmd => (
                        <div key={cmd.id}>
                            <input
                                type="text"
                                className="command-input"
                                value={cmd.first}
                                onChange={e => handleShellCommandChange(cmd.id, e.target.value)}
                                placeholder="Enter git command (e.g. git pull)"
                            />
                            <button
                                className="remove-button"
                                title="Remove shell command"
                                onClick={() => deleteShellCommandRow(cmd.id)}
                            >
                                −
                            </button>
                        </div>
                    ))}
                    <br />
                </div>
            )}
            <br />
        </div>
    );
};

export default CustomBatchCommandRow;
