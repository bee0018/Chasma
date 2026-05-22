import { useEffect, useState } from "react";
import RepositoryCloneEntryRow from "./rows/RepositoryCloneEntryRow";
import {
    GitCloneBlueprint,
    GitCloneRequest,
    RepositoryAdditionResult,
} from "../../API/ChasmaWebApiClient";
import { useCacheStore } from "../../managers/CacheManager";
import { handleApiError } from "../../managers/TransactionHandlerManager";
import { useNavigate } from "react-router-dom";
import { appConfigClient, configClient } from "../../managers/ApiClientManager";

/**
 * Initializes a new instance of the CloneRepositoriesTab component
 * @constructor
 */
const CloneRepositoriesTab: React.FC = () => {
    /** Gets or sets the git clone blueprint entries. */
    const [cloneEntries, setCloneEntries] = useState<
        { id: string; blueprint: GitCloneBlueprint }[]
    >([]);

    /** Gets or sets a value indicating whether the request is ready to be sent. */
    const [disableSendButton, setDisableSendButton] = useState(false);

    /** Gets or sets the repository addition results. **/
    const [repositoryAdditionResults, setRepositoryAdditionResults] = useState<
        RepositoryAdditionResult[]
    >([]);

    /** Gets or sets the global workspace path. */
    const [globalWorkspacePath, setGlobalWorkspacePath] = useState<string | undefined>(undefined);

    /** Gets or sets the logged in user. */
    const user = useCacheStore((state) => state.user);

    /** Sets the notification modal. */
    const setNotification = useCacheStore((state) => state.setNotification);

    /** The use navigation utility. **/
    const navigate = useNavigate();

    /** Adds the git clone blueprint entry. */
    const addGitCloneEntry = () => {
        const blueprint = new GitCloneBlueprint();
        blueprint.workingDirectory = globalWorkspacePath;
        setCloneEntries((prev) => [
            ...prev,
            { id: crypto.randomUUID(), blueprint },
        ]);
    };

    /**
     * Deletes a repository batch row.
     * @param id The row identifier.
     */
    const deleteGitCloneEntry = (id: string) => {
        setCloneEntries((prev) => prev.filter((row) => row.id !== id));
    };

    /**
     * Updates a specific row's blueprint data immutably.
     * @param id The row identifier.
     * @param updatedBlueprint The updated git clone blueprint.
     */
    const updateGitCloneEntry = (id: string, updatedBlueprint: GitCloneBlueprint) => {
        setCloneEntries((prev) =>
            prev.map((row) => (row.id === id ? { ...row, blueprint: updatedBlueprint } : row))
        );
    };

    /**
     * Handles the event when the user wants to clone the repositories.
     */
    const handleCloneRepositoriesRequest = async () => {
        if (disableSendButton) {
            setNotification({
                title: "Currently performing git clone operation...",
                message: "Please wait until all repositories are cloned.",
                isError: false,
                loading: true,
            });
            return;
        }

        setDisableSendButton(true);
        setNotification({
            title: "Started the cloning operation...",
            message: "May take a few moments depending on repository size.",
            isError: false,
            loading: true,
        });
        const request = new GitCloneRequest();
        request.userId = user?.userId;
        request.blueprints = cloneEntries.map((i) => i.blueprint);
        try {
            const response = await configClient.cloneRepositories(request);
            if (response.isErrorResponse) {
                setNotification({
                    title: "Cloning operation failed!",
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
            const errorNotification = handleApiError(error, navigate);
            setNotification(errorNotification);
        } finally {
            setDisableSendButton(false);
        }
    };

    useEffect(() => {
            /**
             * Gets the API configuration values.
             */
            const getApiConfig = async () => {
                try {
                    const response = await appConfigClient.getConfig();
                    setGlobalWorkspacePath(response.globalWorkspacePath);
                } catch (error) {
                    console.log(error);
                    setNotification({
                        title: "Could get API configuration!",
                        message: "Review console logs.",
                        isError: true,
                    });
                }
            };
            
            getApiConfig().catch(e => console.error(e));
        }, [setNotification]);

    return (
        <>
            <div className="content">
                <div className="main-layout">
                    <div className="left-panel">
                        <div className="panel-card">
                            <div className="batch-page-container">
                                <header className="batch-page-header">
                                    <h1 className="page-title">Clone Multiple Repositories</h1>
                                    <p className="page-description">
                                        Manage and clone multiple source repositories to set up your
                                        workspace.
                                    </p>
                                    <div className="page-warning-banner">
                                        <span className="warning-icon">⚠️</span>
                                        <div className="warning-content">
                                            <p className="warning-text">
                                                <strong>Warning:</strong> To successfully clone private repositories or submodules, you must configure your remote hosting credentials in the system settings. Missing or improperly scoped credentials will cause connection failures, including "403 Forbidden" errors.
                                            </p>
                                            <details className="warning-details">
                                                <summary>View Required Settings & Token Scopes</summary>
                                                <div className="requirements-grid">
                                                    <div className="platform-requirement">
                                                        <h4>GitHub Requirements</h4>
                                                        <ul>
                                                            <li><strong>System Config:</strong> Username & Personal Access Token (PAT)</li>
                                                            <li><strong>Fine-Grained Tokens:</strong> Repository Permissions → Contents → <code>Read-only</code></li>
                                                            <li><strong>Classic Tokens:</strong> <code>repo</code> scope (Full control of private repositories)</li>
                                                            <li><strong>SAML/SSO:</strong> Ensure you click "Configure SSO" next to your token and authorize your organization.</li>
                                                        </ul>
                                                    </div>
                                                    <div className="platform-requirement">
                                                        <h4>GitLab Requirements</h4>
                                                        <ul>
                                                            <li><strong>System Config:</strong> Username & Personal Access Token (PAT)</li>
                                                            <li><strong>Token Scopes:</strong> <code>read_repository</code> (or <code>api</code> for full access)</li>
                                                        </ul>
                                                    </div>
                                                </div>
                                            </details>
                                        </div>
                                    </div>
                                    <div className="repository-actions">
                                        <button
                                            type="submit"
                                            className="submit-button"
                                            onClick={() => setCloneEntries([])}
                                        >
                                            Clear
                                        </button>
                                        <button
                                            type="submit"
                                            className="submit-button"
                                            onClick={addGitCloneEntry}
                                        >
                                            Add Entry
                                        </button>
                                        <button
                                            type="submit"
                                            className="submit-button"
                                            onClick={handleCloneRepositoriesRequest}
                                        >
                                            Clone Repos
                                        </button>
                                    </div>
                                </header>
                            </div>
                        </div>
                        {cloneEntries.map((i) => {
                            return <RepositoryCloneEntryRow
                                key={i.id}
                                rowId={i.id}
                                blueprint={i.blueprint}
                                onDelete={deleteGitCloneEntry}
                                onUpdate={updatedBlueprint => updateGitCloneEntry(i.id, updatedBlueprint)}
                                globalWorkspacePath={globalWorkspacePath}
                            />
                        })}
                    </div>

                    <div className="right-panel">
                        <section className="output-section">
                            <div className="output-header">
                                <h3>Repository Cloning Results</h3>
                                <button
                                    className="clear-output-button"
                                    onClick={() => setRepositoryAdditionResults([])}
                                >
                                    Clear Output
                                </button>
                            </div>
                            <div className="output-window">
                                {repositoryAdditionResults.length === 0 && (
                                    <p className="no-output-text">No results to report.</p>
                                )}
                                {repositoryAdditionResults.map((result, index) => (
                                    <div
                                        key={index}
                                        className={`output-entry ${result.isSuccessful ? "success" : "failure"}`}
                                    >
                                        <div className="output-header-row">
                                            <strong>{result.repositoryName}</strong>
                                            <span className="status-icon" />
                                        </div>
                                        <span className="output-command">
                                            &gt;{" "}
                                            {result.isSuccessful
                                                ? "Successfully added repository!"
                                                : "Failed to add repository!"}
                                        </span>
                                        <span className="output-stdout">
                                            {result.isSuccessful
                                                ? `Navigate back to home page to manage ${result.repositoryName}`
                                                : result.reason}
                                        </span>
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

export default CloneRepositoriesTab;
