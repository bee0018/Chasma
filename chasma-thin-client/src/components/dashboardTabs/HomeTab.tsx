import React, { useEffect, useState } from "react";
import GitRepoOverviewCard from "../GitRepoOverviewCard";
import {
    ApplyUpdateRequest,
    DeleteRepositoryRequest,
    LocalGitRepository,
    SystemManifest,
} from "../../API/ChasmaWebApiClient";
import { useCacheStore } from "../../managers/CacheManager";
import { appConfigClient, configClient } from "../../managers/ApiClientManager";
import { useNavigate, useOutletContext } from "react-router-dom";
import { handleApiError } from "../../managers/TransactionHandlerManager";
import ChangeRepositoryDisplayNameModal from "../modals/ChangeRepositoryDisplayNameModal";
import { useDocumentTitle } from "../../util/useDocumentTitle";

/**
 * The properties of the Home Tab.
 */
interface IHomeTabProps {
    /** The repository version trigger. **/
    reposVersion?: number;
}

/**
 * The Home tab contents and display components.
 * @constructor Initializes a new instance of the HomeTab.
 */
const HomeTab: React.FC<IHomeTabProps> = (props: IHomeTabProps) => {
    useDocumentTitle("Home");

    /** The logged-in user. **/
    const user = useCacheStore((state) => state.user);

    /** Gets the local git repositories. **/
    const localGitRepositories = useCacheStore((state) => state.repositories);

    /** The use navigation utility. **/
    const navigate = useNavigate();

    /** Sets the notification modal. */
    const setNotification = useCacheStore(state => state.setNotification);

    /** Gets or sets the repository being edited. */
    const [activeRenameRepo, setActiveRenameRepo] = useState<LocalGitRepository | null>(null);

    /** Gets the outlet context of the browser. */
    const outletContext = useOutletContext<{ reposVersion?: number } | null>();

    /** Gets the new system update details if available. */
    const newSystemUpdate = useCacheStore((state) => state.newSystemUpdate);

    /** Gets the current repository version. */
    const currentReposVersion = outletContext?.reposVersion ?? props.reposVersion ?? 0;

    /** Gets or sets a value indicating whether the application is stopping the application. */
    const [isStopping, setIsStopping] = useState<boolean>(false);

    useEffect(() => {
        /** Retrieves the repository data from the web API. **/
        const retrieveUserRepositoryConfiguration = async () => {
            try {
                const userId = user?.userId;
                const message = await configClient.getLocalGitRepositories(userId);
                useCacheStore.getState().setRepositories(message.repositories);
            }
            catch (e) {
                const errorNotification = await handleApiError(e, navigate);
                setNotification(errorNotification);
            }
        };

        retrieveUserRepositoryConfiguration()
            .catch(e => {
                console.error(e.message);
            });
    }, [currentReposVersion, user?.userId]);

    useEffect(() => {
        const closeMenu = () => setContextMenu(null);
        window.addEventListener("click", closeMenu);
        return () => window.removeEventListener("click", closeMenu);
    }, []);

    /** Gets or sets the context menu. **/
    const [contextMenu, setContextMenu] = useState<{
        mouseX: number;
        mouseY: number;
        repo: LocalGitRepository;
    } | null>(null);

    /**
     * Handles the event when the user wants to delete a repository.
     * @param repoId The repository identifier.
     */
    const handleRepoDelete = async (repoId: string | undefined) => {
        if (!repoId) return;

        try {
            const request = new DeleteRepositoryRequest();
            request.repositoryId = repoId;
            request.userId = user?.userId;
            const response = await configClient.deleteRepository(request);
            if (response.isErrorResponse) {
                handleRepoDeletionError(response.errorMessage);
                return;
            }

            useCacheStore.getState().deleteRepository(repoId);
        } catch (e) {
            await handleRepoDeletionError("Review server logs for more information.");
        }
    };

    /** Handles the event when there is an error deleting a repository. **/
    const handleRepoDeletionError = async (errorMessage: string | undefined) => {
        const errorNotification = await handleApiError(errorMessage, navigate, "Could not delete repository!", "Review server logs for more information.");
        setNotification(errorNotification);
    }


    /** Handles the event when the user right-clicks a card to open the context menu. **/
    const handleContextMenu = (event: React.MouseEvent, repo: LocalGitRepository) => {
        event.preventDefault();
        setContextMenu({
            mouseX: event.clientX,
            mouseY: event.clientY,
            repo,
        });
    };

    /** Handles the event when the user wants to apply the system update. */
    const handleApplySystemUpdateRequest = async () => {
        if (isStopping) return;

        if (window.confirm("Are you sure you want to apply the new update?")) {
            setNotification({
                title: "Applying updates and restarting the system...",
                message: "System will restart shortly",
                isError: false,
                loading: true,
            });
            setIsStopping(true);
            const request = new ApplyUpdateRequest();
            request.systemManifest = SystemManifest.fromJS(newSystemUpdate);
            try {
                const response = await appConfigClient.applySystemUpdate(request);
                if (response.isErrorResponse) {
                    setNotification({
                        title: "Failed to update system",
                        message: response.errorMessage,
                        isError: true,
                    });
                    return;
                }

                setTimeout(() => {
                    useCacheStore.getState().clearCache();
                    window.location.href = "about:blank";
                }, 100);
            } catch (error) {
                if (error instanceof TypeError && error.message === "Failed to fetch") {
                    setNotification({
                        title: "Applying updates and restarting the system...",
                        message: "System will not restart shortly",
                        isError: false,
                        loading: true,
                    });
                    setTimeout(() => {
                        useCacheStore.getState().clearCache();
                        window.location.href = "about:blank";
                    }, 100);
                    return;
                }

                handleApiError(error, navigate, "Error applying update!", "Review server logs for more information.");
            }
            finally {
                setIsStopping(false);
            }
        }
    };

    return (
        <>
            <div>
                <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', marginBottom: '24px', width: '100%' }}>
                    <div>
                        <h1 style={{ margin: 0 }}>Your Repositories, Monitored & Mastered 🕹️</h1>
                        <p style={{ margin: '8px 0 0 0' }}>{`${user?.userName}, manage any of the registered repositories found on your filesystem.`}</p>
                        {localGitRepositories && localGitRepositories.length === 0 &&
                            <>
                                <h2>Get Started With Adding Local Git Repositories</h2>
                                <div className="help-subsection">
                                    <ul className="help-steps">
                                        <li>
                                            <div>
                                                <strong>Adding Local Git Repositories</strong>
                                                <p>- On the left sidebar, open the <strong>REPOSITORY MANAGEMENT</strong> tab.</p>
                                                <p>- Select the <strong>Register ➕</strong> button.</p>
                                                <p>- Select <strong>Add Path +</strong> and in each of the input fields paste the absolute path to the repository root.</p>
                                                <p>- Select <strong>Add Repositories</strong> to add the repositories to the system.</p>
                                            </div>
                                        </li>
                                    </ul>
                                </div>
                                <br />
                                <h2>Get Started With Cloning Git Repositories</h2>
                                <div className="help-subsection">
                                    <ul className="help-steps">
                                        <li>
                                            <div>
                                                <strong>Cloning Git Repositories</strong>
                                                <p><i>Note: Preconfigure either your remote host credentials in the </i> <strong>SYSTEM & DIAGNOSTICS</strong>&gt; <strong>System Settings ⚙️</strong> page.</p>
                                                <p>- On the left sidebar, open the <strong>REPOSITORY MANAGEMENT</strong> tab.</p>
                                                <p>- Select the <strong>Clone 🚚</strong> button.</p>
                                                <p>- Select <strong>Add Entry</strong> and configure each entry for new repositories you want to clone to your system and manage within the system.</p>
                                                <p>- When finished configuring, select <strong>Clone Repos</strong>.</p>
                                            </div>
                                        </li>
                                    </ul>
                                </div>
                            </>
                        }
                    </div>
                    {newSystemUpdate !== undefined &&
                        <button
                            className="update-button"
                            style={{ whiteSpace: 'nowrap' }}
                            onClick={handleApplySystemUpdateRequest}
                        >
                            Update
                        </button>
                    }
                </div>
                <div>
                    {localGitRepositories && localGitRepositories.length > 0 && (
                        localGitRepositories.map((repo) => (
                            <GitRepoOverviewCard
                                key={repo.id}
                                repository={repo}
                                url={`/status/${repo.displayName ? repo.displayName : repo.name}/${repo.id}`}
                                onDelete={handleRepoDelete}
                                onContextMenu={(e) => handleContextMenu(e, repo)}
                                user={user} />
                        ))
                    )}

                    {contextMenu && (
                        <div
                            className="context-menu"
                            style={{
                                top: contextMenu.mouseY,
                                left: contextMenu.mouseX,
                            }}
                            onClick={() => setContextMenu(null)}
                        >
                            <ul>
                                <li onClick={() => navigate(`/status/${contextMenu.repo.displayName ? contextMenu.repo.displayName : contextMenu.repo.name}/${contextMenu.repo.id}`)}>
                                    Open Status Page
                                </li>
                                <li onClick={() => window.open(`/status/${contextMenu.repo.displayName ? contextMenu.repo.displayName : contextMenu.repo.name}/${contextMenu.repo.id}`, "_blank", "noopener,noreferrer")}>
                                    Open Status in New Tab
                                </li>
                                <li onClick={() => handleRepoDelete(contextMenu.repo.id)}>
                                    Delete
                                </li>
                                <li onClick={() => setActiveRenameRepo(contextMenu.repo)}>
                                    Change Display Name
                                </li>
                            </ul>
                        </div>
                    )}
                </div>
                {activeRenameRepo &&
                    <ChangeRepositoryDisplayNameModal
                        onClose={() => setActiveRenameRepo(null)}
                        repository={activeRenameRepo} />
                }
            </div>
        </>
    );
}

export default HomeTab;
