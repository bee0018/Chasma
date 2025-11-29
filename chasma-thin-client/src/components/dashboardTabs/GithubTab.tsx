import React, {useState} from "react";
import NotificationModal from "../modals/NotificationModal";
import {GitHubClient, WorkflowRunResult} from "../../API/ChasmaWebApiClient";
import "../../css/Dashboard.css"
import "../../css/App.css"
import { JSX } from "react/jsx-runtime";

/**
 * The GitHub client that interfaces with the web API.
 */
const gitHubClient = new GitHubClient();

/**
 * Initializes a new instance of the GithubTab.
 * @constructor
 */
const GithubTab: React.FC = () => {
    /** Gets or sets the notification **/
    const [notification, setNotification] = useState<{
        title: string,
        message: string | undefined,
        isError: boolean | undefined,
        loading?: boolean
    } | null>(null);

    /** Gets or sets the GitHub workflow results. **/
    const [workflows, setWorkflows] = useState<WorkflowRunResult[] | undefined>(undefined);

    /** Gets or sets the repository name. **/
    const [repoName, setRepoName] = useState<string | undefined>(undefined);

    /**
     * Closes the modal once the user confirms the message
     */
    const closeModal = () => {
        setNotification(null);
    }

    /** Handles the event when the user request to get the workflow statuses. **/
    async function handleGetWorkFlowStatuses() {
        setNotification({
            title: "Retrieving workflow runs...",
            message: "Please wait while your request is being processed.",
            isError: false,
            loading: true
        });

        try {
            const response = await gitHubClient.getChasmaWorkflowResults();
            if (response.isErrorResponse) {
                setNotification({
                    title: "Retrieval failed!",
                    message: response.errorMessage,
                    isError: true,
                });
                return;
            }

            setWorkflows(response.workflowRunResults)
            setRepoName(response.repositoryName);
            setNotification({
                title: `Successfully retrieved ${response.repositoryName} workflows!`,
                message: `Close this modal and view the previous ${response.repositoryName} workflow build contents.`,
                isError: response.isErrorResponse,
            });
        } catch (e) {
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
                <tr>
                    <td>{build.buildConclusion === "success" ? <span className="checkmark"/> : <i className="red-x"/>}</td>
                    <td>{build.branchName}</td>
                    <td>
                        <a href={build.workflowUrl} target="_blank" rel="noopener noreferrer">
                            {build.runNumber}
                        </a>
                    </td>
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
        <>
            <h1 className="page-title">GitHub Builds Board 📊</h1>
            <div style={{ textAlign: "center" }}>
                <p className="page-description">Click the button below to retrieve build queue results.</p>
                <br/>
                <button
                    className="submit-button"
                    type="submit"
                    onClick={handleGetWorkFlowStatuses}
                >
                    Retrieve
                </button>
            </div>
            <br/>
            <br/>
            {notification && (
                <NotificationModal
                    title={notification.title}
                    message={notification.message}
                    isError={notification.isError}
                    loading={notification.loading}
                    onClose={closeModal} />
            )}
            {repoName && workflows && workflows.length > 0 && (
                <table className="info-table">
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
            )
            }
        </>
    );
}

export default GithubTab;