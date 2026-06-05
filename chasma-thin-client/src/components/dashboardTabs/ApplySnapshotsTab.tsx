import { useEffect, useState } from "react";
import { SnapshotMode } from "../types/CustomTypes";
import Checkbox from "../Checkbox";
import { AddWorkContextSnapshotRequest, ApplyWorkContextSnapshotRequest, DeleteWorkspaceSnapshotRequest, RepositorySnapshotAdditionResult, RepositorySnapshotBlueprint, RepsoitoryWorkContextSnapshotEntry, WorkContextSnapshot } from "../../API/ChasmaWebApiClient";
import RepositorySnapshotRow from "./rows/RepositorySnapshotRow";
import { useCacheStore } from "../../managers/CacheManager";
import { useNavigate } from "react-router-dom";
import { statusClient } from "../../managers/ApiClientManager";
import { handleApiError } from "../../managers/TransactionHandlerManager";

/**
 * Initializes a new ApplySnapshotsTab class.
 * @constructor
 */
const ApplySnapshotsTab: React.FC = () => {
    /** Gets or sets the snapshot view mode. **/
    const [snapshotViewMode, setSnapshotViewMode] = useState<SnapshotMode>("add");

    /** Gets or sets a value indicating whether the user is selecting all repositories to save repository snapshots. **/
    const [isSelectingAllRepositories, setIsSelectingAllRepositories] = useState<boolean>(false);

    /** Gets or sets the workspace snapshot display. */
    const [displayName, setDisplayName] = useState<string>("");

    /** Gets or sets the snapshot note of intent. */
    const [snapshotNote, setSnapshotNote] = useState<string | undefined>(undefined);

    /** Gets or sets the repository snapshot blueprint entries. */
    const [snapshotEntries, setSnapshotEntries] = useState<
        { id: string; blueprint: RepositorySnapshotBlueprint }[]
    >([]);

    /** Gets or sets the flag indicating whether to disable the send button. */
    const [disabledSendButton, setDisableSendButton] = useState(false);

    /** The cached repositories belonging to the logged-in user. **/
    const repositories = useCacheStore((state) => state.repositories);

    /** The navigation function. **/
    const navigate = useNavigate();

    /** Sets the notification modal. */
    const setNotification = useCacheStore(state => state.setNotification);

    /** The logged-in user. **/
    const user = useCacheStore(state => state.user);

    /** The workspace snapshots that the user has. */
    const workspaceSnapshots = useCacheStore(state => state.workspaceSnapshots);

    /** Gets or sets the selected snapshot. */
    const [selectedSnapshot, setSelectedSnapshot] = useState<WorkContextSnapshot | undefined>(workspaceSnapshots.length > 0 ? workspaceSnapshots[0] : undefined);

    /** Gets or sets the repository addition results. **/
    const [repositorySnapshotAdditionResults, setRepositorySnapshotAdditionResults] = useState<
        RepositorySnapshotAdditionResult[]
    >([]);

    /** Adds the git clone blueprint entry. */
    const addSnapshotEntry = () => {
        const blueprint = new RepositorySnapshotBlueprint();
        setSnapshotEntries((prev) => [
            ...prev,
            { id: crypto.randomUUID(), blueprint },
        ]);
    };

    /**
     * Deletes a repository batch row.
     * @param id The row identifier.
     */
    const deleteSnapshotEntry = (id: string) => {
        setSnapshotEntries((prev) => prev.filter((row) => row.id !== id));
    };

    /**
     * Updates a specific row's blueprint data immutably.
     * @param id The row identifier.
     * @param updatedBlueprint The updated repository snapshot blueprint.
     */
    const updateSnapshotEntry = (id: string, updatedBlueprint: RepositorySnapshotBlueprint) => {
        setSnapshotEntries((prev) =>
            prev.map((row) => (row.id === id ? { ...row, blueprint: updatedBlueprint } : row))
        );
    };

    /**
     * Handles the event when the user wants to apply the specified action based on the view mode.
     */
    const handleSnapshotActionRequest = async () => {
        if (disabledSendButton) {
            return;
        }

        setDisableSendButton(true);
        if (snapshotViewMode === "add") {
            await handleSnapshotAdditionRequest();
        }
        else if (snapshotViewMode === "apply") {
            await handleLoadWorkspaceSnapshotRequest();
        }
        else {
            setNotification({
                title: "The specified action is not supported.",
                message: "Nothing to do...",
                isError: true,
            });
        }

        setDisableSendButton(false);
    }

    /**
     * Handles the event when the user wants to save the workspace context snapshot.
     */
    const handleSnapshotAdditionRequest = async () => {
        if (snapshotEntries.length === 0) {
            setNotification({
                title: "Need to enter repositories.",
                message: "Nothing to do...",
                isError: true,
            });
            return;
        }

        setNotification({
            title: "Saving snapshot of selected workspaces",
            message: "Please wait while your request is being processed.",
            isError: false,
            loading: true
        });
        const request = new AddWorkContextSnapshotRequest();
        request.userId = user?.userId;
        request.snapshotDisplayName = displayName;
        request.snapshotNote = snapshotNote;
        request.blueprints = snapshotEntries.filter(i => i.blueprint.repositoryId).map(i => i.blueprint);
        try {
            const response = await statusClient.addWorkspaceSnapshot(request);
            if (response.isErrorResponse) {
                setNotification({
                    title: "Failed to save workspace snapshot",
                    message: response.errorMessage,
                    isError: false,
                });
                return;
            }

            if (response.workContextSnapshot) {
                useCacheStore.getState().addWorkspaceSnapshot(response.workContextSnapshot)
            }

            if (response.additionResults) {
                setRepositorySnapshotAdditionResults(response.additionResults);
            }

            setNotification({
                title: "Repository Snapshot Complete!",
                message: "You may close now.",
                isError: false,
            });
        } catch (error) {
            const errorNotification = handleApiError(error, navigate, "Error adding workspace snapshot!", "Check server logs for more information.");
            setNotification(errorNotification);
        }
    };

    /**
     * Handles the event when the user wants to load the selected workspace.
     */
    const handleLoadWorkspaceSnapshotRequest = async () => {
        if (!selectedSnapshot) {
            setNotification({
                title: "No snapshot is selected",
                message: "Choose a snapshot to load.",
                isError: true,
            });
            return;
        }

        setNotification({
            title: `Loading snapshot: ${selectedSnapshot?.displayName}`,
            message: "Please wait while your request is being processed.",
            isError: false,
            loading: true
        });
        const request = new ApplyWorkContextSnapshotRequest();
        request.snapshotId = selectedSnapshot.snapshotId;
        try {
            const response = await statusClient.loadWorkspaceSnapshot(request);
            if (response.isErrorResponse) {
                setNotification({
                    title: `Failed to load snapshot: ${selectedSnapshot.displayName}`,
                    message: response.errorMessage,
                    isError: true,
                });
                return;
            }

            const loadingFailures: string[] = [];
            if (response.additionResults) {
                response.additionResults.forEach(result => {
                    if (!result.isSuccessful && result.snapshotName) {
                        loadingFailures.push(result.snapshotName);
                    }
                });
            }

            setNotification({
                title: "Repository Loading Operation Complete!",
                message: loadingFailures.length === 0 ? "You may close now." : `The following repository snapshots failed to successfully load: ${loadingFailures.join(", ")}"`,
                isError: false,
            });
        } catch (error) {
            const errorNotification = handleApiError(error, navigate, "Error loading workspace snapshot!", "Check server logs for more information.");
            setNotification(errorNotification);
        }
    };

    /**
     * Deletes the specified snapshot with the specified identifier.
     * @param snapshotId The snapshot identifier to delete.
     */
    const handleDeleteWorkspaceContextSnapshotRequest = async (snapshotId: number) => {
        const request = new DeleteWorkspaceSnapshotRequest();
        request.snapshotIds = [snapshotId];
        try {
            const response = await statusClient.deleteWorkspaceSnapshots(request);
            if (response.isErrorResponse) {
                setNotification({
                    title: "Failed to delete workspace snapshot",
                    message: response.errorMessage,
                    isError: false,
                });
                return;
            }

            response.snapshotIds?.forEach(id => useCacheStore.getState().deleteSnapshot(id));
            setSelectedSnapshot(undefined);
        } catch (error) {
            const errorNotification = handleApiError(error, navigate);
            setNotification(errorNotification);
        }
    };

    /**
     * Handles the event when the user selects a specific snapshot entry.
     * @param snapshotId The identifier of the selected snapshot.
     */
    const handleSnapshotEntrySelected = (snapshotId: number | undefined) => {
        const snapshot = workspaceSnapshots.find(i => i?.snapshotId === snapshotId);
        setSelectedSnapshot(snapshot);
    };

    /**
     * Gets the repository snapshot display name.
     * @param repoSnapshot The repository snapshot.
     * @returns The display name of the repository snapshot row.
     */
    const getRepositorySnapshotDisplayName = (repoSnapshot: RepsoitoryWorkContextSnapshotEntry) => {
        const repository = repositories.find(i => i.id === repoSnapshot.repositoryId);
        if (repository) {
            return repository.name;
        }

        return `${repoSnapshot.repositoryId} (Not Found in System)`;
    };

    useEffect(() => {
        if (isSelectingAllRepositories) {
            if (!repositories || repositories.length === 0) return;

            setSnapshotEntries(
                repositories.map(repo => {
                    const blueprint = new RepositorySnapshotBlueprint();
                    blueprint.repositoryId = repo.id;
                    return { id: crypto.randomUUID(), blueprint: blueprint }
                })
            );
        }
        else if (snapshotViewMode === "add") {
            // Reset to one empty row when unchecked
            const blueprint = new RepositorySnapshotBlueprint();
            setSnapshotEntries([{ id: crypto.randomUUID(), blueprint: blueprint }]);
        }
    }, [isSelectingAllRepositories, repositories, snapshotViewMode]);

    return (
        <>
            <div className="snapshot-page">
                <header className="snapshot-page-header">
                    <div className="snapshot-page-title-group">
                        <h1 className="snapshot-page-title">
                            Development Workspace Snapshot Operations
                        </h1>

                        <p className="snapshot-page-subtitle">
                            Sync, save, and switch across your entire workspace ⚡
                        </p>
                    </div>
                </header>

                <section className="snapshot-mode-section">
                    <div className="snapshot-mode-toggle">
                        <button
                            className={`snapshot-mode-button ${snapshotViewMode === "add" ? "active" : ""
                                }`}
                            onClick={() => setSnapshotViewMode("add")}
                        >
                            Add
                        </button>

                        <button
                            className={`snapshot-mode-button ${snapshotViewMode === "apply" ? "active" : ""
                                }`}
                            onClick={() => setSnapshotViewMode("apply")}
                        >
                            Apply
                        </button>
                    </div>

                    {snapshotViewMode === "add" && (
                        <>
                            <div className="snapshot-metadata-card">
                                <div className="snapshot-field">
                                    <label className="snapshot-field-label">
                                        Display Name
                                    </label>

                                    <input
                                        type="text"
                                        className="snapshot-input"
                                        placeholder="Name of snapshot"
                                        value={displayName}
                                        onChange={(e) => setDisplayName(e.target.value)}
                                        required
                                    />
                                </div>

                                <div className="snapshot-field">
                                    <label className="snapshot-field-label">
                                        Workspace Note
                                    </label>

                                    <textarea
                                        className="snapshot-textarea"
                                        placeholder="(Optional)"
                                        value={snapshotNote}
                                        onChange={(e) => setSnapshotNote(e.target.value)}
                                    />
                                </div>
                            </div>

                            <div className="snapshot-workspace">
                                {/* Repository Selection Panel */}
                                <div className="repo-sidebar">
                                    <div className="repo-sidebar-header">
                                        <h2 className="page-description">
                                            Choose repository workspaces to save.
                                        </h2>
                                    </div>

                                    <div className="repo-sidebar-actions">
                                        <Checkbox
                                            label="Select all"
                                            onBoxChecked={setIsSelectingAllRepositories}
                                        />

                                        {!isSelectingAllRepositories && (
                                            <button
                                                className="snapshot-secondary-btn"
                                                onClick={addSnapshotEntry}
                                            >
                                                Add +
                                            </button>
                                        )}
                                    </div>

                                    <div className="repo-sidebar-content">
                                        {snapshotEntries.map((entry) => (
                                            <RepositorySnapshotRow
                                                key={entry.id}
                                                rowId={entry.id}
                                                snapshotMode={snapshotViewMode}
                                                blueprint={entry.blueprint}
                                                repositories={repositories}
                                                onRepositoryDelete={deleteSnapshotEntry}
                                                onUpdate={(updatedBlueprint) =>
                                                    updateSnapshotEntry(entry.id, updatedBlueprint)
                                                }
                                            />
                                        ))}
                                    </div>

                                    <div className="repo-sidebar-footer">
                                        <button
                                            className="snapshot-save-btn"
                                            disabled={disabledSendButton}
                                            onClick={handleSnapshotActionRequest}
                                        >
                                            Save Snapshot{snapshotEntries.length > 1 ? "s" : ""}
                                        </button>
                                    </div>
                                </div>

                                {/* Snapshot Results Panel */}
                                <div className="snapshot-results-panel">
                                    <div className="snapshot-results-container">
                                        <div className="snapshot-results-header">
                                            <h3>Snapshot Results</h3>

                                            <button
                                                className="snapshot-secondary-btn"
                                                onClick={() => setRepositorySnapshotAdditionResults([])}
                                            >
                                                Clear Output
                                            </button>
                                        </div>

                                        <div className="snapshot-results-body">
                                            {repositorySnapshotAdditionResults.length === 0 && (
                                                <p className="snapshot-empty-state">
                                                    No results to report.
                                                </p>
                                            )}

                                            {repositorySnapshotAdditionResults.map((result, index) => (
                                                <div
                                                    key={index}
                                                    className={`snapshot-result-card ${result.isSuccessful ? "success" : "failure"
                                                        }`}
                                                >
                                                    <div className="snapshot-result-top">
                                                        <strong>{result.repositoryName}</strong>
                                                        <span className="status-icon" />
                                                    </div>

                                                    <span className="snapshot-result-command">
                                                        &gt;{" "}
                                                        {result.isSuccessful
                                                            ? "Successfully saved workspace snapshot!"
                                                            : "Failed to save workspace snapshot!"}
                                                    </span>

                                                    <span className="snapshot-result-message">
                                                        {result.isSuccessful
                                                            ? `Successfully saved ${result.snapshotName}`
                                                            : result.reason}
                                                    </span>
                                                </div>
                                            ))}
                                        </div>
                                    </div>
                                </div>
                            </div>
                        </>
                    )}
                    {snapshotViewMode === "apply" && (
                        <>
                            <div className="snapshot-workspace">
                                {/* Workspace Selection Panel */}
                                <div className="repo-sidebar">
                                    <div className="repo-sidebar-header">
                                        <h2 className="page-description">
                                            Choose workspace development workspaces to load.
                                        </h2>
                                    </div>

                                    <div className="repo-sidebar-content">
                                        {workspaceSnapshots.map((entry) => (
                                            <RepositorySnapshotRow
                                                key={entry.snapshotId}
                                                snapshot={entry}
                                                isSelected={selectedSnapshot?.snapshotId === entry.snapshotId}
                                                snapshotMode={snapshotViewMode}
                                                onSnapshotDelete={handleDeleteWorkspaceContextSnapshotRequest}
                                                onSelected={handleSnapshotEntrySelected}
                                            />
                                        ))}
                                    </div>

                                    <div className="repo-sidebar-footer">
                                        <button
                                            className="snapshot-save-btn"
                                            disabled={disabledSendButton}
                                            onClick={handleSnapshotActionRequest}
                                        >
                                            {snapshotViewMode !== "apply" && `Save Snapshot${snapshotEntries.length > 1 ? "s" : ""}`}
                                            {snapshotViewMode === "apply" && `Load Snapshot ${selectedSnapshot ? `${selectedSnapshot.snapshotId}` : ""}`}
                                        </button>
                                    </div>
                                </div>

                                {/* Repository Snapshots Panel */}
                                <div className="snapshot-results-panel">
                                    <div className="snapshot-results-container">
                                        <div className="snapshot-results-header">
                                            <h3>{selectedSnapshot ? `Snapshot ${selectedSnapshot.snapshotId}` : ""}</h3>
                                        </div>
                                        <div className="snapshot-results-body">
                                            {snapshotViewMode === "apply" && (
                                                <>
                                                    {selectedSnapshot && selectedSnapshot.repositorySnapshots?.length && selectedSnapshot.repositorySnapshots.length ? (
                                                        <table className="status-table">
                                                            <tbody>
                                                                {selectedSnapshot.repositorySnapshots.map((entry, index) => (
                                                                    <tr
                                                                        key={index}>
                                                                        <td>
                                                                            <div className="repo-meta-line">
                                                                                <strong>Repo Title:</strong> {getRepositorySnapshotDisplayName(entry)}
                                                                            </div>
                                                                            <div className="repo-meta-line">
                                                                                <strong>Branch:</strong> {entry.branchName}
                                                                            </div>
                                                                            <div className="repo-meta-line">
                                                                                <strong>Commit Hash:</strong> {entry.commitHash}
                                                                            </div>
                                                                            <div className="repo-meta-line">
                                                                                <strong>Created At:</strong> {entry.createdAt}
                                                                            </div>
                                                                            <div className="repo-meta-line">
                                                                                <strong>Stash Message:</strong> {entry.stashMessage}
                                                                            </div>
                                                                            <div className="repo-meta-line">
                                                                                <strong>Note:</strong> {entry.intentNote}
                                                                            </div>
                                                                        </td>
                                                                    </tr>
                                                                ))}
                                                            </tbody>
                                                        </table>
                                                    ) : <div className="empty-table">No repositories to display.</div>}
                                                </>
                                            )}
                                        </div>
                                    </div>
                                </div>
                            </div>
                        </>
                    )}
                </section>
            </div>
        </>
    );
}

export default ApplySnapshotsTab;


