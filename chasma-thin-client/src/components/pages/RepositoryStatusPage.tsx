import React, {useEffect, useState} from "react";
import {useNavigate, useParams} from "react-router-dom";
import "../../css/RepositoryStatusPage.css"
import '../../css/InfoTable.css'
import {
    ApplyStagingActionRequest, GitPullRequest,
    GitStatusRequest,
    RepositoryStatusClient,
    RepositoryStatusElement
} from "../../API/ChasmaWebApiClient";
import NotificationModal from "../modals/NotificationModal";
import CommitModal from "../modals/CommitModal";
import {getUserEmail, getUserId} from "../../managers/LocalStorageManager";
import PushModal from "../modals/PushModal";
import {isBlankOrUndefined} from "../../stringHelperUtil";

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

    /** Gets or sets a flag indicating whether the user is editing the commit message. **/
    const [isEditingCommitMessage, setIsEditingCommitMessage] = useState<boolean>(false);

    /** Gets or sets a flag indicating whether the user is pushing changes. **/
    const [isPushingChanges, setIsPushingChanges] = useState<boolean>(false);

    /** Gets or sets the number of commits the local repo is ahead of the remote. **/
    const [commitsAhead, setCommitsAhead] = useState<number | undefined>(0);

    /** Gets or sets the number of commits the local repo is behind the remote. **/
    const [commitsBehind, setCommitsBehind] = useState<number | undefined>(0);

    /** Gets or sets the local branch name. **/
    const [branchName, setBranchName] = useState<string | undefined>("");

    /** Gets or sets the remote branch URL. **/
    const [branchUrl, setBranchUrl] = useState<string | undefined>(undefined);

    /** Gets the navigate function. **/
    const navigate = useNavigate();

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
            setCommitsBehind(response.commitsBehind)
            setCommitsAhead(response.commitsAhead)
            setBranchName(response.branchName)
            setBranchUrl(response.remoteUrl)
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

    /**
     * Handles the event when user attempts to go to the branch URL.
     */
    const handleNavigateToBranchUrl = () => {
        if (isBlankOrUndefined(branchUrl)) {
            return;
        }

        // We know the branch url to be valid at this point.
        navigate(branchUrl!);
    };

    /**
     * Handles the event when the user wants to pull latest changes.
     */
    const handlePullRequest = async () => {
        setNotification({
            title: "Pulling changes...",
            message: "Please wait while your request is being processed.",
            isError: false,
            loading: true
        });

        const request = new GitPullRequest()
        request.repositoryId = repoId;
        request.email = getUserEmail();
        request.userId = getUserId();
        try {
            const response = await statusClient.pullChanges(request);
            if (response.isErrorResponse) {
                setNotification({
                    title: "Could not pull changes!",
                    message: response.errorMessage,
                    isError: true,
                });
                return;
            }

            setNotification({
                title: "Successfully pull changes!",
                message: "Close to dismiss.",
                isError: false,
            });
        } catch (e) {
            console.error(e);
            setNotification({
                title: "Could not pull changes!",
                message: "An internal server error has occurred. Review logs.",
                isError: true,
            });
        }
    };

    handleGitStatusRequest().catch(e => console.error(e));
    return (
        <>
            <aside className="sidebar">
                <div className="tab"
                    style={{marginTop: "150px"}}
                    onClick={handleGitStatusRequest}
                >
                    Refresh Repo Status ⟳
                </div>
                <div className="tab"
                     onClick={handlePullRequest}
                >
                    Pull ↓
                </div>
                <div className="tab"
                     onClick={() => setIsEditingCommitMessage(true)}
                >
                    Commit ↑
                </div>
                <div className="tab"
                     onClick={() => setIsPushingChanges(true)}
                >
                    Push ↗
                </div>
            </aside>
            <h1 className="repository-title-header">
                {repoName} Status Manager
            </h1>
            <div className="page-layout">
                <div className="left-panel">
                    <table className="info-table summary-table">
                        <caption onClick={handleNavigateToBranchUrl}
                            style={{textAlign: "left"}}>{`${branchName}`}</caption>
                        <thead>
                        <tr>
                            <th colSpan={2}>Repository Summary</th>
                        </tr>
                        </thead>
                        <tbody>
                        <tr>
                            <td>Commits Ahead</td>
                            <td>{commitsAhead}</td>
                        </tr>
                        <tr>
                            <td>Commits Behind</td>
                            <td>{commitsBehind}</td>
                        </tr>
                        </tbody>
                    </table>
                </div>
                <div className="page-content">
                    <div className="file-changes-container">
                        <h1 className="page-description">Staged Changes</h1>
                        <table className="info-table">
                            {statusElements
                                ?.filter(e => e.isStaged)
                                .map((element, index) => (
                                    <tbody key={index}>
                                    <tr
                                        onClick={() =>
                                            handleApplyStagingActionRequest(
                                                element,
                                                false
                                            )
                                        }
                                    >
                                        <td>{element.filePath}</td>
                                    </tr>
                                    </tbody>
                                ))}
                        </table>
                        <br/>
                    </div>

                    <div className="file-changes-container">
                        <h1 className="page-description">Unstaged Changes</h1>
                        <table className="info-table">
                            {statusElements
                                ?.filter(e => !e.isStaged)
                                .map((element, index) => (
                                    <tbody key={index}>
                                    <tr
                                        onClick={() =>
                                            handleApplyStagingActionRequest(
                                                element,
                                                true
                                            )
                                        }
                                    >
                                        <td>{element.filePath}</td>
                                    </tr>
                                    </tbody>
                                ))}
                        </table>
                        <br/>
                    </div>
                </div>
            </div>
            {notification && (
                <NotificationModal
                    title={notification.title}
                    message={notification.message}
                    isError={notification.isError}
                    loading={notification.loading}
                    onClose={closeModal}
                />
            )}
            {isEditingCommitMessage && (
                <CommitModal
                    repositoryId={repoId}
                    email={getUserEmail()}
                    userId={getUserId()}
                    onClose={() => setIsEditingCommitMessage(false)}
                />
            )}
            {isPushingChanges && (
                <PushModal
                    repositoryId={repoId}
                    onClose={() => setIsPushingChanges(false)}
                />
            )}
        </>
    );
};
export default RepositoryStatusPage;