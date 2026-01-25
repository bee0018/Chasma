import React, { useEffect, useState } from "react";
import { useNavigate, useParams } from "react-router-dom";
import "../../css/RepositoryStatusPage.css";
import "../../css/InfoTable.css";
import {
    ApplyStagingActionRequest,
    GitDiffRequest,
    GitPullRequest,
    GitStatusRequest,
    RepositoryStatusClient,
    RepositoryStatusElement,
} from "../../API/ChasmaWebApiClient";
import NotificationModal from "../modals/NotificationModal";
import CommitModal from "../modals/CommitModal";
import { getUserEmail, getUserId } from "../../managers/LocalStorageManager";
import PushModal from "../modals/PushModal";
import { isBlankOrUndefined } from "../../stringHelperUtil";
import CheckoutModal from "../modals/CheckoutModal";
import PullRequestModal from "../modals/PullRequestModal";
import CreateIssueModal from "../modals/CreateIssueModal";
import DeleteBranchModal from "../modals/DeleteBranchModal";
import { apiBaseUrl } from "../../environmentConstants";
import ExecuteShellCommandsModal from "../modals/ExecuteShellCommandsModal";
import {DiffLine} from "../types/CustomTypes";

/** Status client for the API **/
const statusClient = new RepositoryStatusClient(apiBaseUrl);

/**
 * Parses the unified diff and track line numbers.
 * @param diff The line difference.
 */
function parseUnifiedDiff(diff: string): DiffLine[] {
    let oldLineNum = 0;
    let newLineNum = 0;
    const lines: DiffLine[] = [];
    diff.split("\n").forEach((line) => {
        if (line.startsWith("@@")) {
            const match = line.match(/@@ -(\d+),?\d* \+(\d+),?\d* @@/);
            if (match) {
                oldLineNum = parseInt(match[1], 10) - 1;
                newLineNum = parseInt(match[2], 10) - 1;
            }
            lines.push({ type: "hunk", content: line });
        } else if (line.startsWith("+")) {
            newLineNum++;
            lines.push({ type: "add", content: line.slice(1), oldLineNumber: undefined, newLineNumber: newLineNum });
        } else if (line.startsWith("-")) {
            oldLineNum++;
            lines.push({ type: "remove", content: line.slice(1), oldLineNumber: oldLineNum, newLineNumber: undefined });
        } else {
            oldLineNum++;
            newLineNum++;
            lines.push({
                type: "context",
                content: line.startsWith(" ") ? line.slice(1) : line,
                oldLineNumber: oldLineNum,
                newLineNumber: newLineNum,
            });
        }
    });

    return lines;
}

/**
 * Initializes a new instance of the Repository Status Page class.
 * @constructor
 */
