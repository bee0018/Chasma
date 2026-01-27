import React, {useEffect, useState} from "react";
import '../../css/DasboardTab.css';
import GitRepoOverviewCard from "../GitRepoOverviewCard";
import {
    DeleteRepositoryRequest,
    IgnoreRepositoryRequest,
    LocalGitRepository,
    RepositoryConfigurationClient
} from "../../API/ChasmaWebApiClient";
import NotificationModal from "../modals/NotificationModal";
import {apiBaseUrl} from "../../environmentConstants";
import {useCacheStore} from "../../managers/CacheManager";

/** The Git API client. **/
const configClient = new RepositoryConfigurationClient(apiBaseUrl)

/**
 * The properties of the Home Tab.
 */
interface IHomeTabProps {
    /** The repository version trigger. **/
    reposVersion: number;
}

/**
 * The Home tab contents and display components.
 * @constructor Initializes a new instance of the HomeTab.
 */
const HomeTab: React.FC<IHomeTabProps> = (props: IHomeTabProps) => {
    /** The logged-in user. **/
    const user = useCacheStore((state) => state.user);

    /** Gets the local git repositories. **/
    const localGitRepositories = useCacheStore((state) => state.repositories);

    useEffect(() => {
        updateUserRepositoryConfiguration().catch(console.error);
    }, [props.reposVersion]);

    /**
     * Updates the repository configuration when the repositories are updated in the background.
     */
    const updateUserRepositoryConfiguration = async () => {
        try {
            const userId = user?.userId;
            const message = await configClient.getLocalGitRepositories(userId);
            useCacheStore.getState().setRepositories(message.repositories);
        } catch (e) {
            console.error(e);
        }
    };

    useEffect(() => {
        /** Retrieves the repository data from the web API. **/
        const retrieveUserRepositoryConfiguration = async () => {
            try {
                const userId = user?.userId;
                const message = await configClient.getLocalGitRepositories(userId);
                useCacheStore.getState().setRepositories(message.repositories);
            }
            catch (e) {
                setNotification({
                    title: "Git repository retrieval failed!",
                    message: "Review server logs for more information.",
                    isError: true,
                });
                useCacheStore.getState().setRepositories(undefined);
                console.error(`Could not get git repositories from filesystem: ${e}`);
            }
        };

        retrieveUserRepositoryConfiguration()
            .catch(e => {
            console.error(e.message);
        });
    }, []);

    useEffect(() => {
        const closeMenu = () => setContextMenu(null);
        window.addEventListener("click", closeMenu);
        return () => window.removeEventListener("click", closeMenu);
    }, []);


    /** Gets or sets the notification **/
    const [notification, setNotification] = useState<{
        title: string,
        message: string | undefined,
        isError: boolean | undefined,
        loading?: boolean
    } | null>(null);

    /** Gets or sets the context menu. **/
    const [contextMenu, setContextMenu] = useState<{
        mouseX: number;
        mouseY: number;
        repo: LocalGitRepository;
    } | null>(null);

    /** Sends a request to the add the local git repositories on the filesystem. **/
    async function handleAddLocalGitRepositories() {
        setNotification({
            title: "Adding local git repositories from logical drives...",
            message: "Please wait while your request is being processed. May take a while depending on how large your filesystem is.",
            isError: false,
            loading: true
        });

        try {
            const userId = user?.userId;
            const response = await configClient.addLocalGitRepositories(userId);
            if (response.isErrorResponse) {
                setNotification({
                    title: "Git repository retrieval failed!",
                    message: response.errorMessage,
                    isError: true,
                });
                return;
            }
            setNotification({
                title: "Git repository retrieval finished!",
                message: "Close the modal to find the repositories found on your system.",
                isError: false,
            });
            useCacheStore.getState().setRepositories(response.currentRepositories);
        }
        catch (e) {
            setNotification({
                title: "Git repository retrieval failed!",
                message: "Review server logs for more information.",
                isError: true,
            });
            console.error(`Could not get git repositories from filesystem: ${e}`);
        }
    }

    /**
     * Closes the modal once the user confirms the message
     */
    const closeModal = () => {
        setNotification(null);
    }

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
            handleRepoDeletionError("Review server logs for more information.");
            console.error(e);
        }
    };

    /** Handles the event when there is an error deleting a repository. **/
    const handleRepoDeletionError = (errorMessage: string | undefined) => {
        setNotification({
            title: "Could not delete repository!",
            message: errorMessage,
            isError: true,
        });
    }

    /**
     * Handles the action when the user wants to include/ignore the repository.
     * @param repoId The repository identifier.
     */
    const handleIgnoreAction = async (repoId: string | undefined) => {
        try {
            const request = new IgnoreRepositoryRequest();
            request.userId = user?.userId;
            request.repositoryId = repoId;
            request.isIgnored = true;
            const response = await configClient.ignoreRepository(request);
            if (response.isErrorResponse) {
                setNotification({
                    title: `Repository ignore action failed!`,
                    message: response.errorMessage,
                    isError: true,
                });
                return;
            }

            useCacheStore.getState().setRepositories(response.includedRepositories);
        }
        catch (e) {
            setNotification({
                title: `Repository ignore action failed!`,
                message: "Review server logs for more information.",
                isError: true,
            });
            console.error(`Could not ignore repositories on the filesystem: ${e}`);
        }
    };

    /** Handles the event when the user right-clicks a card to open the context menu. **/
    const handleContextMenu = (event: React.MouseEvent, repo: LocalGitRepository) => {
        event.preventDefault();
        setContextMenu({
            mouseX: event.clientX,
            mouseY: event.clientY,
            repo,
        });
    };

    return (
        <>
            <div className="page">
                <h1 className="page-title">Multi-Repository Manager Home🏠</h1>
                <p className="page-description"
                   style={{ textAlign: "center" }}>{`${user?.username}, manage any of the registered repositories found on your filesystem.`}</p>
                {notification && (
                    <NotificationModal
                        title={notification.title}
                        message={notification.message}
                        isError={notification.isError}
                        loading={notification.loading}
                        onClose={closeModal} />
                )}
                <div className="card-container">
                    {localGitRepositories && localGitRepositories.length > 0 && (
                        localGitRepositories.map((repo) => (
                            <GitRepoOverviewCard key={repo.id}
                                                 repository={repo}
                                                 url={`/status/${repo.name}/${repo.id}`}
                                                 onDelete={handleRepoDelete}
                                                 onContextMenu={(e) => handleContextMenu(e, repo)} />
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
                                <li onClick={() => window.location.href = `/status/${contextMenu.repo.name}/${contextMenu.repo.id}`}>
                                    Open Status Page
                                </li>
                                <li onClick={() => handleRepoDelete(contextMenu.repo.id)}>
                                    Delete
                                </li>
                                <li onClick={() => handleIgnoreAction(contextMenu.repo.id)}>
                                    Ignore
                                </li>
                            </ul>
                        </div>
                    )}
                </div>
                    <br/>
                    <button
                        className="submit-button"
                        type="submit"
                        onClick={handleAddLocalGitRepositories}
                    >
                        Add Git Repos from Local Machine
                    </button>
                <br/>
            </div>
        </>
    );
}

export default HomeTab;