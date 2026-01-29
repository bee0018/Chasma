import React, {useEffect, useState} from "react";
import "../../css/DasboardTab.css";
import {
    BatchCommandEntry,
    BatchCommandEntryResult,
    ExecuteBatchShellCommandsRequest,
    ShellClient
} from "../../API/ChasmaWebApiClient";
import NotificationModal from "../modals/NotificationModal";
import {apiBaseUrl} from "../../environmentConstants";
import {useCacheStore} from "../../managers/CacheManager";
import Checkbox from "../Checkbox";
import CustomBatchCommandRow from "./CustomBatchCommandRow";
import {CommandMode} from "../types/CustomTypes";
import {isBlankOrUndefined} from "../../stringHelperUtil";

/** The GitCtrl Shell API client. **/
const shellClient = new ShellClient(apiBaseUrl);

/**
 * Initializes a new BatchOperationsTab class.
 * @constructor
 */
const BatchOperationsTab: React.FC = () => {
    /** Cached repositories. **/
    const repositories = useCacheStore((state) => state.repositories);

    /** Command execution output per repository. **/
    const [output, setOutput] = useState<{
        repoName: string | undefined;
        executedCommand: string | undefined;
        success: boolean | undefined;
        message?: string | undefined;
    }[] | undefined>([]);

    /** Gets or sets the notification **/
    const [notification, setNotification] = useState<{
        title: string;
        message: string | undefined;
        isError: boolean | undefined;
        loading?: boolean;
    } | null>(null);

    /** Batch rows storing repository ID and commands **/
    const [customBatchRows, setCustomBatchRows] = useState<{
        id: string;
        repositoryId?: string;
        commands: { id: string; first: string; second: string }[];
    }[]>([{ id: crypto.randomUUID(), repositoryId: undefined, commands: [] }]);

    /** Gets or sets the command execution mode. **/
    const [commandMode, setCommandMode] = useState<CommandMode>("uniform");

    /** Gets or sets shell commands rows to execute at once. **/
    const [uniformBatchRows, setUniformBatchRows] = useState<{
        id: string;
        repositoryId?: string;
        commands: { id: string; first: string; second: string }[];
    }[]>([{ id: crypto.randomUUID(), repositoryId: undefined, commands: [] }]);

    /** Gets or sets a value indicating whether the user is selecting all repositories to execute commands. **/
    const [isSelectingAllRepositories, setIsSelectingAllRepositories] = useState<boolean>(false);

    /** Adds a new repository row for batch operations. **/
    const addCustomBatchRow = () => {
        setCustomBatchRows(prev => [
            ...prev,
            { id: crypto.randomUUID(), repositoryId: undefined, commands: [] }
        ]);
    };

    /** Deletes a repository batch row. **/
    const deleteCustomBatchRow = (id: string) => {
        setCustomBatchRows(prev => prev.filter(row => row.id !== id));
    };

    /**
     * Updates a custom batch row.
     * @param id The row identifier.
     * @param field "repositoryId" or "commands"
     * @param value The new value.
     */
    const updateCustomBatchRow = (
        id: string,
        field: "repositoryId" | "commands",
        value: string | { id: string; first: string; second: string }[]
    ) => {
        setCustomBatchRows(prev =>
            prev.map(row =>
                row.id === id ? { ...row, [field]: value } : row
            )
        );
    };

    /** Adds a uniform shell command row to the form. **/
    const addUniformShellCommandRow = () => {
        setUniformBatchRows(prev => {
            const row = prev[0];
            return [{
                ...row,
                commands: [
                    ...row.commands,
                    { id: crypto.randomUUID(), first: "", second: "" }
                ]
            }];
        });
    }

    /**
     * Deletes a uniform command batch row.
     * @param cmdId The command identifier.
     */
    const deleteUniformShellCommand = (cmdId: string) => {
        setUniformBatchRows(prev => {
            const row = prev[0];
            return [{
                ...row,
                commands: row.commands.filter(cmd => cmd.id !== cmdId)
            }];
        });
    };

    /**
     * Updates a uniform command batch row.
     * @param cmdId The row identifier.
     * @param value The new value
     */
    const updateUniformShellCommand = (cmdId: string, value: string) => {
        setUniformBatchRows(prev => {
            const row = prev[0];
            return [{
                ...row,
                commands: row.commands.map(cmd =>
                    cmd.id === cmdId ? { ...cmd, first: value } : cmd
                )
            }];
        });
    };

    /**
     * Executes a batch git operation for all repositories.
     */
    const executeBatchOperation = async () => {
        setNotification({
            title: "Executing batch operation...",
            message: "Running git command on all selected repositories.",
            isError: false,
            loading: true,
        });
        const request = new ExecuteBatchShellCommandsRequest();
        request.batchCommands = []
        if (commandMode === "custom") {
            customBatchRows.filter(row => row.repositoryId)
                .forEach(row => {
                    const entry = new BatchCommandEntry();
                    entry.repositoryId = row.repositoryId;
                    entry.commands = row.commands
                        .map(cmd => cmd.first)
                        .filter(cmd => cmd.trim() !== "");
                    request.batchCommands?.push(entry);
                });
        }
        else {
            const repositoryIds = customBatchRows
            .filter(i => i.repositoryId !== undefined)
            .map(row => row.repositoryId);
            const uniformCommands = uniformBatchRows[0]
                .commands
                .map(cmd => cmd.first)
                .filter(cmd => !isBlankOrUndefined(cmd));
            request.batchCommands = repositoryIds.map(repoId => {
                const entry = new BatchCommandEntry();
                entry.repositoryId = repoId;
                entry.commands = uniformCommands;
                return entry;
            });
        }
        setOutput([]);
        try {
            const response = await shellClient.executeBatchShellCommands(request);
            if (response.isErrorResponse) {
                setNotification({
                    title: "Batch operation failed!",
                    message: "Review server logs for more information.",
                    isError: true,
                });
                return;
            }

            setOutput(
                response.results?.map((result: BatchCommandEntryResult) => ({
                    repoName: result.repositoryName,
                    executedCommand: result.executedCommand,
                    success: result.isSuccess,
                    message: result.message,
                }))
            );
            setNotification({
                title: "Batch operation completed!",
                message: "Review the output window for details.",
                isError: false,
            });
        } catch (e) {
            setNotification({
                title: "Error executing batch shell operation failed!",
                message: "Review server logs for more information.",
                isError: true,
            });
            console.error(e);
        }
    };

    /**
     * Closes the modal once the user confirms the message
     */
    const closeModal = () => {
        setNotification(null);
    };

    useEffect(() => {
        if (isSelectingAllRepositories) {
            if (!repositories || repositories.length === 0) return;

            setCustomBatchRows(
                repositories.map(repo => ({
                    id: crypto.randomUUID(),
                    repositoryId: repo.id,
                    commands: []
                }))
            );
        } else {
            // Reset to one empty row when unchecked
            setCustomBatchRows([{ id: crypto.randomUUID(), repositoryId: undefined, commands: [] }]);
        }
    }, [isSelectingAllRepositories, repositories]);

    return (
        <>
            <div className="page">
                <h1 className="page-title" style={{ textAlign: "center" }}>
                    Batch Repository Operations
                </h1>
                <p className="page-description" style={{ textAlign: "center" }}>
                    Execute shell commands across all registered repositories at once.
                </p>
                <div className="command-mode-toggle">
                    <div className="command-mode-toggle-inner">
                        <button
                            className={`command-mode-button ${commandMode === "uniform" ? "active" : ""}`}
                            onClick={() => setCommandMode("uniform")}
                        >
                            Uniform
                        </button>
                        <button
                            className={`command-mode-button ${commandMode === "custom" ? "active" : ""}`}
                            onClick={() => setCommandMode("custom")}
                        >
                            Custom
                        </button>
                    </div>
                </div>
                <div style={{
                    display: "flex",
                    justifyContent: "space-between",
                    alignItems: "center",
                    marginBottom: "8px",
                }}>
                    <Checkbox
                        label={"Select all repositories"}
                        onBoxChecked={setIsSelectingAllRepositories}
                    />
                    <button
                        className="submit-button"
                        hidden={isSelectingAllRepositories}
                        onClick={addCustomBatchRow}
                    >
                        Add Repo
                    </button>
                </div>
                {commandMode === "uniform" && (
                    <div className="batch-command-row">
                        <h3 style={{ color: "#00bfff", marginBottom: "8px" }}>
                            Uniform Shell Commands
                        </h3>
                        <div
                            style={{
                                display: "flex",
                                flexDirection: "column",
                                marginBottom: "10px",
                            }}
                        >
                            {uniformBatchRows[0].commands.map(cmd => (
                                <div
                                    key={cmd.id}
                                    style={{
                                        display: "flex",
                                        alignItems: "center",
                                        gap: "8px",
                                        marginBottom: "8px",
                                    }}
                                >
                                    <input
                                        type="text"
                                        style={{marginTop: "16px"}}
                                        className="command-input"
                                        value={cmd.first}
                                        placeholder="Enter git command (e.g. git pull)"
                                        onChange={e => updateUniformShellCommand(cmd.id, e.target.value)}
                                    />
                                    <button
                                        className="remove-button"
                                        onClick={() => deleteUniformShellCommand(cmd.id)}
                                    >
                                        −
                                    </button>
                                </div>
                            ))}
                        </div>
                        <button
                            className="add-button"
                            type="button"
                            onClick={addUniformShellCommandRow}
                        >
                            +
                        </button>
                    </div>
                )}
                {customBatchRows.map(row => (
                    <CustomBatchCommandRow
                        key={row.id}
                        id={row.id}
                        repositoryId={row.repositoryId}
                        commands={row.commands}
                        onDelete={deleteCustomBatchRow}
                        onUpdate={updateCustomBatchRow}
                        commandMode={commandMode}
                    />
                ))}
                {notification && (
                    <NotificationModal
                        title={notification.title}
                        message={notification.message}
                        isError={notification.isError}
                        loading={notification.loading}
                        onClose={closeModal}
                    />
                )}
                <br />
                <button
                    className="submit-button"
                    type="submit"
                    onClick={executeBatchOperation}
                >
                    Run Batch Git Commands
                </button>

                <div className="batch-page">
                    <div
                        style={
                        {
                            display: "flex",
                            justifyContent: "space-between",
                            alignItems: "center",
                            marginTop: "20px",
                            marginBottom: "8px",
                        }
                    }
                    >
                        <h3 style={{ margin: 0 }}>Operation Output</h3>
                        {output && output.length > 0 && (
                            <button
                                className="clear-output-button"
                                onClick={() => setOutput([])}
                            >
                                Clear Output
                            </button>
                        )}
                    </div>
                    <div className="output-window">
                        {output && output.length === 0 && <p>No operations executed yet.</p>}

                        {output?.map((result, index) => (
                            <div
                                key={index}
                                className={`output-entry ${result.success ? "success" : "failure"}`}
                            >
                                <div>
                                    <strong>{result.repoName}: {result.executedCommand}</strong>
                                    <span className="status-icon"></span>
                                </div>
                                {result.message && (
                                    <div className="output-message">{result.message}</div>
                                )}
                            </div>
                        ))}
                    </div>
                </div>

            </div>
        </>
    );
};

export default BatchOperationsTab;
