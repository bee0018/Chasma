import React, {useState} from "react";
import NotificationModal from "../modals/NotificationModal";
import {GetWorkflowResultsRequest, RepositoryStatusClient, WorkflowRunResult} from "../../API/ChasmaWebApiClient";
import "../../styles/App.css"
import {useNavigate, useParams} from "react-router-dom";
import {apiBaseUrl} from "../../environmentConstants";

/**
 * The repository status client that interfaces with the web API.
 */
const statusClient = new RepositoryStatusClient(apiBaseUrl);

/**
 * Initializes a new instance of the WorkflowRunsPage.
 * @constructor
 */
const WorkflowRunsPage: React.FC = () => {
    /** The repository name and owner from the url. **/
    const {repoName, repoOwner} = useParams<{repoName: string, repoOwner: string}>();

    /** Gets or sets the notification **/
    const [notification, setNotification] = useState<{
        title: string,
        message: string | undefined,
        isError: boolean | undefined,
        loading?: boolean
    } | null>(null);

    /** Gets or sets the GitHub workflow results. **/
    const [workflows, setWorkflows] = useState<WorkflowRunResult[] | undefined>(undefined);

    /** View mode: "card" or "table" **/
    const [viewMode, setViewMode] = useState<"card" | "table">("card");

    /** The navigation function. **/
    const navigate = useNavigate();

    /**
     * Closes the modal once the user confirms the message
     */
    const closeModal = () => {
        setNotification(null);
    }

    /** Handles the event when the user requests to get the workflow statuses. **/
    async function handleGetWorkFlowStatuses() {
        setNotification({
            title: "Retrieving workflow runs...",
            message: "Please wait while your request is being processed.",
            isError: false,
            loading: true
        });

        try {
            const request = new GetWorkflowResultsRequest();
            request.repositoryName = repoName;
            request.repositoryOwner = repoOwner;

            const response = await statusClient.getChasmaWorkflowResults(request);
            if (response.isErrorResponse) {
                setNotification({
                    title: "Retrieval failed!",
                    message: response.errorMessage,
                    isError: true,
                });
                return;
            }

            setWorkflows(response.workflowRunResults)
            setNotification({
                title: `Successfully retrieved ${response.repositoryName} workflows!`,
                message: `Close this modal and view the workflow build contents.`,
                isError: response.isErrorResponse,
            });
        } catch (e) {
            console.error(e);
            setNotification({
                title: "Failed to retrieve workflows!",
                message: "An internal server error has occurred. Review logs.",
                isError: true,
            });
        }
    }

    /** Renders the table view for workflows **/
    const renderTableView = () => {
        if (!workflows || workflows.length === 0) {
            return <p className="no-workflows">No workflows retrieved yet.</p>;
        }

        return (
            <div className="workflow-table-container">
                <table className="workflow-table">
                    <thead>
                    <tr>
                        <th>Result</th>
                        <th>Branch</th>
                        <th>Run #</th>
                        <th>Trigger</th>
                        <th>Commit</th>
                        <th>Status</th>
                        <th>Conclusion</th>
                        <th>Created</th>
                        <th>Updated</th>
                        <th>Author</th>
                    </tr>
                    </thead>
                    <tbody>
                    {workflows.map((build, index) => (
                        <tr key={index} className={build.buildConclusion === "success" ? "success" : "failure"} onClick={() => window.open(build.workflowUrl, "_blank")}>
                            <td>{build.buildConclusion === "success" ? "✔" : "✖"}</td>
                            <td>{build.branchName}</td>
                            <td>{build.runNumber}</td>
                            <td>{build.buildTrigger}</td>
                            <td>{build.commitMessage}</td>
                            <td>{build.buildStatus}</td>
                            <td>{build.buildConclusion}</td>
                            <td>{build.createdDate}</td>
                            <td>{build.updatedDate}</td>
                            <td>{build.authorName}</td>
                        </tr>
                    ))}
                    </tbody>
                </table>
            </div>
        );
    }

    return (
        <div className="workflow-page-container">
            <div className="workflow-actions-container">
                <button
                    className="home-button"
                    onClick={() => navigate('/home')}
                >
                    ← Home
                </button>
                <button
                    className="retrieve-button"
                    type="submit"
                    onClick={handleGetWorkFlowStatuses}
                >
                    Retrieve Workflows
                </button>
            </div>
            <div className="workflow-page-header">
                <h1>GitHub Builds Dashboard 📊</h1>
                <p>Retrieve the most recent workflow run results below.</p>
            </div>

            <div className="command-mode-toggle">
                <button
                    className={`command-mode-button ${viewMode === "card" ? "active" : ""}`}
                    onClick={() => setViewMode("card")}
                >
                    Card
                </button>
                <button
                    className={`command-mode-button ${viewMode === "table" ? "active" : ""}`}
                    onClick={() => setViewMode("table")}
                >
                    Table
                </button>
            </div>
            <br/>
            {viewMode === "card" ? (
                <div className="workflow-cards-container">
                    {workflows && workflows.length > 0 ? workflows.map((build, index) => (
                        <div key={index} className={`workflow-card ${build.buildConclusion === "success" ? "success" : "failure"}`} onClick={() => window.open(build.workflowUrl, '_blank')}>
                            <div className="workflow-card-header">
                                <span className="workflow-result">{build.buildConclusion === "success" ? "✔" : "✖"}</span>
                                <span className="workflow-branch">{build.branchName}</span>
                                <span className="workflow-run-number">#{build.runNumber}</span>
                            </div>
                            <div className="workflow-card-body">
                                <p><strong>Trigger:</strong> {build.buildTrigger}</p>
                                <p><strong>Commit:</strong> {build.commitMessage}</p>
                                <p><strong>Status:</strong> {build.buildStatus}</p>
                                <p><strong>Conclusion:</strong> {build.buildConclusion}</p>
                                <p><strong>Created:</strong> {build.createdDate}</p>
                                <p><strong>Updated:</strong> {build.updatedDate}</p>
                                <p><strong>Author:</strong> {build.authorName}</p>
                            </div>
                        </div>
                    )) : <p className="no-workflows">No workflows retrieved yet.</p>}
                </div>
            ) : renderTableView()}
            {notification && (
                <NotificationModal
                    title={notification.title}
                    message={notification.message}
                    isError={notification.isError}
                    loading={notification.loading}
                    onClose={closeModal} />
            )}
        </div>
    );
}

export default WorkflowRunsPage;
