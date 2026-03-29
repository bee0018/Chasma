import { useEffect, useState } from "react";
import { BranchSyncStatus, GetBranchSyncStatusRequest, RemotePullRequest } from "../../API/ChasmaWebApiClient";
import { remoteClient, statusClient } from "../../managers/ApiClientManager";
import { capitalizeFirst } from "../../stringHelperUtil";
import { GlobalViewMode } from "../types/CustomTypes";
import { useCacheStore } from "../../managers/CacheManager";
import { useNavigate } from "react-router-dom";
import { handleApiError } from "../../managers/TransactionHandlerManager";

/**
 * Initializes a new instance of the GlobalPullRequestTab component
 * @constructor
 */
const GlobalRepositoryTab: React.FC = () => {
    /** Gets or sets the global pull requests. */
    const [pullRequests, setPullRequests] = useState<RemotePullRequest[]>([]);

    /** Gets or sets the pull request search query. **/
    const [prSearchQuery, setPrSearchQuery] = useState("");

    /** Gets or sets the branch sync search query. **/
    const [branchSyncSearchQuery, setBranchSyncSearchQuery] = useState("");

    /** Gets or sets the global view mode. **/
    const [viewMode, setViewMode] = useState<GlobalViewMode>("prs");

    /** Gets or sets the global branch statuses. */
    const [branchStatuses, setBranchStatuses] = useState<BranchSyncStatus[]>([]);

    /** Gets or sets a value indicating whether the request is ready to be sent. */
    const [disableSendButton, setDisableSendButton] = useState(false);

    /** The logged-in user. **/
    const user = useCacheStore((state) => state.user);

    /** The navigation function. **/
    const navigate = useNavigate();

   /** Sets the notification modal. */
   const setNotification = useCacheStore(state => state.setNotification);

    /** The filtered branches by search query. **/
    const filteredBranches = pullRequests.filter(pr =>
        pr.branchName!.toLowerCase().includes(prSearchQuery.toLowerCase())
    );

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
            const errorNotification = handleApiError(e, navigate, "Could not retrieve pull requests!", "Check internal server logs for more information.");
            setNotification(errorNotification);
        }
    };

    /**
     * Handles the event when the user wants to get the branch sync status.
     */
    const handleBranchSyncRequest = async () => {
        setDisableSendButton(true);
        setNotification({
            title: `Getting branch sync status for ${branchSyncSearchQuery}`,
            message: "Please wait while your request is being processed. May take a few moments depending on retrieving build information.",
            isError: false,
            loading: true
        });
        const request = new GetBranchSyncStatusRequest();
        request.branchName = branchSyncSearchQuery;
        request.userId = user?.userId;
        try {
            const response = await statusClient.getBranchSyncStatus(request);
            if (response.isErrorResponse) {
                setNotification({
                    title: "Could not get branch sync status!",
                    message: response.errorMessage,
                    isError: true,
                });
                setDisableSendButton(false);
                return;
            }

           if (response.branchSyncStatuses) {
            setBranchStatuses(response.branchSyncStatuses);
           }

           setNotification(null);
           setDisableSendButton(false);
        }
        catch (e)
        {
            const errorNotification = handleApiError(e, navigate, "Error getting branch sync status!", "Review console logs for more information.");
            setNotification(errorNotification);
            setDisableSendButton(false);
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
            <section className="command-mode-section">
                <div className="command-mode-toggle">
                    <button
                        className={`command-mode-button ${viewMode === "prs" ? "active" : ""}`}
                        onClick={() => setViewMode("prs")}
                    >
                        PRs
                    </button>
                    <button
                        className={`command-mode-button ${viewMode === "branchSync" ? "active" : ""}`}
                        onClick={() => setViewMode("branchSync")}
                    >
                        Branch Sync
                    </button>
                </div>
            </section>
            {viewMode === "prs" &&
                <>
                    <div className="workflow-page-header">
                        <h1>System-Wide Open Pull Requests</h1>
                        <p>Your centralized hub for tracking, reviewing, and merging open pull requests with precision and control🧠</p>
                        <input
                            type="text"
                            placeholder="Search branches..."
                            value={prSearchQuery}
                            onChange={e => setPrSearchQuery(e.target.value)}
                            className="input-field" />
                    </div>
                    <br/>
                    {pullRequests.length === 0 && prSearchQuery === "" && <p className="no-workflows">No pull requests retrieved yet.</p>}
                    {pullRequests.length > 0 && prSearchQuery === "" &&
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
                    {filteredBranches.length > 0 && prSearchQuery !== "" &&
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
                </>
            }
            {viewMode === "branchSync" &&
                <>
                    <div className="workflow-page-header">
                        <h1>Cross-Repo Branches</h1>
                        <p>Keep every branch in lockstep—system-wide sync without the chaos🔀</p>
                        <input
                            type="text"
                            placeholder="Search branch to sync..."
                            value={branchSyncSearchQuery}
                            onChange={e => setBranchSyncSearchQuery(e.target.value)}
                            className="input-field" />
                        <button
                            className="submit-button"
                            disabled={disableSendButton}
                            onClick={handleBranchSyncRequest}
                            type="submit">
                                Search
                        </button>
                    </div>
                    <br/>
                    {branchStatuses.length === 0 && <p className="no-workflows">No branch statuses have been retrieved yet.</p>}
                    {branchStatuses.length > 0 &&
                        <div className="workflow-table-container">
                            <table className="workflow-table">
                                <thead>
                                <tr>
                                    <th>Repository Name</th>
                                    <th>Branch Existence</th>
                                    <th>Commits Behind Base</th>
                                    <th>Commits Ahead Of Base</th>
                                    <th>Pull Request Open</th>
                                    <th>Build Status</th>
                                    <th>Last Updated</th>
                                </tr>
                                </thead>
                                <tbody>
                                {branchStatuses.map((status, index) => (
                                    <tr
                                        key={index}
                                        className="success"
                                    >
                                        <td>{status.repositoryName}</td>
                                        <td
                                            style={{color: status.branchExists ? "white" : "orange"}}>
                                                {status.branchExists ? "Exists" : "Does not exist"}
                                        </td>
                                        <td
                                            style={{color: Number(status.behind) > 0 ? "yellow" : "white"}}>
                                                {status.behind}
                                        </td>
                                        <td
                                            style={{color: Number(status.ahead) > 0 ? "lightgreen" : "white"}}>
                                                {status.ahead}
                                        </td>
                                        <td>{status.pullRequestOpen ? "Open" : "-"}</td>
                                        <td>{status.buildStatus}</td>
                                        <td>{status.lastUpdated}</td>
                                    </tr>
                                ))}
                                </tbody>
                            </table>
                        </div>
                    }
                </>
            }
        </div>
        </>
    );
};

export default GlobalRepositoryTab;
