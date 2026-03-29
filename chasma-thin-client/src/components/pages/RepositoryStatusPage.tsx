import React, { useEffect, useMemo, useState } from "react";
import { useNavigate, useParams } from "react-router-dom";
import {
    ApplyBulkStagingActionRequest,
    ApplyStagingActionRequest,
    GitDiffRequest,
    GitPullRequest,
    GitRestoreRequest,
    GitRmRequest,
    GitStatusRequest,
    PullSimulationEntry,
    RemoteHostPlatform,
    RemotePullRequest,
    RepositoryStatusElement,
    SimulatedAddBranchResult,
    SimulatedGitPullResult,
    SimulatedMergeResult,
    SimulateGitPullRequest,
} from "../../API/ChasmaWebApiClient";
import CommitModal from "../modals/CommitModal";
import PushModal from "../modals/PushModal";
import CheckoutModal from "../modals/CheckoutModal";
import DeleteBranchModal from "../modals/DeleteBranchModal";
import {useCacheStore} from "../../managers/CacheManager";
import {capitalizeFirst} from "../../stringHelperUtil";
import MergeModal from "../modals/MergeModal";
import RepositoryStashesPage from "./statusComponents/RepositoryStashesPage";
import {parseUnifiedDiff} from "../../managers/DiffViewerManager";
import AddStashModal from "../modals/AddStashModal";
import AddBranchModal from "../modals/AddBranchModal";
import ResetModal from "../modals/ResetModal";
import {configClient, dryRunClient, statusClient} from "../../managers/ApiClientManager";
import Checkbox from "../Checkbox";
import "../../styles/pages/batchOperationsPage.css"
import RemoteIssuesPage from "./remote/RemoteIssuesPage";
import RemotePullRequestPage from "./remote/RemotePullRequestPage";
import ExecuteShellCommandsPage from "./statusComponents/ExecuteShellCommandsPage";
import { handleApiError } from "../../managers/TransactionHandlerManager";
import { Virtuoso } from "react-virtuoso";

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

    /** Gets or sets a flag indicating whether the user is editing the commit message. **/
    const [isEditingCommitMessage, setIsEditingCommitMessage] = useState(false);

    /** Gets or sets a flag indicating whether the user is pushing changes. **/
    const [isPushingChanges, setIsPushingChanges] = useState(false);

    /** Gets or sets a flag indicating whether the user is checking out changes. **/
    const [isCheckingOut, setIsCheckingOut] = useState(false);

    /** Gets or sets a flag indicating whether the user is deleting a branch. **/
    const [isDeletingBranch, setIsDeletingBranch] = useState(false);

    /** Gets or sets a flag indicating whether the user is merging branches. **/
    const [isMergingBranch, setIsMergingBranch] = useState(false);

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

    /** Gets or sets a value indicating whether the user is adding a stash. **/
    const [isAddingStash, setIsAddingStash] = useState(false);

    /** Gets or sets the open pull request associated with the current branch. **/
    const [openPullRequests, setOpenPullRequests] = useState<RemotePullRequest[] | undefined>(undefined);

    /** Gets or sets a value indicating whether the user is adding a new branch. **/
    const [isAddingBranch, setIsAddingBranch] = useState(false);

    /** Gets or sets a value indicating whether the user is resetting changes. **/
    const [isResettingChanges, setIsResettingChanges] = useState(false);

    /** Gets or sets a value indicating whether the user is in safe mode. **/
    const [isSafeMode, setIsSafeMode] = useState(false);

    /** Gets or sets the simulated pull results. **/
    const [simulatedPullResults, setSimulatedPullResults] = useState<SimulatedGitPullResult[]>([]);

    /** Gets or sets the simulated add branch results. **/
    const [simulatedAddBranchResults, setSimulatedAddBranchResults] = useState<SimulatedAddBranchResult[]>([]);

    /** Gets or sets the simulated merge results. **/
    const [simulatedMergeResults, setSimulatedMergeResults] = useState<SimulatedMergeResult[]>([]);

    /** Gets or sets a value indicating whether the staging action request is ready to be sent. */
    const [disableStageActionSending, setDisableStageActionSending] = useState(false);

    /** Gets or sets a value indicating whether the git diff request is ready to be sent. */
    const [disableDiffRequestSending, setDisableDiffRequestSending] = useState(false);

    /** Gets or sets a value indicating whether the bulk staging request is ready to be sent. */
    const [disableBulkStagingRequestSending, setDisableBulkStagingRequestSending] = useState(false);

    /** Gets or sets a value indicating whether the git restore request is ready to be sent. */
    const [disableRestoreRequestSending, setDisableRestoreRequestSending] = useState(false);

    /** Gets or sets a value indicating whether the git rm request is ready to be sent. */
    const [disableDeleteFileSending, setDisableDeleteFileSending] = useState(false);

    /** The logged-in user. **/
    const user = useCacheStore((state) => state.user);

    /** Gets or sets the selected files for staging. */
    const [selectedFiles, setSelectedFiles] = useState<Set<string>>(new Set());

    /** Gets or sets the last selected index for staging files. */
    const [lastSelectedIndex, setLastSelectedIndex] = useState<number | null>(null);

    /** Gets or sets the time that this repository was last updated. */
    const [lastUpdated, setLastUpdated] = useState<string | undefined>(undefined);

    /** Gets or sets the list of staged files. */
    const stagedList = statusElements?.filter(e => e.isStaged) || [];

    /** Gets or sets the list of unstaged files. */
    const unstagedList = statusElements?.filter(e => !e.isStaged) || [];

    /** Gets the selected repository instance. **/
    const selectedRepo = useCacheStore((state) => state.repositories.find(i => i.id === repoId));

    /** Sets the notification modal. */
   const setNotification = useCacheStore(state => state.setNotification);

   /** Gets or sets the value indicating whether the parsed diff is too big. */
   const [parsedDiffTooBig, setParsedDiffTooBig] = useState(false);

    /** Gets or sets the context menu. **/
    const [contextMenu, setContextMenu] = useState<{
        mouseX: number;
        mouseY: number;
        statusElement: RepositoryStatusElement;
    } | null>(null);

    /** Gets or sets the active tab that the user has selected. **/
    const [activeTab, setActiveTab] = useState<string>("home");

    /** Handles the event when the user selects a tab. **/
    const handleTabClick = (tab: string) => {
        setActiveTab(tab);
    };

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
            request.userId = user?.userId
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
            setLastUpdated(response.lastUpdated);
        } catch (e) {
            const errorNotification = handleApiError(e, navigate, "Failed to perform 'git status' operation!", "An internal server error has occurred. Review logs.");
            setNotification(errorNotification);
        }
    }

    /**
     * Handles the request to apply the staging action to the selected file.
     * @param selectedFile The selected file.
     */
    async function handleApplyStagingActionRequest(selectedFile: RepositoryStatusElement) {
        if (disableStageActionSending) {
            return;
        }

        setDisableStageActionSending(true);
        const stagingAction = !selectedFile.isStaged
        const request = new ApplyStagingActionRequest();
        request.repoKey = selectedFile.repositoryId;
        request.fileName = selectedFile.filePath;
        request.isStaging = stagingAction;
        request.userId = user?.userId;

        try {
            const response = await statusClient.applyStagingAction(request);
            const action = stagingAction ? "stage" : "unstage";
            if (response.isErrorResponse) {
                setNotification({
                    title: `Could not ${action}!`,
                    message: response.errorMessage,
                    isError: true,
                });
                setDisableStageActionSending(false);
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

            setDisableStageActionSending(false);
        } catch (e) {
            const errorNotification = handleApiError(e, navigate, `Failed to perform '${stagingAction ? "stage" : "unstage"}' operation!`, "An internal server error has occurred. Review logs.");
            setNotification(errorNotification);
            setDisableStageActionSending(false);
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
     * Handles the event when the user wants to pull changes.
     */
    const handlePullRequestOperation = () => {
        if (isSafeMode) {
            handleGitPullRequestDryRun();
            return;
        }

        handlePullRequest();
    }

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
            const errorNotification = handleApiError(e, navigate, "Could not pull changes", "An internal server error has occurred. Review logs.");
            setNotification(errorNotification);
        }
    };

    /**
     * Handles the event when the user wants to simulate pulling changes.
     */
    const handleGitPullRequestDryRun = async () => {
        setNotification({
            title: "Performing pull simulation...",
            message: "Please wait while your dry run is being processed..",
            isError: false,
            loading: true
        });

        const request = new SimulateGitPullRequest();
        const entry = new PullSimulationEntry();
        entry.repositoryId = repoId;
        request.entries = [entry];
        try {
            const response = await dryRunClient.simulateGitPull(request);
            if (response.isErrorResponse) {
                setNotification({
                    title: "Failed to perform pull simulation!",
                    message: response.errorMessage,
                    isError: true,
                });
                return;
            }

            if (!response.pullResults) {
                setNotification({
                    title: "Failed to perform pull simulation!",
                    message: "There were no simulation results. Check server logs for more information.",
                    isError: true,
                });
                return;
            }

            setNotification({
                title: "Successfully performed pull simulation!",
                message: "Close to dismiss.",
                isError: false,
            });

            setSimulatedPullResults(response.pullResults);
        } catch (e) {
            const errorNotification = handleApiError(e, navigate, "Could not simulate pull simulation!", "An internal server error has occurred. Review logs.");
            setNotification(errorNotification);
        }
    }

    /**
     * Handles the event when the user wants to get the diff of a file.
     * @param file The file to be diffed.
     * @param isStaged Flag indicating whether the file is in the staging area.
     */
    async function handleGetGitDiffRequest(file: RepositoryStatusElement | null, isStaged: boolean | undefined) {
        if (!repoId || file === null || disableDiffRequestSending) return;

        setDisableDiffRequestSending(true);
        const request = new GitDiffRequest();
        request.repositoryId = repoId;
        request.filePath = file.filePath;
        request.isStaged = isStaged;
        try {
            const response = await statusClient.getGitDiff(request);
            if (response.isErrorResponse) {
                console.error(response.errorMessage);
                setRawDiff("");
                setDisableDiffRequestSending(false);
                return;
            }

            setDisableDiffRequestSending(false);
            setRawDiff(response.diffContent!);
        } catch (e) {
            setDisableDiffRequestSending(false);
            const errorNotification = handleApiError(e, navigate, "Failed to get diff!", "An internal server error has occurred. Open terminal and run 'git diff' on the selected file.");
            setNotification(errorNotification);
        }
    }

    /**
     * Handles the event when the user wants to handle bulk staging action.
     * @param isStaging Flag indicating whether the user is staging/unstaging the files.
     */
    const handleBulkStagingActionRequest = async (isStaging: boolean) => {
        if (disableBulkStagingRequestSending) return;

        setDisableBulkStagingRequestSending(true);
        const files = Array.from(selectedFiles);
        if (files.length === 0) {
            setDisableBulkStagingRequestSending(false);
            return;
        }

        try {
            const request = new ApplyBulkStagingActionRequest();
            request.repositoryId = repoId;
            request.fileNames = files;
            request.isStaging = isStaging;
            const response = await statusClient.applyBulkStagingAction(request);
            if (response.isErrorResponse) {
                setNotification({
                    title: "Bulk staging operation failed!",
                    message: response.errorMessage,
                    isError: true,
                });
                setDisableBulkStagingRequestSending(false);
                return;
            }

            setDisableBulkStagingRequestSending(false);
            setStatusElements(response.statusElements);
            setSelectedFiles(new Set());
            setSelectedFile(null);
        } catch (e) {
            setDisableBulkStagingRequestSending(false);
            const errorNotification = handleApiError(e, navigate, "Error performing bulk staging operation!", "Check console logs for more information.");
            setNotification(errorNotification);
        }
    }

    /**
     * Restores the selected file to its original revision.
     * @param selectedFile The file to restore.
     */
    const handleGitRestoreRequest = async (selectedFile: RepositoryStatusElement) => {
        if (disableRestoreRequestSending) return;

        setDisableRestoreRequestSending(true);
        try {
            const request = new GitRestoreRequest();
            request.selectedFile = selectedFile;
            const response = await statusClient.restoreFile(request);
            if (response.isErrorResponse) {
                setNotification({
                    title: "'git restore' operation failed!",
                    message: response.errorMessage,
                    isError: true,
                });
                setDisableRestoreRequestSending(false);
                return;
            }

            setDisableRestoreRequestSending(false);
            setSelectedFile(null);
        } catch (error) {
            setDisableRestoreRequestSending(false);
            const errorNotification = handleApiError(error, navigate, "Error performing 'git restore'!", "Check console logs for more information.");
            setNotification(errorNotification);
        }
    };

    /**
     * Deletes the file from the filesystem.
     * @param selectedFile The file to delete.
     */
    const handleGitRmRequest = async (selectedFile: RepositoryStatusElement) => {
        if (disableDeleteFileSending) return;

        setDisableDeleteFileSending(true);
        try {
            const request = new GitRmRequest();
            request.selectedFile = selectedFile;
            const response = await configClient.removeFile(request);
            if (response.isErrorResponse) {
                setNotification({
                    title: "'git rm' operation failed!",
                    message: response.errorMessage,
                    isError: true,
                });
                setDisableDeleteFileSending(false);
                return;
            }

            setDisableDeleteFileSending(false);
            setSelectedFile(null);
        } catch (error) {
            setDisableDeleteFileSending(false);
            const errorNotification = handleApiError(error, navigate, "Error performing 'git rm'!", "Check console logs for more information.");
            setNotification(errorNotification);
        }
    };

    /**
     * Handles the event when the user clicks a file from the unstaged/staged changes.
     * @param file The file to be selected.
     * @param isStaged Flag indicating whether the file is in the staging area.
     */
    const handleSelectFile = (file: RepositoryStatusElement | null, isStaged: boolean) => {
        setSelectedFile(file);
        handleGetGitDiffRequest(file, isStaged);
    };

    /** Defines the maximum raw diff size. 2MB. */
    const MAX_RAW_DIFF_SIZE = 2_000_000; // 2MB, adjust as needed

    /** The parsed unified diff. */
    const parsedDiff = useMemo(() => {
        if (!rawDiff) {
            return [];
        }

        if (rawDiff.length > MAX_RAW_DIFF_SIZE) {
            console.warn("Diff too large, truncating for performance.");
            return parseUnifiedDiff(rawDiff.slice(0, MAX_RAW_DIFF_SIZE));
        }

        return parseUnifiedDiff(rawDiff);
    }, [rawDiff]);

    /** Gets the maximum parsed lines to render. */
    const MAX_PARSED_LINES = 5000;

    /** Gets the safely parsed lines to display to prevent freezing. */
    const safeParsedDiff = useMemo(() => {
        if (parsedDiff.length > MAX_PARSED_LINES) {
            console.warn("Parsed diff too long, showing only first 5000 lines.");
            setParsedDiffTooBig(true);
            return parsedDiff.slice(0, MAX_PARSED_LINES);
        }

        setParsedDiffTooBig(false);
        return parsedDiff;
    }, [parsedDiff]);

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
            return "Nothing to push";
        }

        return `Ready with ${commitsAhead} commit${commitsAhead && commitsAhead > 1 ? "s" : ""}`
    }

    /**
     * Cleans up the simulation results from the console.
     */
    const cleanUpSimulationResults = () => {
        setSimulatedPullResults([]);
        setSimulatedAddBranchResults([]);
        setSimulatedMergeResults([]);
    }

    /**
     * Handles the event when the user wants to bulk stage/unstage files.
     * @param e The mouse event.
     * @param file The file to stage.
     * @param index The selected file index.
     * @param list The list of files in the index.
     * @param isStaged Flag indicating whether the user is staging/unstaging the file.
     */
    const handleMultiSelect = (e: React.MouseEvent, file: RepositoryStatusElement, index: number, list: RepositoryStatusElement[], isStaged: boolean) => {
        const newSelection = new Set(selectedFiles);
        if (e.shiftKey && lastSelectedIndex !== null) {
            // SHIFT → range select
            const start = Math.min(lastSelectedIndex, index);
            const end = Math.max(lastSelectedIndex, index);
            for (let i = start; i <= end; i++) {
                newSelection.add(list[i].filePath!);
            }
        }
        else if (e.ctrlKey || e.metaKey) {
            // CTRL / CMD → toggle
            if (newSelection.has(file.filePath!)) {
                newSelection.delete(file.filePath!);
            } else {
                newSelection.add(file.filePath!);
            }

            setLastSelectedIndex(index);
        }
        else {
            // Normal click → preserve existing behavior + selection
            newSelection.clear();
            newSelection.add(file.filePath!);
            setLastSelectedIndex(index);
            setSelectedFile(file);
            handleGetGitDiffRequest(file, isStaged);
        }

        setSelectedFiles(newSelection);
    };

    useEffect(() => {
        const closeMenu = () => setContextMenu(null);
        window.addEventListener("click", closeMenu);
        return () => window.removeEventListener("click", closeMenu);
    }, []);

    useEffect(() => {
        const handler = (e: KeyboardEvent) => {
            if (e.key === "Escape") {
                setSelectedFiles(new Set());
            }
        };

        window.addEventListener("keydown", handler);
        return () => window.removeEventListener("keydown", handler);
    }, []);

    return (
        <div className="dashboard-container">
            <aside className="sidebar">
                <div className="sidebar-profile">
                    <span className="profile-icon">📁</span>
                    <span>{repoName}</span>
                </div>
                <div className="tab" style={{ marginTop: "20px" }} onClick={() => navigate("/home")}>Dashboard 👈</div>
                <div
                    className={`tab ${activeTab === "home" ? "active" : ""}`}
                    onClick={() => handleTabClick("home")}
                >
                    Home 🏠
                </div>
                <div
                    className="tab"
                     onClick={() => {
                         cleanUpSimulationResults();
                         handlePullRequestOperation();
                     }}
                >
                    Pull ⬇️
                </div>
                {!isSafeMode &&
                    <>
                    <div className="tab" onClick={() => setIsEditingCommitMessage(true)}>Commit 📌</div>
                    <div className="tab" onClick={() => setIsPushingChanges(true)}>Push ⬆️</div>
                    <div className="tab" onClick={() => setIsResettingChanges(true)}>Reset ⏮️</div>
                        <div
                            className={`tab ${activeTab === "stashes" ? "active" : ""}`}
                            onClick={() => handleTabClick("stashes")}
                        >
                            Stashes🗄️
                        </div>
                        <div className="tab" style={{ marginTop: "20px" }} onClick={() => setIsCheckingOut(true)}>Checkout Branch🌿</div>
                    </>
                }
                <div className="tab" onClick={() => setIsAddingBranch(true)}>Add Branch ➕</div>
                <div className="tab" onClick={() => setIsMergingBranch(true)}>Merge 🔀</div>
                {!isSafeMode &&
                    <>
                        <div className="tab" onClick={() => setIsDeletingBranch(true)}>Delete Branch 🗑️</div>
                        {user?.permissions
                            && user.permissions.isUsingGitHubApi
                            && selectedRepo?.hostPlatform === RemoteHostPlatform.GitHub &&
                            <div
                                className={`tab ${activeTab === "pullRequest" ? "active" : ""}`}
                                onClick={() => handleTabClick("pullRequest")}
                                style={{ marginTop: "20px" }}
                            >
                                Create Pull Request📥
                            </div>
                        }
                        {user?.permissions
                            && user.permissions.isUsingGitLabApi
                            && selectedRepo?.hostPlatform === RemoteHostPlatform.GitLab &&
                            <div
                                className={`tab ${activeTab === "pullRequest" ? "active" : ""}`}
                                onClick={() => handleTabClick("pullRequest")}
                                style={{ marginTop: "20px" }}
                            >
                                Create Merge Request📥
                            </div>
                        }
                        {user?.permissions
                            && user.permissions.isUsingGitHubApi
                            && selectedRepo?.hostPlatform === RemoteHostPlatform.GitHub &&
                            <div
                                className={`tab ${activeTab === "issues" ? "active" : ""}`}
                                onClick={() => handleTabClick("issues")}>
                                    Create GitHub Issue🐛
                            </div>
                        }
                        {user?.permissions
                            && user.permissions.isUsingGitLabApi
                            && selectedRepo?.hostPlatform === RemoteHostPlatform.GitLab &&
                            <div
                                className={`tab ${activeTab === "issues" ? "active" : ""}`}
                                onClick={() => handleTabClick("issues")}>
                                    Create GitLab Issue🐛
                            </div>
                        }
                        {user?.permissions
                            && user.permissions.isUsingBitbucketApi
                            && selectedRepo?.hostPlatform === RemoteHostPlatform.Bitbucket &&
                            <div
                                className={`tab ${activeTab === "issues" ? "active" : ""}`}
                                onClick={() => handleTabClick("issues")}>
                                    Create Bitbucket Task🐛
                            </div>
                        }
                        <div
                            className={`tab ${activeTab === "shell" ? "active" : ""}`}
                            style={{ marginTop: "20px" }}
                            onClick={() => handleTabClick("shell")}>
                                Custom Shell Commands🖥️
                        </div>
                    </>
                }
            </aside>

            {activeTab === "home" && (
                <div className="content">
                    <div className="main-layout">
                        {/* Left side: Repo summary + staged/unstaged */}
                        <div className="left-panel">
                            <div className="panel-card">
                                <div className="panel-header">
                                    <h2 className="page-description">Repository Summary</h2>
                                    <Checkbox
                                        label={"Safe Mode"}
                                        onBoxChecked={setIsSafeMode}
                                        tooltip={"When safe mode is enabled, no actual git execution will be performed. It will be all simulated."}
                                    />
                                </div>
                                <div className="repo-summary" onClick={handleNavigateToBranchUrl}>
                                    <div className="repo-summary-item">
                                        <span className="repo-summary-label">Branch:</span>
                                        <span className="repo-summary-value">{branchName}</span>
                                    </div>
                                    <div className="repo-summary-item">
                                        <span className="repo-summary-label">Last Updated:</span>
                                        <span className="repo-summary-value">{lastUpdated}</span>
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
                            {selectedFiles.size > 1 && (
                                        <div className="bulk-actions">
                                            <button
                                                className="stage-button stage"
                                                onClick={() => handleBulkStagingActionRequest(true)}
                                            >
                                                Stage Selected ({selectedFiles.size})
                                            </button>
                                            <button
                                                className="stage-button unstage"
                                                onClick={() => handleBulkStagingActionRequest(false)}
                                            >
                                                Unstage Selected ({selectedFiles.size})
                                            </button>
                                        </div>
                                    )}
                            {!isSafeMode &&
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
                                            <tbody>
                                            {statusElements?.filter(e => e.isStaged).map((element, index) => (
                                                <tr
                                                    key={index}
                                                    className={`
                                                        ${selectedFile?.filePath === element.filePath ? "selected" : ""}
                                                        ${selectedFiles.has(element.filePath!) ? "multi-selected" : ""}
                                                    `}>
                                                    <td
                                                        onClick={(e) => handleMultiSelect(e, element, index, stagedList, true)}
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
                                            ))}
                                            </tbody>
                                        </table>
                                    ) : <div className="empty-table">No staged changes</div>}
                                </div>
                            }

                            {!isSafeMode &&
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
                                            <tbody>
                                            {statusElements?.filter(e => !e.isStaged).map((element, index) => (
                                                <tr
                                                    key={index}
                                                     className={`
                                                        ${selectedFile?.filePath === element.filePath ? "selected" : ""}
                                                        ${selectedFiles.has(element.filePath!) ? "multi-selected" : ""}
                                                    `}>
                                                    <td
                                                        onClick={(e) => handleMultiSelect(e, element, index, unstagedList, false)}
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
                                            ))}
                                            </tbody>
                                        </table>
                                    ) : <div className="empty-table">No unstaged changes</div>}
                                </div>
                            }
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
                                    <li onClick={() => handleApplyStagingActionRequest(contextMenu.statusElement)}>
                                        {contextMenu.statusElement && contextMenu.statusElement.isStaged ? "Unstage" : "Stage"}
                                    </li>
                                    <li onClick={() => handleGitRestoreRequest(contextMenu?.statusElement)}>
                                        Restore
                                    </li>
                                    <li onClick={() => handleGitRmRequest(contextMenu.statusElement)}>
                                        Delete
                                    </li>
                                    <li onClick={() => setIsAddingStash(true)}>
                                        View Stash Options
                                    </li>
                                </ul>
                            </div>
                        )}

                        {!isSafeMode &&
                            <>
                                {/* Right side: Diff viewer */}
                                <div className="right-panel">
                                    {parsedDiffTooBig && <p style={{textAlign: "center", color: "yellow"}}>Parsed diff too long, showing only first 5000 lines.</p>}
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
                                                    <div className="diff-panel-header">
                                                        Unified Diff: {selectedFile.filePath}
                                                    </div>

                                                    <Virtuoso
                                                        style={{ height: "600px" }} // or make dynamic
                                                        totalCount={safeParsedDiff.length}
                                                        itemContent={(index: number) => {
                                                            const line = safeParsedDiff[index];

                                                            return (
                                                                <div
                                                                    className={`diff-line ${
                                                                        line.type === "add"
                                                                            ? "diff-added"
                                                                            : line.type === "remove"
                                                                            ? "diff-removed"
                                                                            : ""
                                                                    }`}
                                                                >
                                                                    <span className="diff-line-number">
                                                                        {line.oldLineNumber ?? ""}
                                                                    </span>
                                                                    <span className="diff-line-number">
                                                                        {line.newLineNumber ?? ""}
                                                                    </span>
                                                                    <span className="diff-code">{line.content}</span>
                                                                </div>
                                                            );
                                                        }}
                                                    />
                                                </div>
                                            )}
                                            {isSplitView && (
                                            <div style={{ display: "contents", gap: "10px" }}>
                                                <div className="diff-panel" style={{ flex: 1 }}>
                                                    <div className="diff-panel-header">
                                                        Original: {selectedFile.filePath}
                                                    </div>
                                                    <Virtuoso
                                                        style={{ height: "600px" }}
                                                        totalCount={safeParsedDiff.length}
                                                        itemContent={(index: number) => {
                                                            const line = safeParsedDiff[index];
                                                            return (
                                                                <div
                                                                    className={`diff-line ${
                                                                        line.type === "remove" ? "diff-removed" : ""
                                                                    }`}
                                                                >
                                                                    <span className="diff-line-number">
                                                                        {line.oldLineNumber ?? ""}
                                                                    </span>
                                                                    <span className="diff-code">
                                                                        {line.type === "add" ? "" : line.content}
                                                                    </span>
                                                                </div>
                                                            );
                                                        }}
                                                    />
                                                </div>
                                                <div className="diff-panel" style={{ flex: 1 }}>
                                                    <div className="diff-panel-header">
                                                        Modified: {selectedFile.filePath}
                                                    </div>

                                                    <Virtuoso
                                                        style={{ height: "600px" }}
                                                        totalCount={safeParsedDiff.length}
                                                        itemContent={(index: number) => {
                                                            const line = safeParsedDiff[index];
                                                            return (
                                                                <div
                                                                    className={`diff-line ${
                                                                        line.type === "add" ? "diff-added" : ""
                                                                    }`}
                                                                >
                                                                    <span className="diff-line-number">
                                                                        {line.newLineNumber ?? ""}
                                                                    </span>
                                                                    <span className="diff-code">
                                                                        {line.type === "remove" ? "" : line.content}
                                                                    </span>
                                                                </div>
                                                            );
                                                        }}
                                                    />
                                                </div>
                                            </div>
                                        )}
                                        </div>
                                    ) : (
                                        <div className="empty-table">Select a file to view diff</div>
                                    )}
                                </div>
                            </>
                        }

                        {/*The simulated dry run section*/}
                        {isSafeMode &&
                            <div className="right-panel">
                                <section className="output-section">
                                        {simulatedPullResults.length > 0 && (
                                            <div className="output-header">
                                                <h3>Pull Dry Run Results</h3>
                                                <button
                                                    className="clear-output-button"
                                                    onClick={cleanUpSimulationResults}
                                                >
                                                    Clear Output
                                                </button>
                                            </div>
                                        )}
                                    {simulatedAddBranchResults.length > 0 && (
                                        <div className="output-header">
                                            <h3>Add Branch Dry Run Results</h3>
                                            <button
                                                className="clear-output-button"
                                                onClick={cleanUpSimulationResults}
                                            >
                                                Clear Output
                                            </button>
                                        </div>
                                    )}
                                    {simulatedMergeResults.length > 0 && (
                                        <div className="output-header">
                                            <h3>Merge Dry Run Results</h3>
                                            <button
                                                className="clear-output-button"
                                                onClick={cleanUpSimulationResults}
                                            >
                                                Clear Output
                                            </button>
                                        </div>
                                    )}
                                    <div className="output-window">
                                        {
                                            simulatedPullResults.length === 0 &&
                                            simulatedAddBranchResults.length === 0 &&
                                            simulatedMergeResults.length === 0 &&
                                            <p className="no-output-text">No results to report.</p>
                                        }
                                        {simulatedPullResults.map((result, index) => (
                                            <div
                                                key={index}
                                                className={`output-entry ${result.isSuccessful ? "success" : "failure"}`}
                                            >
                                                <div className="output-header-row">
                                                    <strong>{result.isSuccessful ? "Safe to pull!": "'git pull' would fail!"}</strong>
                                                    <span className="status-icon" />
                                                </div>
                                                {result.commitsToPull?.map((entry, i) => (
                                                    <span
                                                        key={i}
                                                        className="output-command"
                                                    >
                                                        &gt; {entry.commitHash} - {entry.message}
                                                    </span>
                                                ))}
                                                <span className="output-stdout">{result.errorMessage}</span>
                                            </div>
                                        ))}
                                        {simulatedAddBranchResults.map((result, index) => (
                                            <div
                                                key={index}
                                                className={`output-entry ${result.isSuccessful ? "success" : "failure"}`}
                                            >
                                                <div className="output-header-row">
                                                    <strong>{result.isSuccessful ? "Safe to add branch!": "Add branch operation would fail!"}</strong>
                                                    <span className="status-icon" />
                                                </div>
                                                <span className="output-command">&gt; {result.infoMessage ? result.infoMessage : "Branch Naming Conflict"}</span>
                                                <span className="output-stdout">{result.errorMessage}</span>
                                            </div>
                                        ))}
                                        {simulatedMergeResults.map((result, index) => (
                                            <div
                                                key={index}
                                                className={`output-entry ${result.isSuccessful ? "success" : "failure"}`}
                                            >
                                                <div className="output-header-row">
                                                    <strong>{result.mergeStatus}</strong>
                                                    <span className="status-icon" />
                                                </div>
                                                <span className="output-stdout">{result.errorMessage}</span>
                                            </div>
                                        ))}
                                    </div>
                                </section>
                            </div>
                        }
                    </div>
                </div>
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
            {activeTab === "issues" && selectedRepo && (
                <div
                    className="panel-card"
                    style={{width: "100%"}}
                >
                    <RemoteIssuesPage repository={selectedRepo} />
                </div>
            )}
            {activeTab === "pullRequest" && selectedRepo && (
                <div
                    className="panel-card"
                    style={{width: "100%"}}
                >
                    <RemotePullRequestPage repository={selectedRepo} />
                </div>
            )}
            {isDeletingBranch &&
                <DeleteBranchModal
                    repositoryId={repoId}
                    onClose={() => setIsDeletingBranch(false)}  />
            }
            {activeTab === "shell" && (
                <div
                    className="panel-card"
                    style={{width: "100%"}}
                >
                    <ExecuteShellCommandsPage repositoryId={repoId} />
                </div>
            )}
            {isMergingBranch &&
                <MergeModal
                    onClose={() => setIsMergingBranch(false)}
                    repositoryId={repoId}
                    userId={user?.userId}
                    isSafeMode={isSafeMode}
                    onSuccess={results => {
                        cleanUpSimulationResults();
                        setSimulatedMergeResults(results);
                    }}
                />
            }
            {isAddingStash &&
                <AddStashModal
                    repositoryId={repoId}
                    onClose={() => setIsAddingStash(false)} />
            }
            {isAddingBranch &&
            <AddBranchModal
                repositoryId={repoId}
                userId={user?.userId}
                onClose={() => setIsAddingBranch(false)}
                isSafeMode={isSafeMode}
                onSuccess={results => {
                    cleanUpSimulationResults();
                    setSimulatedAddBranchResults(results);
                }}
            />
            }
            {activeTab === "stashes" && (
                <div
                    className="panel-card"
                    style={{width: "100%"}}
                >
                    <RepositoryStashesPage repositoryId={repoId} />
                </div>
            )}
            {isResettingChanges &&
            <ResetModal
                repositoryId={repoId}
                onClose={() => setIsResettingChanges(false)} />
            }
        </div>
    );
};

export default RepositoryStatusPage;
