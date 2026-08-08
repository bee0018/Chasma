import { useState } from "react";
import { useDocumentTitle } from "../../util/useDocumentTitle";
import Checkbox from "../Checkbox";
import { BranchSyncStatus, GetBranchSyncStatusRequest } from "../../API/ChasmaWebApiClient";
import BranchSyncModalConfirmationModal from "../modals/BranchSyncModalConfirmation";
import { useCacheStore } from "../../managers/CacheManager";
import { statusClient } from "../../managers/ApiClientManager";
import { handleApiError } from "../../managers/TransactionHandlerManager";
import { useNavigate } from "react-router-dom";
import { isBlankOrUndefined } from "../../stringHelperUtil";

/**
 * Initializes a new instance of the RepositoryHealthTab component
 * @constructor
 */
const RepositoryHealthTab: React.FC = () => {
    useDocumentTitle("Health");

    /** Gets or sets a value indicating whether the user is syncing the head branch on each of the repositories. */
    const [syncSpecificBranch, setSyncSpecificBranch] = useState<boolean>(false);

    /** Gets or sets the branch sync search query. **/
    const [branchSyncSearchQuery, setBranchSyncSearchQuery] = useState("");

    /** Gets or sets a value indicating whether the request is ready to be sent. */
    const [disableSendButton, setDisableSendButton] = useState(false);

    /** Gets or sets a value indicating whether the user is confirming branch synchronization. */
    const [isConfirmingBranchSync, setIsConfirmingBranchSync] = useState<boolean>(false);

    /** Gets or sets the global branch statuses. */
    const [branchStatuses, setBranchStatuses] = useState<BranchSyncStatus[]>([]);

    /** The logged-in user. **/
    const user = useCacheStore((state) => state.user);

    /** Sets the notification modal. */
    const setNotification = useCacheStore(state => state.setNotification);

    /** The navigation function. **/
    const navigate = useNavigate();

    /**
     * Gets the branch sync row theme based on the health score.
     * @param status The branch synchronization status.
     */
    const getBranchSyncRowTheme = (status: BranchSyncStatus | undefined) => {
        if (!status) {
            return "unknown";
        }

        if (!status.healthScore?.score) {
            return "unknown";
        }

        if (status.healthScore.score >= 0 && status.healthScore.score <= 59) {
            return "failure";
        }

        if (status.healthScore.score >= 60 && status.healthScore.score <= 69) {
            return "poor"
        }

        if (status.healthScore.score >= 70 && status.healthScore.score <= 79) {
            return "average"
        }

        if (status.healthScore.score >= 80 && status.healthScore.score <= 89) {
            return "good"
        }

        if (status.healthScore.score >= 90 && status.healthScore.score <= 99) {
            return "success"
        }

        if (status.healthScore.score === 100) {
            return "excellent"
        }
    };

    /**
         * Handles the event when the user wants to get the branch sync status.
         * @param isSkipping Flag indicating whether the user wants to skip getting builds.
         */
    const handleBranchSyncRequest = async (isSkipping: boolean) => {
        setDisableSendButton(true);
        let branchName: string = !isBlankOrUndefined(branchSyncSearchQuery) ? branchSyncSearchQuery : "HEAD branches";
        setNotification({
            title: `Getting branch health status for ${branchName}`,
            message: "Please wait while your request is being processed. May take a few moments depending on retrieving build information.",
            isError: false,
            loading: true
        });
        const request = new GetBranchSyncStatusRequest();
        request.branchName = branchSyncSearchQuery;
        request.userId = user?.userId;
        request.skipBuildRetrieval = isSkipping;
        request.syncSpecifiedBranch = syncSpecificBranch;
        try {
            const response = await statusClient.getBranchSyncStatus(request);
            if (response.isErrorResponse) {
                setNotification({
                    title: "Could not get branch health status!",
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
        catch (e) {
            const errorNotification = await handleApiError(e, navigate, "Error getting branch sync status!", "Review console logs for more information.");
            setNotification(errorNotification);
            setDisableSendButton(false);
        }
    };

    /**
    * Handles the event when the user wants to select a specific branch to sync across the repositories.
    * @param isSyncingSpecificBranch Flag indicating whether the user is syncing a specific branch.
    */
    const handleSyncBranchSelection = (isSyncingSpecificBranch: boolean) => {
        if (!isSyncingSpecificBranch) {
            setBranchSyncSearchQuery("");
        }

        setSyncSpecificBranch(isSyncingSpecificBranch);
    };

    return (
        <>
            <div className="snapshot-page">
                <header className="snapshot-page-header">
                    <div className="snapshot-page-title-group">
                        <h1 className="snapshot-page-title">
                            Repository Health
                        </h1>

                        <p className="snapshot-page-subtitle">
                            Keep your codebase fast, clean, and bulletproof with essential maintenance strategies that eliminate repository bloat, speed up daily workflows, and prevent technical debt 🩺
                        </p>
                    </div>
                </header>

                <div style={{ textAlign: "left", marginBottom: "10px" }}>
                    <Checkbox
                        label="Search health status for specific branch across repositories"
                        onBoxChecked={handleSyncBranchSelection}
                        checked={syncSpecificBranch}
                    />
                </div>
                {syncSpecificBranch &&
                    <input
                        type="text"
                        placeholder="Search specific branch health across all repositories..."
                        value={branchSyncSearchQuery}
                        onChange={e => setBranchSyncSearchQuery(e.target.value)}
                        className="input-field" />
                }
                <button
                    className="submit-button"
                    disabled={disableSendButton}
                    onClick={() => setIsConfirmingBranchSync(true)}
                    type="submit">
                    Search
                </button>
            </div>
            <br />
            {branchStatuses.length === 0 && <p className="no-workflows">No branch statuses have been retrieved yet.</p>}
            {branchStatuses.length > 0 &&
                <div className="workflow-table-container">
                    <table className="workflow-table">
                        <thead>
                            <tr>
                                <th>Repository Name</th>
                                <th>Branch Name</th>
                                <th>Branch Existence</th>
                                <th>Commits Behind Base</th>
                                <th>Commits Ahead Of Base</th>
                                <th>Pull Request Open</th>
                                <th>Build Status</th>
                                <th>Last Updated</th>
                                <th>Health Score</th>
                                <th>Health Status</th>
                            </tr>
                        </thead>
                        <tbody>
                            {branchStatuses.map((status, index) => (
                                <tr
                                    key={index}
                                    className={getBranchSyncRowTheme(status)}
                                >
                                    <td><b>{status.repositoryName}</b></td>
                                    <td>{status.branchName}</td>
                                    <td>{status.branchExists ? "Found ✅" : "Not Found ❌"}</td>
                                    <td
                                        style={{ color: Number(status.behind) > 0 ? "yellow" : "white" }}>
                                        {status.behind}
                                    </td>
                                    <td
                                        style={{ color: Number(status.ahead) > 0 ? "lightgreen" : "white" }}>
                                        {status.ahead}
                                    </td>
                                    <td>{status.pullRequestOpen ? "Open" : "-"}</td>
                                    <td>{status.buildStatus}</td>
                                    <td>{status.lastUpdated}</td>
                                    <td>{status.healthScore?.score}%</td>
                                    <td style={{ color: status.healthScore?.scoreCategory === "Failed to get status" ? "red" : "" }}>
                                        <b>{status.healthScore?.scoreCategory}</b>
                                        {status.healthScore?.description?.map(reason => {
                                            return <li>{reason}</li>
                                        })}
                                    </td>
                                </tr>
                            ))}
                        </tbody>
                    </table>
                </div>
            }
            {isConfirmingBranchSync &&
                <BranchSyncModalConfirmationModal
                    onClose={() => setIsConfirmingBranchSync(false)}
                    onSelected={handleBranchSyncRequest}
                />
            }
        </>
    );
}

export default RepositoryHealthTab;