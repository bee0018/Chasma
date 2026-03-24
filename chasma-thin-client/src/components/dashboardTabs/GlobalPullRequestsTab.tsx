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

    /** Gets or sets the global pull requests. */
    const [pullRequests, setPullRequests] = useState<RemotePullRequest[]>([]);

    /** Gets or sets the search query. **/
    const [searchQuery, setSearchQuery] = useState("");

    /** The filtered branches by search query. **/
    const filteredBranches = pullRequests.filter(pr =>
        pr.branchName!.toLowerCase().includes(searchQuery.toLowerCase())
    );

    /**
     * Closes the modal once the user confirms the message
     */
    const closeModal = () => {
        setNotification(null);
    }
console.log(searchQuery)
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
                <h1>System-Wide Open Pull Requests</h1>
                <p>Your centralized hub for tracking, reviewing, and merging open pull requests with precision and control🧠</p>
                <input
                    type="text"
                    placeholder="Search branches..."
                    value={searchQuery}
                    onChange={e => setSearchQuery(e.target.value)}
                    className="input-field" />
            </div>
            <br/>
            {pullRequests.length === 0 && searchQuery === "" && <p className="no-workflows">No pull requests retrieved yet.</p>}
            {pullRequests.length > 0 && searchQuery === "" &&
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
            {filteredBranches.length > 0 && searchQuery !== "" &&
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
                        {filteredBranches.map((pr, index) => (
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
