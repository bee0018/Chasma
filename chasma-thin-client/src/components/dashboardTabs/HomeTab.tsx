import React, {useEffect, useState} from "react";
import GitRepoOverviewCard from "../GitRepoOverviewCard";
import {
    DeleteRepositoryRequest,
    IgnoreRepositoryRequest,
    LocalGitRepository,
} from "../../API/ChasmaWebApiClient";
import {useCacheStore} from "../../managers/CacheManager";
import {configClient} from "../../managers/ApiClientManager";
import { useNavigate } from "react-router-dom";
import { handleApiError } from "../../managers/TransactionHandlerManager";

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

    /** The use navigation utility. **/
    const navigate = useNavigate();

    /** Sets the notification modal. */
    const setNotification = useCacheStore(state => state.setNotification);

    /** Gets or sets a value indicating whether the request is ready to be sent. */
    const [disableSendButton, setDisableSendButton] = useState(false);

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
            const errorNotification = handleApiError(e, navigate);
            setNotification(errorNotification);
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
                const errorNotification = handleApiError(e, navigate);
                setNotification(errorNotification);
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

    /** Gets or sets the context menu. **/
    const [contextMenu, setContextMenu] = useState<{
        mouseX: number;
        mouseY: number;
        repo: LocalGitRepository;
    } | null>(null);

    /** Sends a request to the add the local git repositories on the filesystem. **/
    async function handleAddLocalGitRepositories() {
        if (disableSendButton) {
            setNotification({
                title: "Currently adding local git repositories to the system.",
                message: "Please await repository additions while your request is being processed. May take a while depending on how large your filesystem is.",
                isError: false,
                loading: true
            });
            return;
        }

        setDisableSendButton(true);
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
                setDisableSendButton(false);
                return;
            }

            setNotification({
                title: "Git repository retrieval finished!",
                message: "Close the modal to find the repositories found on your system.",
                isError: false,
            });
            useCacheStore.getState().setRepositories(response.currentRepositories);
            setDisableSendButton(false);
        }
        catch (e) {
            const errorNotification = handleApiError(e, navigate);
            setNotification(errorNotification);
            setDisableSendButton(false);
        }
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
        }
    };

    /** Handles the event when there is an error deleting a repository. **/
    const handleRepoDeletionError = (errorMessage: string | undefined) => {
        const errorNotification = handleApiError(errorMessage, navigate, "Could not delete repository!", "Review server logs for more information.");
        setNotification(errorNotification);
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
            const errorNotification = handleApiError(e, navigate, "Repository ignore action failed!", "Review server logs for more information.");
            setNotification(errorNotification);
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
            <div>
                <div>
                    <h1>Manage Git. Effortlessly.</h1>
                    <p>{`${user?.userName}, manage any of the registered repositories found on your filesystem.`}</p>
                </div>
                <div>
                    {localGitRepositories && localGitRepositories.length > 0 && (
                        localGitRepositories.map((repo) => (
                            <GitRepoOverviewCard
                                key={repo.id}
                                repository={repo}
                                url={`/status/${repo.name}/${repo.id}`}
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
                                <li onClick={() => navigate(`/status/${contextMenu.repo.name}/${contextMenu.repo.id}`)}>
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
                <button
                    className="submit-button"
                    type="submit"
                    onClick={handleAddLocalGitRepositories}
                >
                    Add Git Repos from Local Machine
                </button>
            </div>
        </>
    );
}

export default HomeTab;
