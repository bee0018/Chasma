import React, { useEffect, useState } from "react";
import { useNavigate, useParams } from "react-router-dom";
import {
    ApplyStagingActionRequest,
    GitDiffRequest,
    GitHubPullRequest,
    GitPullRequest,
    GitStatusRequest,
    RepositoryStatusClient,
    RepositoryStatusElement,
} from "../../API/ChasmaWebApiClient";
import NotificationModal from "../modals/NotificationModal";
import CommitModal from "../modals/CommitModal";
import PushModal from "../modals/PushModal";
import CheckoutModal from "../modals/CheckoutModal";
import PullRequestModal from "../modals/PullRequestModal";
import CreateIssueModal from "../modals/CreateIssueModal";
import DeleteBranchModal from "../modals/DeleteBranchModal";
import { apiBaseUrl } from "../../environmentConstants";
import ExecuteShellCommandsModal from "../modals/ExecuteShellCommandsModal";
import {DiffLine} from "../types/CustomTypes";
import {useCacheStore} from "../../managers/CacheManager";
import {capitalizeFirst} from "../../stringHelperUtil";

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

    /** Gets or sets the open pull request associated with the current branch. **/
    const [openPullRequests, setOpenPullRequests] = useState<GitHubPullRequest[] | undefined>(undefined);

    /** The logged-in user. **/
    const user = useCacheStore((state) => state.user);

    /** Gets or sets the context menu. **/
    const [contextMenu, setContextMenu] = useState<{
        mouseX: number;
        mouseY: number;
        statusElement: RepositoryStatusElement;
    } | null>(null);

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
            setOpenPullRequests(response.pullRequests);
        } catch (e) {
            console.error(e);
            setNotification({
                title: "Failed to perform 'git status' operation!",
                message: "An internal server error has occurred. Review logs.",
                isError: true,
            });
        }
    }

    /**
     * Handles the request to apply the staging action to the selected file.
     * @param selectedFile The selected file.
     */
    async function handleApplyStagingActionRequest(selectedFile: RepositoryStatusElement) {
        const stagingAction = !selectedFile.isStaged
        const request = new ApplyStagingActionRequest();
        request.repoKey = selectedFile.repositoryId;
        request.fileName = selectedFile.filePath;
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
        } catch (e) {
            console.error(e);
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
        request.email = user?.email;
        request.userId = user?.userId;
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

    /**
     * Handles the event when the user wants to get the diff of a file.
     * @param file The file to be diffed.
     * @param isStaged Flag indicating whether the file is in the staging area.
     */
    async function handleGetGitDiffRequest(file: RepositoryStatusElement | null, isStaged: boolean | undefined) {
        if (!repoId || file === null) return;
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
    const handleSelectFile = (file: RepositoryStatusElement | null, isStaged: boolean) => {
        setSelectedFile(file);
        handleGetGitDiffRequest(file, isStaged);
    };

    /** The parsed unified diff. */
    const parsedDiff = parseUnifiedDiff(rawDiff);

    /** Handles the event when the user right-clicks a file to open the context menu. **/
    const handleContextMenu = (event: React.MouseEvent, statusElement: RepositoryStatusElement) => {
        event.preventDefault();
        setContextMenu({
            mouseX: event.clientX,
            mouseY: event.clientY,
            statusElement,
        });
    };

    /**
     * Gets the push state phrase.
     * @param commitsAhead The number of commits ahead of the base branch.
     */
    function getPushStatePhrase(commitsAhead: number | undefined) {
        if (!commitsAhead || commitsAhead === 0) {
            return "Not ready";
        }

        return `Ready with ${commitsAhead} commit${commitsAhead && commitsAhead > 1 ? "s" : ""}`
    }

    useEffect(() => {
        const closeMenu = () => setContextMenu(null);
        window.addEventListener("click", closeMenu);
        return () => window.removeEventListener("click", closeMenu);
    }, []);

    return (
        <div className="dashboard-container">
            <aside className="sidebar">
                <div className="sidebar-profile">
                    <span className="profile-icon">📁</span>
                    <span>{repoName}</span>
                </div>
                <div className="tab" style={{ marginTop: "20px" }} onClick={() => navigate("/home")}>Home🏠</div>
                <div className="tab" onClick={handleGitStatusRequest}>Refresh Repo Status 🔄</div>
                <div className="tab" onClick={handlePullRequest}>Pull ⬇️</div>
                <div className="tab" onClick={() => setIsEditingCommitMessage(true)}>Commit 📌</div>
                <div className="tab" onClick={() => setIsPushingChanges(true)}>Push ⬆️</div>
                <div className="tab" style={{ marginTop: "20px" }} onClick={() => setIsCheckingOut(true)}>Checkout Branch🌿➡️</div>
                <div className="tab" onClick={() => setIsDeletingBranch(true)}>Delete Branch 🗑️🌿</div>
                <div className="tab" onClick={() => setIsCreatingPullRequest(true)}>Create Pull Request📥🌿</div>
                <div className="tab" onClick={() => setIsCreatingIssue(true)}>Create Issue🐛</div>
                <div className="tab" style={{ marginTop: "20px" }} onClick={() => setIsExecutingShellCommands(true)}>Custom Shell Commands🖥️</div>
            </aside>

            <div className="content">
                <div className="main-layout">
                    {/* Left side: Repo summary + staged/unstaged */}
                    <div className="left-panel">
                        <div className="panel-card">
                            <h2 className="page-description">Repository Summary</h2>
                            <div className="repo-summary" onClick={handleNavigateToBranchUrl}>
                                <div className="repo-summary-item">
                                    <span className="repo-summary-label">Branch:</span>
                                    <span className="repo-summary-value">{branchName}</span>
                                </div>
                                <div className="repo-summary-item">
                                    <span className="repo-summary-label">Current Commit:</span>
                                    <span className="repo-summary-value">{commitHash}</span>
                                </div>
                                <div className="repo-summary-item">
                                    <span className="repo-summary-label">Commits Ahead:</span>
                                    <span
                                        className="repo-summary-value"
                                        style={{ color: commitsAhead && commitsAhead > 0 ? "green" : "#aaaaaa" }}
                                    >
                                        {commitsAhead}
                                    </span>
                                </div>
                                <div className="repo-summary-item">
                                    <span className="repo-summary-label">Commits Behind:</span>
                                    <span
                                        className="repo-summary-value"
                                        style={{ color: commitsBehind && commitsBehind > 0 ? "red" : "#aaaaaa" }}
                                    >
                                        {commitsBehind}
                                    </span>
                                </div>
                                <div className="repo-summary-item">
                                    <span className="repo-summary-label">Push State:</span>
                                    <span
                                        className="repo-summary-value"
                                        style={{ color: commitsAhead && commitsAhead > 0 ? "green" : "white" }}
                                    >
                                        {getPushStatePhrase(commitsAhead)}
                                    </span>
                                </div>
                                <br/>
                                {openPullRequests && openPullRequests.length > 0 && (
                                    openPullRequests.map((pr) => (
                                        <div
                                            key={pr.number}
                                            onClick={e => {
                                                e.stopPropagation();
                                                window.open(pr.htmlUrl, "_blank");
                                            }}
                                        >
                                            <div className="repo-summary-item">
                                                <span className="repo-summary-label">PR Number:</span>
                                                <span className="repo-summary-value">{pr.number}</span>
                                            </div>

                                            <div className="repo-summary-item">
                                                <span className="repo-summary-label">Merged State:</span>
                                                <span
                                                    className="repo-summary-value"
                                                    style={{ color: !pr.merged ? "yellow" : "purple" }}
                                                >
                                                {!pr.merged ? "Unmerged" : "Merged"}
                                                </span>
                                            </div>

                                            <div className="repo-summary-item">
                                                <span className="repo-summary-label">Active State:</span>
                                                <span
                                                    className="repo-summary-value"
                                                    style={{ color: pr.activeState === "open" ? "lightgreen" : "lightblue" }}
                                                >
                                                    {pr.activeState === "open" ? "Active" : "Inactive"}
                                                </span>
                                            </div>

                                            <div className="repo-summary-item">
                                                <span className="repo-summary-label">Mergeable State:</span>
                                                <span
                                                    className="repo-summary-value"
                                                    style={{ color: pr.mergeableState === "clean" ? "lightblue" : "orange" }}
                                                >
                                                    {capitalizeFirst(pr.mergeableState)}
                                                </span>
                                            </div>

                                            <div className="repo-summary-item">
                                                <span className="repo-summary-label">Created at:</span>
                                                <span className="repo-summary-value">{pr.createdAt}</span>
                                            </div>

                                            <div className="repo-summary-item">
                                                <span className="repo-summary-label">Merged at:</span>
                                                <span className="repo-summary-value">{pr.mergedAt}</span>
                                            </div>
                                        </div>
                                    ))
                                )}
                            </div>
                        </div>


                        <div className="panel-card">
                            <h2 className="page-description">Staged Changes</h2>
                            {statusElements?.filter(e => e.isStaged).length ? (
                                <table className="status-table">
                                    <thead>
                                    <tr>
                                        <th>File</th>
                                        <th>Action</th>
                                    </tr>
                                    </thead>
                                    {statusElements?.filter(e => e.isStaged).map((element, index) => (
                                        <tbody key={index}>
                                        <tr className={selectedFile?.filePath === element.filePath ? "selected" : ""}>
                                            <td
                                                onClick={() => handleSelectFile(element, true)}
                                                onContextMenu={e => handleContextMenu(e, element)}
                                            >
                                                {element.filePath}
                                            </td>
                                            <td>
                                                <button
                                                    className="stage-button unstage"
                                                    onClick={e => {
                                                        e.stopPropagation();
                                                        handleApplyStagingActionRequest(element);
                                                    }}
                                                >
                                                    -
                                                </button>
                                            </td>
                                        </tr>
                                        </tbody>
                                    ))}
                                </table>
                            ) : <div className="empty-table">No staged changes</div>}
                        </div>

                        <div className="panel-card">
                            <h2 className="page-description">Unstaged Changes</h2>
                            {statusElements?.filter(e => !e.isStaged).length ? (
                                <table className="status-table">
                                    <thead>
                                    <tr>
                                        <th>File</th>
                                        <th>Action</th>
                                    </tr>
                                    </thead>
                                    {statusElements?.filter(e => !e.isStaged).map((element, index) => (
                                        <tbody key={index}>
                                        <tr className={selectedFile?.filePath === element.filePath ? "selected" : ""}>
                                            <td
                                                onClick={() => handleSelectFile(element, false)}
                                                onContextMenu={e => handleContextMenu(e, element)}
                                            >
                                                {element.filePath}
                                            </td>
                                            <td>
                                                <button
                                                    className="stage-button stage"
                                                    onClick={e => {
                                                        e.stopPropagation();
                                                        handleApplyStagingActionRequest(element);
                                                    }}
                                                >
                                                    +
                                                </button>
                                            </td>
                                        </tr>
                                        </tbody>
                                    ))}
                                </table>
                            ) : <div className="empty-table">No unstaged changes</div>}
                        </div>
                    </div>

                    {contextMenu && (
                        <div
                            className="context-menu"
                            style={{
                                top: contextMenu.mouseY,
                                left: contextMenu.mouseX,
                            }}
                            onClick={() => setContextMenu(null)}
                        >
                            <ul>
                                <li onClick={() => handleApplyStagingActionRequest(contextMenu?.statusElement)}>
                                    {contextMenu.statusElement && contextMenu.statusElement.isStaged ? "Unstage" : "Stage"}
                                </li>
                                <li>
                                    Ignore
                                </li>
                                <li>
                                    Remove
                                </li>
                            </ul>
                        </div>
                    )}

                    {/* Right side: Diff viewer */}
                    <div className="right-panel">
                        <div className="diff-toolbar">
                            <button
                                className="submit-button"
                                onClick={() => setIsSplitView(!isSplitView)}
                            >
                                {isSplitView ? "Toggle Unified View" : "Toggle Split View"}
                            </button>
                            {selectedFile && (
                                <button
                                    className="submit-button"
                                    style={{ background: selectedFile?.isStaged ? "red" : "green" }}
                                    onClick={() => handleApplyStagingActionRequest(selectedFile)}
                                >
                                    {selectedFile?.isStaged ? "Unstage" : "Stage"}
                                </button>
                            )}
                        </div>

                        {selectedFile ? (
                            <div className={`diff-viewer ${isSplitView ? "diff-side-by-side" : ""}`}>
                                {!isSplitView && (
                                    <div className="diff-panel">
                                        <div className="diff-panel-header">Unified Diff: {selectedFile.filePath}</div>
                                        {parsedDiff.map((line, i) => (
                                            <div
                                                key={i}
                                                className={`diff-line ${
                                                    line.type === "add" ? "diff-added" : line.type === "remove" ? "diff-removed" : ""
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
                            <div className="empty-table">Select a file to view diff</div>
                        )}
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

            {isEditingCommitMessage &&
                <CommitModal
                    repositoryId={repoId}
                    email={user?.email}
                    userId={user?.userId}
                    onClose={() => setIsEditingCommitMessage(false)}
                    onSuccess={() => handleSelectFile(null, false)} />
            }
            {isPushingChanges &&
                <PushModal
                    repositoryId={repoId}
                    onClose={() => setIsPushingChanges(false)}
                    onSuccess={() => handleSelectFile(null, false)} />
            }
            {isCheckingOut &&
                <CheckoutModal
                    repositoryId={repoId}
                    onClose={() => setIsCheckingOut(false)}
                    onSuccess={() => handleSelectFile(null, false)} />
            }
            {isCreatingPullRequest &&
                <PullRequestModal
                    repositoryId={repoId}
                    repoName={repoName}
                    onClose={() => setIsCreatingPullRequest(false)}  />
            }
            {isCreatingIssue &&
                <CreateIssueModal
                    repositoryId={repoId}
                    repoName={repoName}
                    onClose={() => setIsCreatingIssue(false)} />
            }
            {isDeletingBranch &&
                <DeleteBranchModal
                    repositoryId={repoId}
                    onClose={() => setIsDeletingBranch(false)}  />
            }
            {isExecutingShellCommands &&
                <ExecuteShellCommandsModal
                    repositoryId={repoId}
                    onClose={() => setIsExecutingShellCommands(false)}
                    onSuccess={() => handleSelectFile(null, false)} />
            }
        </div>
    );
};

export default RepositoryStatusPage;
