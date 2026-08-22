import { useCallback, useEffect, useState } from "react";
import { RemotePullRequest } from "../../API/ChasmaWebApiClient";
import { remoteClient } from "../../managers/ApiClientManager";
import { capitalizeFirst } from "../../stringHelperUtil";
import { useCacheStore } from "../../managers/CacheManager";
import { useNavigate } from "react-router-dom";
import { handleApiError } from "../../managers/TransactionHandlerManager";
import { useDocumentTitle } from "../../util/useDocumentTitle";
import CheckoutModal from "../modals/CheckoutModal";

/**
 * Initializes a new instance of the GlobalPullRequestTab component
 * @constructor
 */
const GlobalRepositoryTab: React.FC = () => {
    useDocumentTitle("Active PRs");

    /** Gets or sets the global pull requests. */
    const [pullRequests, setPullRequests] = useState<RemotePullRequest[]>([]);

    /** Gets or sets the pull request search query. **/
    const [prSearchQuery, setPrSearchQuery] = useState("");

    /** Gets or sets the selected repository identifier. */
    const [selectedRepositoryId, setSelectedRepositoryId] = useState<string | undefined>(undefined);

    /** Gets or sets the targeted branch to checkout. */
    const [targetedBranch, setTargetedBranch] = useState<string | undefined>(undefined);

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
    const retrievePullRequestsRequest = useCallback(async () => {
        try {
            const message = await remoteClient.getGlobalPullRequests();
            if (message.pullRequests) {
                setPullRequests(message.pullRequests);
            }
        }
        catch (e) {
            const errorNotification = await handleApiError(e, navigate, "Could not retrieve pull requests!", "Check internal server logs for more information.");
            setNotification(errorNotification);
        }
    }, [navigate, setNotification]);

    /**
     * Handles the event when the user wants to checkout a branch from a pull request.
     * @param pr The remote pull request.
     */
    const handleCheckoutBranch = (pr: RemotePullRequest) => {
        setSelectedRepositoryId(pr.repositoryId);
        setTargetedBranch(pr.branchName);
    };

    /**
     * Clears the checkout details.
     */
    const clearCheckoutDetails = () => {
        setSelectedRepositoryId(undefined);
        setTargetedBranch(undefined);
    };

    /** Gets pull request update every 2.5s **/
    useEffect(() => {
        retrievePullRequestsRequest();
        const interval = setInterval(() => {
            retrievePullRequestsRequest();
        }, 2500);
        return () => clearInterval(interval);
    }, [retrievePullRequestsRequest]);

    return (
        <>
            <div className="workflow-page-container">
                <div className="workflow-page-header">
                    <h1>System-Wide Active Change Requests</h1>
                    <p>Your centralized hub for tracking, reviewing, and merging open change requests with precision and control🧠</p>
                    <input
                        type="text"
                        placeholder="Search branches..."
                        value={prSearchQuery}
                        onChange={e => setPrSearchQuery(e.target.value)}
                        className="input-field" />
                </div>
                <br />
                {pullRequests.length === 0 && prSearchQuery === "" && <p className="no-workflows">No pull requests retrieved yet.</p>}
                {pullRequests.length > 0 && prSearchQuery === "" &&
                    <div className="workflow-table-container">
                        <div className="table-screen-warning">
                            <p><strong>Table hidden</strong></p>
                            <p>Please expand your browser window or switch to full screen to view this table.</p>
                        </div>
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
                                    <th>Checkout</th>
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
                                        <td>{pr.mergedAt ? "Yes" : ""}</td>
                                        <td>
                                            <button
                                                className="repo-action"
                                                onClick={(e) => {
                                                    e.stopPropagation();
                                                    handleCheckoutBranch(pr);
                                                }}
                                            >
                                                Checkout
                                            </button>
                                        </td>
                                    </tr>
                                ))}
                            </tbody>
                        </table>
                    </div>
                }
                {filteredBranches.length > 0 && prSearchQuery !== "" &&
                    <div className="workflow-table-container">
                        <div className="table-screen-warning">
                            <p><strong>Table hidden</strong></p>
                            <p>Please expand your browser window or switch to full screen to view this table.</p>
                        </div>
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
                {selectedRepositoryId &&
                    <CheckoutModal
                        repositoryId={selectedRepositoryId}
                        onClose={clearCheckoutDetails}
                        targetedBranch={targetedBranch}
                    />
                }
            </div>
        </>
    );
};

export default GlobalRepositoryTab;
