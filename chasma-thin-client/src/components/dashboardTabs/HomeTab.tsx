import React, {useEffect, useState} from "react";
import '../../css/DasboardTab.css';
import GitRepoOverviewCard from "../GitRepoOverviewCard";
import {LocalGitRepository, RepositoryConfigurationClient} from "../../API/ChasmaWebApiClient";
import NotificationModal from "../modals/NotificationModal";
import {getUserId, getUsername} from "../../managers/LocalStorageManager";
import {apiBaseUrl} from "../../environmentConstants";

/** The Git API client. **/
const configClient = new RepositoryConfigurationClient(apiBaseUrl)

/**
 * The Home tab contents and display components.
 * @constructor Initializes a new instance of the HomeTab.
 */
const HomeTab: React.FC = () => {
    useEffect(() => {
        /** Retrieves the repository data from the web API. **/
        const retrieveUserRepositoryConfiguration = async () => {
            try {
                const userId = getUserId();
                const message = await configClient.getLocalGitRepositories(userId);
                setLocalGitRepositories(message.repositories);
                localStorage.setItem("gitRepositories", JSON.stringify(message.repositories));
            }
            catch (e) {
                setNotification({
                    title: "Git repository retrieval failed!",
                    message: "Review server logs for more information.",
                    isError: true,
                });
                setLocalGitRepositories(undefined);
                console.error(`Could not get git repositories from filesystem: ${e}`);
                localStorage.removeItem("gitRepositories");
            }
        };

        retrieveUserRepositoryConfiguration()
            .catch(e => {
            console.error(e.message);
        });
    }, []);

    /** Gets or sets the notification **/
    const [notification, setNotification] = useState<{
        title: string,
        message: string | undefined,
        isError: boolean | undefined,
        loading?: boolean
    } | null>(null);

    /** Gets or sets the local git repositories. **/
    const [localGitRepositories, setLocalGitRepositories] = useState<LocalGitRepository[] | undefined>(() => {
        const repos = localStorage.getItem("gitRepositories")
        if (!repos || repos.length === 0) return [];
        return JSON.parse(repos);
    });

    /** Sends a request to the add the local git repositories on the filesystem. **/
    async function handleAddLocalGitRepositories() {
        setNotification({
            title: "Adding local git repositories from logical drives...",
            message: "Please wait while your request is being processed. May take a while depending on how large your filesystem is.",
            isError: false,
            loading: true
        });

        try {
            const userId = getUserId();
            const response = await configClient.addLocalGitRepositories(userId);
            if (response.isErrorResponse) {
                setNotification({
                    title: "Git repository retrieval failed!",
                    message: response.errorMessage,
                    isError: true,
                });
                setLocalGitRepositories(undefined);
                return;
            }
            setNotification({
                title: "Git repository retrieval finished!",
                message: "Close the modal to find the repositories found on your system.",
                isError: false,
            });
            setLocalGitRepositories(response.currentRepositories);
        }
        catch (e) {
            setNotification({
                title: "Git repository retrieval failed!",
                message: "Review server logs for more information.",
                isError: true,
            });
            setLocalGitRepositories(undefined);
            console.error(`Could not get git repositories from filesystem: ${e}`);
        }
    }

    /**
     * Closes the modal once the user confirms the message
     */
    const closeModal = () => {
        setNotification(null);
    }

    /** Cleanup function to remove repository from cache after successful deletion. **/
    const handleRepoDelete = (repoId: string | undefined) => {
        setLocalGitRepositories(prev =>
            prev?.filter(repo => repo.id !== repoId)
        );
    };

    /** Handles the event when there is an error deleting a repository. **/
    const handleRepoDeletionError = (errorMessage: string | undefined) => {
        setNotification({
            title: "Could not delete repository!",
            message: errorMessage,
            isError: true,
        });
    }

    return (
        <>
            <div className="page">
                <h1 className="page-title">Multi-Repository Manager Home🏠</h1>
                <p className="page-description"
                   style={{ textAlign: "center" }}>{`${getUsername()}, manage any of the registered repositories found on your filesystem.`}</p>
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
                                                 repoId={repo.id}
                                                 repoName={repo.name}
                                                 repoOwner={repo.owner}
                                                 url={`/status/${repo.name}/${repo.id}`}
                                                 onDelete={handleRepoDelete}
                                                 onError={handleRepoDeletionError} />
                        ))
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