import React, {useState} from "react";
import {
    AddGitRepositoriesRequest,
    RepositoryAdditionResult,
} from "../../API/ChasmaWebApiClient";
import {useCacheStore} from "../../managers/CacheManager";
import {Row} from "../types/CustomTypes";
import {configClient} from "../../managers/ApiClientManager";
import { useNavigate } from "react-router-dom";
import { handleApiError } from "../../managers/TransactionHandlerManager";
import { isBlankOrUndefined } from "../../stringHelperUtil";
import { useDocumentTitle } from "../../util/useDocumentTitle";

/**
 * Initializes a new RepositoryAdditionsTab class.
 * @constructor
 */
const RepositoryAdditionsTab: React.FC = () => {
    useDocumentTitle("Add Repositories");

    /** The logged-in user. **/
    const user = useCacheStore((state) => state.user);

    /** The navigation function. **/
    const navigate = useNavigate();

   /** Sets the notification modal. */
   const setNotification = useCacheStore(state => state.setNotification);

    /** Gets or sets the repository addition results. **/
    const [repositoryAdditionResults, setRepositoryAdditionResults] = useState<RepositoryAdditionResult[]>([]);

    /** Gets or sets a value indicating whether the request is ready to be sent. */
    const [disableSendButton, setDisableSendButton] = useState(false);

    /** Gets or sets the shell command rows. **/
    const [rows, setRows] = useState<Row[]>([]);

    /** Handles custom repo path row changes in the form. **/
    const handleRepoPathChange = (
            id: string,
            field: "first" | "second",
            value: string
    ) => {
            setRows(prev =>
                prev.map(row =>
                    row.id === id ? { ...row, [field]: value } : row
                )
            );
        };
    
    /**
     * Deletes the row with the specified row identifier.
     * @param rowId The row identifier.
     */
    function deleteRepoPathRow(rowId: string) {
        const filteredRows = rows.filter(row => row.id !== rowId);
        setRows(filteredRows);
    }

    /** Adds a repository path row to the form. **/
    const addRepoPathRow = () => {
        setRows(prev => [
            ...prev,
            {id: crypto.randomUUID(), first: "", second: ""}
        ])};

    /** Handles the event when the user wants to add multiple repositories to the system. */
    const handleAddRepositories = async () => {
        if (disableSendButton) {
            return;
        }

        setDisableSendButton(true);
        setNotification({
            title: "Adding repository to system...",
            message: "Please await repository additions while your request is being processed. May take a while depending on how large your filesystem is.",
            isError: false,
            loading: true
        });
        const request = new AddGitRepositoriesRequest();
        request.userId = user?.userId;
        request.repositoryPaths = [];
        rows.forEach(row => {
            if (!isBlankOrUndefined(row.first) && request.repositoryPaths) {
                request.repositoryPaths.push(row.first);
            }
        });

        try {
            const response = await configClient.addGitRepository(request);
            if (response.isErrorResponse) {
                setNotification({
                    title: "Error adding repositories!",
                    message: response.errorMessage,
                    isError: true,
                });
                return;
            }

            response.repositories?.forEach(repo => useCacheStore.getState().addLocalGitRepository(repo));
            if (response.additionResults) {
                setRepositoryAdditionResults(response.additionResults);
            }

            setNotification({
            title: "Repository Addition Operation Complete!",
            message: "You may close now.",
            isError: false,
        });
        } catch (error) {
            const errorNotification = await handleApiError(error, navigate, "Failed to add repositories!", "Error adding repositories. Check console logs for more information.");
            setNotification(errorNotification);
        }
        finally {
            setDisableSendButton(false);
        }
    };

    return (
        <>
            <div className="content">
                <div className="main-layout">
                    <div className="left-panel">
                        <div className="panel-card">
                            <div className="panel-header">
                                <h2 className="page-description">Streamline your workflow by instantly syncing new repositories; simply provide the full system path to your project folder to get started ⛷️</h2>
                            </div>
                        </div>
                        <section className="command-mode-section">
                            <div className="repository-actions">
                                <button
                                    className="add-repo-button"
                                    onClick={addRepoPathRow}>
                                    Add Path +
                                </button>
                            </div>
                        </section>

                        <section className="custom-commands-section">
                            <div>
                                {rows.map(row => (
                                    <div
                                        key={row.id}
                                        style={{
                                            display: "flex",
                                            gap: "10px",
                                            marginBottom: "10px"
                                        }}
                                    >
                                        <input
                                            type="text"
                                            placeholder="Absolute Repository File Path"
                                            className="command-input modern-input"
                                            value={row.first}
                                            onChange={e => handleRepoPathChange(row.id, "first", e.target.value)}/>
                                        <button
                                            className="remove-button modern-remove"
                                            type="button"
                                            onClick={() => deleteRepoPathRow(row.id)}
                                        >
                                            -
                                        </button>
                                    </div>
                                ))}
                                <br/>
                            </div>
                        </section>
                        <div className="run-batch-section">
                            <button
                                className="run-batch-button"
                                disabled={disableSendButton}
                                onClick={handleAddRepositories}
                            >
                                Add Repositories
                            </button>
                        </div>
                    </div>

                    <div className="right-panel">
                        <section className="output-section">
                            <div className="output-header">
                                <h3>Repository Addition Results</h3>
                                <button
                                    className="clear-output-button"
                                    onClick={() => setRepositoryAdditionResults([])}
                                >
                                    Clear Output
                                </button>
                            </div>
                            <div className="output-window">
                                {
                                    repositoryAdditionResults.length === 0 &&
                                    <p className="no-output-text">No results to report.</p>
                                }
                                {repositoryAdditionResults.map((result, index) => (
                                    <div
                                        key={index}
                                        className={`output-entry ${result.isSuccessful ? "success" : "failure"}`}
                                    >
                                        <div className="output-header-row">
                                            <strong>{result.repositoryName}</strong>
                                            <span className="status-icon" />
                                        </div>
                                        <span className="output-command">&gt; {result.isSuccessful ? "Successfully added repository!": "Failed to add repository!"}</span>
                                        <span className="output-stdout">{result.isSuccessful ? `Navigate back to home page to manage ${result.repositoryName}` : result.reason}</span>
                                    </div>
                                ))}
                            </div>
                        </section>
                    </div>
                </div>
            </div>
        </>
    );
};

export default RepositoryAdditionsTab;
