import React, {useState} from "react";
import {
    GetPipelineJobsRequest,
    GetWorkflowResultsRequest,
    RemoteHostPlatform,
    WorkflowRunResult
} from "../../API/ChasmaWebApiClient";
import "../../styles/App.css"
import {useNavigate, useParams} from "react-router-dom";
import {remoteClient} from "../../managers/ApiClientManager";
import {useCacheStore} from "../../managers/CacheManager";
import { handleApiError } from "../../managers/TransactionHandlerManager";

/**
 * Initializes a new instance of the WorkflowRunsPage.
 * @constructor
 */
const WorkflowRunsPage: React.FC = () => {
    /** The repository identifier from the url. **/
    const {repoId} = useParams<{repoId: string}>();

    /** Gets the selected repository. **/
    const selectedRepo = useCacheStore(state => state.repositories.find(i => i.id === repoId));

    /** Gets or sets the GitHub workflow results. **/
    const [workflows, setWorkflows] = useState<WorkflowRunResult[] | undefined>(undefined);

    /** View mode: "card" or "table" **/
    const [viewMode, setViewMode] = useState<"card" | "table">("card");

    /** Gets or sets the flag indicating whether to disable the send button. */
    const [disabledSendButton, setDisableSendButton] = useState(false);

    /** The navigation function. **/
    const navigate = useNavigate();

    /** Sets the notification modal. */
   const setNotification = useCacheStore(state => state.setNotification);

    /** Handles the event when the user requests to get the workflow statuses. **/
    async function handleGetWorkFlowStatuses() {
        setDisableSendButton(true);
        if (!selectedRepo?.hostPlatform) {
            setNotification({
                title: "Failed to retrieve build statuses!",
                message: "The remote host platform is not found.",
                isError: true,
            });
            setDisableSendButton(false);
            return;
        }

        setNotification({
            title: "Retrieving build statuses...",
            message: "Please wait while your request is being processed.",
            isError: false,
            loading: true
        });

        if (selectedRepo.hostPlatform === RemoteHostPlatform.GitHub) {
            await handleGetGitHubWorkflowResults();
        }
        else if (selectedRepo.hostPlatform === RemoteHostPlatform.GitLab) {
            await handleGetGitLabPipelineJobResults();
        }
        else if (selectedRepo.hostPlatform === RemoteHostPlatform.Bitbucket) {

        }
        else {
            setNotification({
                title: "Failed to retrieve build statuses!",
                message: "The remote host platform is not supported.",
                isError: true,
            });
        }

        setDisableSendButton(false);
    }

    /**
     * Handles the event when the user wants to get GitHub workflow run results.
     */
    const handleGetGitHubWorkflowResults = async () => {
        try {
            const request = new GetWorkflowResultsRequest();
            request.repositoryName = selectedRepo?.name;
            request.repositoryOwner = selectedRepo?.owner;

            const response = await remoteClient.getGitHubWorkflowResults(request);
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
                title: `Successfully retrieved ${response.repositoryName} workflows runs!`,
                message: `Close this modal and view the workflow build contents.`,
                isError: response.isErrorResponse,
            });
        } catch (e) {
            const errorNotification = handleApiError(e, navigate, "Failed to retrieve workflows!", "An internal server error has occurred. Review logs.");
            setNotification(errorNotification);
        }
    }

    /**
     * Handles the event when the user wants to get GitLab pipeline job results.
     */
    const handleGetGitLabPipelineJobResults = async () => {
        try {
            const request = new GetPipelineJobsRequest();
            request.repositoryId = selectedRepo?.id

            const response = await remoteClient.getPipelineJobs(request);
            if (response.isErrorResponse) {
                setNotification({
                    title: "Retrieval failed!",
                    message: response.errorMessage,
                    isError: true,
                });
                return;
            }

            setWorkflows(response.results)
            setNotification({
                title: `Successfully retrieved ${selectedRepo?.name} pipeline jobs!`,
                message: `Close this modal and view the workflow build contents.`,
                isError: response.isErrorResponse,
            });
        } catch (e) {
            const errorNotification = handleApiError(e, navigate, "Failed to retrieve pipeline jobs!", "An internal server error has occurred. Review logs.");
            setNotification(errorNotification);
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
                        <tr
                            key={index}
                            className={build.buildConclusion === "success" ? "success" : "failure"}
                            onClick={() => window.open(build.workflowUrl, "_blank")}
                        >
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
                    disabled={disabledSendButton}
                    onClick={handleGetWorkFlowStatuses}
                >
                    Retrieve Workflows
                </button>
            </div>
            <div className="workflow-page-header">
                <h1>{selectedRepo?.hostPlatform && `${RemoteHostPlatform[selectedRepo.hostPlatform]}`} Builds Dashboard 📊</h1>
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
        </div>
    );
}

export default WorkflowRunsPage;
