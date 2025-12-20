import React, {useEffect, useState} from "react";
import {useParams} from "react-router-dom";
import "../css/RepositoryStatusPage.css"
import '../css/InfoTable.css'
import {
    ApplyStagingActionRequest, GitStatusRequest,
    RepositoryStatusClient,
    RepositoryStatusElement
} from "../API/ChasmaWebApiClient";
import NotificationModal from "./modals/NotificationModal";

/** The status client for the web API. **/
const statusClient = new RepositoryStatusClient()

/**
 * Initializes a new instance of the Repository Status Page class.
 * @constructor
 */
const RepositoryStatusPage: React.FC = () => {
    /** The repository name and identifier from the url. **/
    const {repoName, repoId} = useParams<{repoName: string, repoId: string}>();

    /** Gets or sets the repository status elements. **/
    const [statusElements, setStatusElements] = useState<RepositoryStatusElement[] | undefined>(undefined);

    /** Gets or sets the notification **/
    const [notification, setNotification] = useState<{
        title: string,
        message: string | undefined,
        isError: boolean | undefined,
        loading?: boolean
    } | null>(null);

    /**
     * Closes the modal once the user confirms the message
     */
    const closeModal = () => {
        setNotification(null);
    }

    useEffect(() => {
            const interval = setInterval(async () => {
                await handleGitStatusRequest();
            }, 5000);

            return () => clearInterval(interval);
        },
        []);

    /** Handles the request to perform a 'git status' on the selected repository. **/
    async function handleGitStatusRequest() {
        try {
            const request = new GitStatusRequest();
            request.repositoryId = repoId;

            const response = await statusClient.getRepoStatus(request);
            if (response.isErrorResponse) {
                setNotification({
                    title: "Could not get repository status!",
                    message: response.errorMessage,
                    isError: true,
                });
                return;
            }

            setStatusElements(response.statusElements)
        }
        catch (e) {
            setNotification({
                title: "Failed to perform 'git status' operation!",
                message: "An internal server error has occurred. Review logs.",
                isError: true,
            });
        }
    }

    /**
     * Handles the request to apply the staging action.
     * @param file The file to be staged/unstaged.
     * @param isStaging Flag indicating whether to stage or unstage the file.
     */
    async function handleApplyStagingActionRequest(file: RepositoryStatusElement, isStaging: boolean) {
        const request = new ApplyStagingActionRequest();
        request.repoKey = file.repositoryId
        request.fileName = file.filePath
        request.isStaging = isStaging

        try {
            const response = await statusClient.applyStagingAction(request);
            const action = isStaging ? "stage" : "unstage"
            if (response.isErrorResponse) {
                setNotification({
                    title: `Could not ${action}!`,
                    message: response.errorMessage,
                    isError: true,
                });
                return;
            }

            setStatusElements(response.statusElements)
        }
        catch (e) {
            setNotification({
                title: "Failed to perform 'git add' operation!",
                message: "An internal server error has occurred. Review logs.",
                isError: true,
            });
        }
    }

    /** Gets the 'Staged Changes' and 'Unstaged Changes' containers. **/
    function getFileStatusContainers() {
        return (
            <div className="page-content">
                <div className="file-changes-container">
                    <h1 className="page-description">Staged Changes</h1>
                    <br/>
                    <table className="info-table">
                    {statusElements && statusElements.length > 0 && (
                        statusElements.filter(element => element.isStaged).map((element, index) => {
                            return (
                                <tbody key={index}>
                                    <tr style={{padding:'0px'}}
                                        onClick={() => handleApplyStagingActionRequest(element, false)}>
                                        <td>{element.filePath}</td>
                                    </tr>
                                </tbody>
                            )
                        })
                    )}
                    </table>
                    <br/>
                </div>
                <br/>
                <div className="file-changes-container">
                    <h1 className="page-description">Unstaged Changes</h1>
                    <br/>
                    <table className="info-table">
                        {statusElements && statusElements.length > 0 && (
                            statusElements.filter(element => !element.isStaged).map((element, index) => {
                                return (
                                    <tbody key={index}>
                                    <tr style={{padding:'0px'}}
                                        onClick={() => handleApplyStagingActionRequest(element, true)}>
                                        <td>{element.filePath}</td>
                                    </tr>
                                    </tbody>
                                )
                            })
                        )}
                    </table>
                    <br/>
                </div>
                <br/>
            </div>
        )
    }

    handleGitStatusRequest().catch(e => console.error(e));
    return (
        <>
            <button className="refresh-btn"
                    onClick={() => handleGitStatusRequest()}>
                ⟳ Refresh Repo Status
            </button>
            <h1 className="repository-title-header">{`${repoName} Status Manager`}</h1>
            {getFileStatusContainers()}

            {notification && (
                <NotificationModal
                    title={notification.title}
                    message={notification.message}
                    isError={notification.isError}
                    loading={notification.loading}
                    onClose={closeModal} />
            )}
        </>
    )
}
export default RepositoryStatusPage;