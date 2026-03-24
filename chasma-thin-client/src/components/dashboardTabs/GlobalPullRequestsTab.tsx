import { useEffect, useState } from "react";
import NotificationModal from "../modals/NotificationModal";
import { RemotePullRequest } from "../../API/ChasmaWebApiClient";
import { remoteClient } from "../../managers/ApiClientManager";
import { capitalizeFirst } from "../../stringHelperUtil";

/**
 * Initializes a new instance of the GlobalPullRequestTab component
 * @constructor
 */
const GlobalPullRequestsTab: React.FC = () => {
    /** Gets or sets the notification **/
        const [notification, setNotification] = useState<{
            title: string,
            message: string | undefined,
            isError: boolean | undefined,
            loading?: boolean
        } | null>(null);

    /** Gets or sets the view mode of the dashboard. **/
    const [viewMode, setViewMode] = useState<"global" | "targeted">("global");

    /** Gets or sets the global pull requests. */
    const [pullRequests, setPullRequests] = useState<RemotePullRequest[]>([]);

    /**
     * Closes the modal once the user confirms the message
     */
    const closeModal = () => {
        setNotification(null);
    }

    /**
     * Retrieves the tracked pull requests from the backend API.
     */
    const retrievePullRequestsRequest = async () => {
        try {
            const message = await remoteClient.getGlobalPullRequests();
            if (message.pullRequests) {
                setPullRequests(message.pullRequests);
            }
        }
        catch (e) {
            setNotification({
                title: "Could not retrieve pull requests!",
                message: "Check internal server logs for more information.",
                isError: true,
            });
        }
    };

    /** Load git status every 2.5s **/
    useEffect(() => {
        retrievePullRequestsRequest();
        const interval = setInterval(() => {
            retrievePullRequestsRequest();
        }, 2500);
        return () => clearInterval(interval);
    }, []);

    return (
        <>
        <div className="workflow-page-container">
            <div className="workflow-page-header">
                <h1>System-Wide Pull Requests</h1>
                <p>Your centralized hub for tracking, reviewing, and merging pull requests with precision and control🧠</p>
            </div>
            <div className="command-mode-toggle">
                <button
                    className={`command-mode-button ${viewMode === "global" ? "active" : ""}`}
                    onClick={() => setViewMode("global")}
                >
                    Global
                </button>
                <button
                    className={`command-mode-button ${viewMode === "targeted" ? "active" : ""}`}
                    onClick={() => setViewMode("targeted")}
                >
                    Targeted
                </button>
            </div>
            <br/>
            {viewMode === "global" && pullRequests.length === 0 && <p className="no-workflows">No pull requests retrieved yet.</p>}
            {viewMode === "global" && pullRequests.length > 0 &&
                <div className="workflow-table-container">
                <table className="workflow-table">
                    <thead>
                    <tr>
                        <th>Number</th>
                        <th>Repo Name</th>
                        <th>Repo Owner</th>
                        <th>Branch</th>
                        <th>Active State</th>
                        <th>Merge State</th>
                        <th>Created At</th>
                        <th>Merged At</th>
                        <th>Merged</th>
                    </tr>
                    </thead>
                    <tbody>
                    {pullRequests.map((pr, index) => (
                        <tr
                            key={index}
                            className="success"
                            onClick={() => window.open(pr.htmlUrl, "_blank")}
                        >
                            <td>{pr.number}</td>
                            <td>{pr.repositoryName}</td>
                            <td>{pr.repositoryOwner}</td>
                            <td>{pr.branchName}</td>
                            <td>{capitalizeFirst(pr.activeState)}</td>
                            <td>{capitalizeFirst(pr.mergeableState)}</td>
                            <td>{pr.createdAt}</td>
                            <td>{pr.mergedAt}</td>
                            <td>{pr.merged}</td>
                        </tr>
                    ))}
                    </tbody>
                </table>
            </div>
            }
            {notification && (
                <NotificationModal
                    title={notification.title}
                    message={notification.message}
                    isError={notification.isError}
                    loading={notification.loading}
                    onClose={closeModal} />
            )}
        </div>
        </>
    );
};

export default GlobalPullRequestsTab;
