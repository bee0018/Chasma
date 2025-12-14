import React, {useState} from "react";
import '../../css/DasboardTab.css';
import GitRepoOverviewCard from "../GitRepoOverviewCard";
import {GitHubClient, LocalGitRepository} from "../../API/ChasmaWebApiClient";
import NotificationModal from "../modals/NotificationModal";

/** The Git API client. **/
const gitClient = new GitHubClient()

/**
 * The Home tab contents and display components.
 * @constructor Initializes a new instance of the HomeTab.
 */
const HomeTab: React.FC = () => {
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

    /** Gets or sets the time at which the last successful filesystem retrieval was conducted. **/
    const [retrievalTimestamp, setRetrievalTimestamp] = useState<Date | undefined>(() => {
        const timestamp = localStorage.getItem("retrievalTimestamp")
        if (!timestamp) return undefined;
        return JSON.parse(timestamp);
    });

    /** Sends a request to the retrieve the local git repositories on the filesystem. **/
    async function handleGetLocalGitRepositories() {
        setNotification({
            title: "Retrieving local git repositories on your filesystem...",
            message: "Please wait while your request is being processed. May take a while depending on how large your filesystem is.",
            isError: false,
            loading: true
        });

        try {
            const response = await gitClient.getLocalGitRepositories();
            setNotification({
                title: "Git repository retrieval finished!",
                message: "Close the modal to find the repositories found on your system.",
                isError: false,
            });
            setLocalGitRepositories(response.repositories);
            setRetrievalTimestamp(response.timestamp);
            localStorage.setItem("gitRepositories", JSON.stringify(response.repositories));
            localStorage.setItem("retrievalTimestamp", JSON.stringify(response.timestamp));
        }
        catch (e) {
            setNotification({
                title: "Git repository retrieval failed!",
                message: "Review server logs for more information.",
                isError: true,
            });
            setLocalGitRepositories(undefined);
            setRetrievalTimestamp(undefined);
            localStorage.removeItem("gitRepositories");
            localStorage.removeItem("retrievalTimestamp");
            console.error(`Could not get git repositories from filesystem: ${e}`);
        }
    }

    /**
     * Closes the modal once the user confirms the message
     */
    const closeModal = () => {
        setNotification(null);
    }

    return (
        <div>
            <h1 className="page-title">Multi-Repository Manager Home🏠</h1>
            <p className="page-description"
                style={{ textAlign: "center" }}>Manage any of the registered repositories found on your filesystem.</p>
            {retrievalTimestamp && (
                <h3 style={{ color: "lightgreen" }}>{`Results shown from ${retrievalTimestamp}.`}</h3>
            )}
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
                                             repoName={repo.name}
                                             repoOwner={repo.owner}
                                             url={repo.url} />
                    ))
                )}
            </div>
            <div style={{ justifySelf: "center" }}>
                <br/>
                <button
                    className="submit-button"
                    type="submit"
                    onClick={handleGetLocalGitRepositories}
                >
                    Find Git Repos from Local Machine
                </button>
            </div>
        </div>
    );
}

export default HomeTab;