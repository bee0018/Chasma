import React, {useState} from "react";
import NotificationModal from "../modals/NotificationModal";
import {GetWorkflowResultsRequest, RepositoryStatusClient, WorkflowRunResult} from "../../API/ChasmaWebApiClient";
import "../../css/Dashboard.css"
import "../../css/App.css"
import { JSX } from "react/jsx-runtime";
import {useNavigate, useParams} from "react-router-dom";

/**
 * The repository status client that interfaces with the web API.
 */
const statusClient = new RepositoryStatusClient();

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

    /** The navigation function. **/
    const navigate = useNavigate();

    /**
     * Closes the modal once the user confirms the message
     */
    const closeModal = () => {
        setNotification(null);
    }

    /** Handles the event when the user request to get the workflow statuses. **/
    async function handleGetWorkFlowStatuses() {
        console.log("GetWorkFlowStatuses");
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

            console.log(request);
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
                message: `Close this modal and view the previous ${response.repositoryName} workflow build contents.`,
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

    /**
     * Gets the display elements of the workflow elements.
     * @return the workflow run statuses
     */
    function getWorkFlowRows() {
        let rows: JSX.Element[] = [];
        if (!workflows) {
            return <>No workflows retrieved.</>;
        }

        workflows.forEach(build => {
            rows.push(
                <tr onClick={() => window.open(build.workflowUrl, '_blank')}>
                    <td>{build.buildConclusion === "success" ? <i className="checkmark"/> : <i className="red-x"/>}</td>
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
            );
        });

        return rows;
    }

    return (
        <div className="page">
            <div className="page-header">
                <button
                    className="submit-button"
                    onClick={() => navigate('/home')}
                >
                    ← Home
                </button>
                <h1 className="page-title">GitHub Builds Board 📊</h1>
                <div style={{ textAlign: "center" }}>
                    <p className="page-description">Click the button below to retrieve build queue results.</p>
                </div>
            </div>
            {notification && (
                <NotificationModal
                    title={notification.title}
                    message={notification.message}
                    isError={notification.isError}
                    loading={notification.loading}
                    onClose={closeModal} />
            )}
            {repoName && workflows && workflows.length > 0 && (
                <div>
                    <table className="info-table"
                           style={{display: "contents"}}>
                        <caption style={{textAlign: "left"}}>{`${repoName} Most Recent ${workflows.length} Builds:`}</caption>
                        <thead>
                        <tr>
                            <th>Result</th>
                            <th>Branch Name</th>
                            <th>Run Number</th>
                            <th>Trigger</th>
                            <th>Commit Message</th>
                            <th>Status</th>
                            <th>Conclusion</th>
                            <th>Created</th>
                            <th>Updated</th>
                            <th>Author</th>
                        </tr>
                        </thead>
                        <tbody>
                        {getWorkFlowRows()}
                        </tbody>
                    </table>
                </div>

            )
            }
            <br/>
            <button
                className="submit-button"
                type="submit"
                onClick={handleGetWorkFlowStatuses}
            >
                Retrieve
            </button>
            <br/>
        </div>
    );
}

export default WorkflowRunsPage;