const RepositoryStatusPage: React.FC = () => {
    /** The repository name and identifier from the url. **/
    const { repoName, repoId } = useParams<{ repoName: string; repoId: string }>();

    /** The navigate function to switch between views. **/
    const navigate = useNavigate();

    /** Gets or sets the repository status elements. **/
    const [statusElements, setStatusElements] = useState<RepositoryStatusElement[] | undefined>(undefined);

    /** Gets or sets the notification **/
    const [notification, setNotification] = useState<{
        title: string;
        message: string | undefined;
        isError: boolean | undefined;
        loading?: boolean;
    } | null>(null);

    /** Gets or sets a flag indicating whether the user is editing the commit message. **/
    const [isEditingCommitMessage, setIsEditingCommitMessage] = useState(false);

    /** Gets or sets a flag indicating whether the user is pushing changes. **/
    const [isPushingChanges, setIsPushingChanges] = useState(false);

    /** Gets or sets a flag indicating whether the user is checking out changes. **/
    const [isCheckingOut, setIsCheckingOut] = useState(false);

    /** Gets or sets a flag indicating whether the user is creating a pull request. **/
    const [isCreatingPullRequest, setIsCreatingPullRequest] = useState(false);

    /** Gets or sets a flag indicating whether the user is creating an issue. **/
    const [isCreatingIssue, setIsCreatingIssue] = useState(false);

    /** Gets or sets a flag indicating whether the user is deleting a branch. **/
    const [isDeletingBranch, setIsDeletingBranch] = useState(false);

    /** Gets or sets a flag indicating whether the user executing shell commands. **/
    const [isExecutingShellCommands, setIsExecutingShellCommands] = useState(false);

    /** Gets or sets the number of commits the local repo is ahead of the remote. **/
    const [commitsAhead, setCommitsAhead] = useState<number | undefined>(0);

    /** Gets or sets the number of commits the local repo is behind the remote. **/
    const [commitsBehind, setCommitsBehind] = useState<number | undefined>(0);

    /** Gets or sets the local branch name. **/
    const [branchName, setBranchName] = useState<string | undefined>("");

    /** Gets or sets the latest commit hash. **/
    const [commitHash, setCommitHash] = useState<string | undefined>("");

    /** Gets or sets the remote branch URL. **/
    const [branchUrl, setBranchUrl] = useState<string | undefined>(undefined);

    /** Gets or sets the selected file. **/
    const [selectedFile, setSelectedFile] = useState<RepositoryStatusElement | null>(null);

    /** Gets or sets the raw diff. **/
    const [rawDiff, setRawDiff] = useState<string>("");

    /** Gets or sets a value indicating whether the diff viewer is in split mode. **/
    const [isSplitView, setIsSplitView] = useState(false);

    /**
     * Closes the modal once the user confirms the message
     */
    const closeModal = () => {
        setNotification(null);
    }

    /** Load git status every 2.5s **/
    useEffect(() => {
        if (!repoId) return;
        handleGitStatusRequest();
        const interval = setInterval(() => {
            handleGitStatusRequest();
            if (selectedFile !== null) {
                handleGetGitDiffRequest(selectedFile, selectedFile.isStaged)
            }
        }, 2500);
        return () => clearInterval(interval);
    }, [repoId, selectedFile]);

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

            setStatusElements(response.statusElements);
            setCommitsBehind(response.commitsBehind);
            setCommitsAhead(response.commitsAhead);
            setBranchName(response.branchName);
            setBranchUrl(response.remoteUrl);
            setCommitHash(response.commitHash);
        } catch {
            setNotification({
                title: "Failed to perform 'git status' operation!",
                message: "An internal server error has occurred. Review logs.",
                isError: true,
            });
        }
    }

    /**
     * Handles the request to apply the staging action to the selected file.
     */
    async function handleApplyStagingActionRequest() {
        const stagingAction = !selectedFile?.isStaged
        const request = new ApplyStagingActionRequest();
        request.repoKey = selectedFile?.repositoryId;
        request.fileName = selectedFile?.filePath;
        request.isStaging = stagingAction;

        try {
            const response = await statusClient.applyStagingAction(request);
            const action = stagingAction ? "stage" : "unstage";
            if (response.isErrorResponse) {
                setNotification({
                    title: `Could not ${action}!`,
                    message: response.errorMessage,
                    isError: true,
                });
                return;
            }
            setStatusElements(response.statusElements);
            const file = statusElements?.find(i => i.filePath === selectedFile?.filePath);
            if (!file) {
                setSelectedFile(null);
            } else {
                file.isStaged = stagingAction;
                setSelectedFile(file);
                await handleGetGitDiffRequest(file, stagingAction);
            }
        } catch {
            setNotification({
                title: `Failed to perform '${stagingAction ? "stage" : "unstage"}' operation!`,
                message: "An internal server error has occurred. Review logs.",
                isError: true,
            });
        }
    }

    /**
     * Handles the event when user attempts to go to the branch URL.
     */
    const handleNavigateToBranchUrl = () => {
        if (!branchUrl) return;
        window.open(branchUrl, "_blank");
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

        const request = new GitPullRequest();
        request.repositoryId = repoId!;
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
        } catch {
            setNotification({
                title: "Could not pull changes!",
                message: "An internal server error has occurred. Review logs.",
                isError: true,
            });
        }
    };

    /**
     * Handles the event when the user wants to get the diff of a file.
     * @param file The file to be diffed.
     * @param isStaged Flag indicating whether the file is in the staging area.
     */
    async function handleGetGitDiffRequest(file: RepositoryStatusElement, isStaged: boolean | undefined) {
        if (!repoId) return;
        const request = new GitDiffRequest();
        request.repositoryId = repoId;
        request.filePath = file.filePath;
        request.isStaged = isStaged;
        try {
            const response = await statusClient.getGitDiff(request);
            if (response.isErrorResponse) {
                console.error(response.errorMessage);
                setRawDiff("");
                return;
            }
            setRawDiff(response.diffContent!);
        } catch (e) {
            console.error(e);
        }
    }

    /**
     * Handles the event when the user clicks a file from the unstaged/staged changes.
     * @param file The file to be selected.
     * @param isStaged Flag indicating whether the file is in the staging area.
     */
    const handleSelectFile = (file: RepositoryStatusElement, isStaged: boolean) => {
        setSelectedFile(file);
        handleGetGitDiffRequest(file, isStaged);
    };

    /** The parsed unified diff. */
    const parsedDiff = parseUnifiedDiff(rawDiff);
    return (
        <>
            <aside className="sidebar">
                <div className="tab" style={{ marginTop: "150px" }} onClick={() => navigate("/home")}>Home</div>
                <br />
                <div className="tab" onClick={handleGitStatusRequest}>Refresh Repo Status ⟳</div>
                <div className="tab" onClick={handlePullRequest}>Pull ↓</div>
                <div className="tab" onClick={() => setIsEditingCommitMessage(true)}>Commit ↑</div>
                <div className="tab" onClick={() => setIsPushingChanges(true)}>Push ↗</div>
                <br />
                <div className="tab" onClick={() => setIsCheckingOut(true)}>Checkout Branch</div>
                <div className="tab" onClick={() => setIsDeletingBranch(true)}>Delete Branch</div>
                <div className="tab" onClick={() => setIsCreatingPullRequest(true)}>Create Pull Request</div>
                <div className="tab" onClick={() => setIsCreatingIssue(true)}>Create Issue</div>
                <br />
                <div className="tab" onClick={() => setIsExecutingShellCommands(true)}>Custom Shell Commands</div>
            </aside>
            <h1 className="repository-title-header">{repoName} Status Manager</h1>
            <div className="page-layout">
                <div className="left-panel">
                    <table className="info-table summary-table" onClick={handleNavigateToBranchUrl}>
                        <caption>{`${branchName} - ${!isBlankOrUndefined(commitHash) ? commitHash : ""}`}</caption>
                        <thead>
                        <tr><th colSpan={2}>Repository Summary</th></tr>
                        </thead>
                        <tbody>
                        <tr><td>Commits Ahead</td><td>{commitsAhead}</td></tr>
                        <tr><td>Commits Behind</td><td>{commitsBehind}</td></tr>
                        </tbody>
                    </table>
                    <br/>
                    <div className="file-changes-container">
                        <h1 className="page-description">Staged Changes</h1>
                        <table className="info-table">
                            {statusElements?.filter(e => e.isStaged).map((element, index) => (
                                <tbody key={index}>
                                <tr>
                                    <td onClick={() => handleSelectFile(element, true)}>{element.filePath}</td>
                                </tr>
                                </tbody>
                            ))}
                        </table>
                    </div>
                    <div className="file-changes-container">
                        <h1 className="page-description">Unstaged Changes</h1>
                        <table className="info-table">
                            {statusElements?.filter(e => !e.isStaged).map((element, index) => (
                                <tbody key={index}>
                                <tr>
                                    <td onClick={() => handleSelectFile(element, false)}>{element.filePath}</td>
                                </tr>
                                </tbody>
                            ))}
                        </table>
                    </div>
                </div>
                <div className="diff-right-panel diff-resizable">
                    <div style={{
                        display: "flex",
                        justifyContent: "space-between",
                        alignItems: "center",
                        marginBottom: "8px", }}>
                        <button
                            className="submit-button"
                            onClick={() => setIsSplitView(!isSplitView)}
                        >
                            {isSplitView ? "Toggle Unified View" : "Toggle Split View"}
                        </button>
                        <button
                            className="submit-button"
                            style={{background: selectedFile?.isStaged ? "red" : "green"}}
                            hidden={selectedFile === null}
                            onClick={handleApplyStagingActionRequest}
                        >
                            {selectedFile?.isStaged ? "Unstage" : "Stage"}
                        </button>
                    </div>
                    {selectedFile ? (
                        <div className={`diff-viewer ${isSplitView ? "diff-side-by-side" : ""}`}>
                            {/* Unified View */}
                            {!isSplitView && (
                                <div className="diff-panel">
                                    <div className="diff-panel-header">Unified Diff: {selectedFile.filePath}</div>
                                    {parsedDiff.map((line, i) => (
                                        <div
                                            key={i}
                                            className={`diff-line ${
                                                line.type === "add"
                                                    ? "diff-added"
                                                    : line.type === "remove"
                                                        ? "diff-removed"
                                                        : ""
                                            }`}
                                        >
                                            <span className="diff-line-number">{line.oldLineNumber ?? ""}</span>
                                            <span className="diff-line-number">{line.newLineNumber ?? ""}</span>
                                            <span className="diff-code">{line.content}</span>
                                        </div>
                                    ))}
                                </div>
                            )}
                            {isSplitView && (
                                <>
                                    <div className="diff-panel">
                                        <div className="diff-panel-header">Original: {selectedFile.filePath}</div>
                                        {parsedDiff.map((line, i) => (
                                            <div
                                                key={i}
                                                className={`diff-line ${line.type === "remove" ? "diff-removed" : ""}`}
                                            >
                                                <span className="diff-line-number">{line.oldLineNumber ?? ""}</span>
                                                <span className="diff-code">{line.type === "add" ? "" : line.content}</span>
                                            </div>
                                        ))}
                                    </div>
                                    <div className="diff-panel">
                                        <div className="diff-panel-header">Modified: {selectedFile.filePath}</div>
                                        {parsedDiff.map((line, i) => (
                                            <div
                                                key={i}
                                                className={`diff-line ${line.type === "add" ? "diff-added" : ""}`}
                                            >
                                                <span className="diff-line-number">{line.newLineNumber ?? ""}</span>
                                                <span className="diff-code">{line.type === "remove" ? "" : line.content}</span>
                                            </div>
                                        ))}
                                    </div>
                                </>
                            )}
                        </div>
                    ) : (
                        <div style={{ color: "#ccc", padding: "12px" }}>Select a file to view diff</div>
                    )}
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
            {isEditingCommitMessage &&
                <CommitModal
                    repositoryId={repoId}
                    email={getUserEmail()}
                    userId={getUserId()}
                    onClose={() => setIsEditingCommitMessage(false)}
                    onSuccess={() => setSelectedFile(null)} />
            }
            {isPushingChanges &&
                <PushModal
                    repositoryId={repoId}
                    onClose={() => setIsPushingChanges(false)}
                    onSuccess={() => setSelectedFile(null)} />
            }
            {isCheckingOut &&
                <CheckoutModal
                    repositoryId={repoId}
                    onClose={() => setIsCheckingOut(false)}
                    onSuccess={() => setSelectedFile(null)} />
            }
            {isCreatingPullRequest &&
                <PullRequestModal
                    onClose={() => setIsCreatingPullRequest(false)}
                    repositoryId={repoId} repoName={repoName} />
            }
            {isCreatingIssue &&
                <CreateIssueModal
                    onClose={() => setIsCreatingIssue(false)}
                    repositoryId={repoId} repoName={repoName} />
            }
            {isDeletingBranch &&
                <DeleteBranchModal
                    onClose={() => setIsDeletingBranch(false)}
                    repositoryId={repoId} />
            }
            {isExecutingShellCommands &&
                <ExecuteShellCommandsModal
                    repositoryId={repoId}
                    onClose={() => setIsExecutingShellCommands(false)}
                    onSuccess={() => setSelectedFile(null)} />
            }
        </>
    );
};
export default RepositoryStatusPage;