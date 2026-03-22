import {
    CreateMergeRequest,
    CreatePRRequest,
    GetGitLabProjectMembersRequest,
    GitBranchRequest,
    GitLabProjectMember,
    LocalGitRepository,
    RemoteHostPlatform,
} from "../../../API/ChasmaWebApiClient";
import React, {useEffect, useState} from "react";
import {branchClient, remoteClient} from "../../../managers/ApiClientManager";
import NotificationModal from "../../modals/NotificationModal";
import Checkbox from "../../Checkbox";

/** The members of the page to create issues. **/
interface RemotePullRequestPageProps {
    /** The repository to create issues for. **/
    repository : LocalGitRepository
}

/**
 * Initializes a new instance of the RemotePullRequestPage.
 * @param props The properties of the RemotePullRequestPage.
 * @constructor
 */
const RemotePullRequestPage: React.FC<RemotePullRequestPageProps> = (props: RemotePullRequestPageProps) => {
    /** Gets or sets the modal title. **/
        const [title, setTitle] = useState<string>("Create Pull Request");
    
        /** Gets or sets the error message. **/
        const [errorMessage, setErrorMessage] = useState<string | undefined>(undefined);
    
        /** Gets or sets a value indicating whether the pull request response was successful. **/
        const [successfullyCreated, setSuccessfullyCreated] = useState<boolean | undefined>(undefined);
    
        /** Gets or sets the pull request description. **/
        const [pullRequestDescription, setPullRequestDescription] = useState<string | undefined>(undefined);
    
        /** Gets or sets the pull request title. **/
        const [pullRequestTitle, setPullRequestTitle] = useState<string | undefined>(undefined);
    
        /** Gets or sets the destination branch. **/
        const [destinationBranch, setDestinationBranch] = useState<string | undefined>(undefined);
    
        /** Gets or sets the working branch name. **/
        const [workingBranchName, setWorkingBranchName] = useState<string | undefined>(undefined);
    
        /** Gets or sets the remote branches to checkout. **/
        const [branchesList, setBranchesList] = React.useState<string[] | undefined>([]);
    
        /** Gets or sets the pull request URL. **/
        const [pullRequestUrl, setPullRequestUrl] = useState<string | undefined>(undefined);

        /** Gets or sets the GitLab project members associated with the repository. **/
        const [gitLabProjectMembers, setGitLabProjectMembers] = useState<GitLabProjectMember[]>([]);
    
        /** Gets or sets the main GitLab assignee of the issue. **/
        const [mainGitLabAssignee, setMainGitLabAssignee] = useState<GitLabProjectMember | undefined>(undefined);
    
        /** Gets or sets the selected GitLab additional merge request assignees. */
        const [selectedAdditionalAssignees, setSelectedAdditionalAssignees] = useState<{rowId: string, member?: GitLabProjectMember}[]>([]);

        /** Gets or sets the selected GitLab merge request reviewers. */
        const [selectedGitLabReviewers, setSelectedGitLabReviewers] = useState<{rowId: string, member?: GitLabProjectMember}[]>([]);
        
        /** Gets or sets the project identifier. */
        const [projectId, setProjectId] = useState<number | undefined>(undefined);
        
        /** Gets or sets a value indicating whether the user is removing the source branch upon a merge. */
        const [isRemoveSourceBranch, setIsRemoveSourceBranch] = useState(false);

        /** Gets or sets a value indicating whether the user is going to squash the commits upon merge. */
        const [isSquashing, setIsSquashing] = useState(false);

        /** Gets or sets a value indicating whether the merge request allows collaboration. */
        const [isAllowingCollaboration, setIsAllowingCollaboration] = useState(false);

        /** Gets or sets the notification **/
        const [notification, setNotification] = useState<{
            title: string;
            message: string | undefined;
            isError: boolean | undefined;
            loading?: boolean;
        } | null>(null);

        /**
         * Handles the event when a user intends to create a pull request.
         */
        const handleRemotePullRequestOperation = async () => {
            setNotification({
                title: "Attempting to create pull request...",
                message: "Please wait while your request is being processed.",
                isError: false,
                loading: true
            });
            if (props.repository.hostPlatform === RemoteHostPlatform.GitHub) {
                await handleCreatePrRequest();
            }
            else if (props.repository.hostPlatform === RemoteHostPlatform.GitLab) {
                await handleCreateGitlabMergeRequest();
            }
            else {
                setNotification({
                    title: "Failed to complete operation!",
                    message: `The host platform: ${RemoteHostPlatform[props.repository.hostPlatform!]} is not supported!`,
                    isError: true,
                });
            }
        };
    
        /**
         * Handles the event when a user intends to create a pull request on GitHub.
         */
        const handleCreatePrRequest = async () => {
            setTitle("Creating pull request. May take a few moments...");
            const request = new CreatePRRequest();
            request.repositoryId = props.repository.id;
            request.pullRequestBody = pullRequestDescription;
            request.pullRequestTitle = pullRequestTitle;
            request.repositoryName = props.repository.name;
            request.destinationBranchName = destinationBranch
            request.workingBranchName = workingBranchName;
            try {
                const response = await remoteClient.createPullRequest(request);
                if (response.isErrorResponse) {
                    setTitle("Error creating Pull Request");
                    setErrorMessage(response.errorMessage);
                    setSuccessfullyCreated(false);
                    return;
                }
    
                setTitle(`Pull Request ${response.pullRequestId} successfully created at ${response.timeStamp}`);
                setErrorMessage(undefined);
                setSuccessfullyCreated(true);
                setPullRequestUrl(response.pullRequestUrl);
                setNotification(null);
            }
            catch (e) {
                console.error(e);
                setErrorMessage("Error occurred while creating Pull Request. Check error logs.");
                setTitle("Error creating Pull Request");
                setSuccessfullyCreated(false);
            }
        };

        /** Handles the event when a user wants to create a GitLab merge request. */
        const handleCreateGitlabMergeRequest = async () => {
            setTitle("Creating pull request. May take a few moments...");
            const request = new CreateMergeRequest();
            request.repositoryId = props.repository.id;
            request.sourceBranch = workingBranchName;
            request.targetBranch = destinationBranch;
            request.title = pullRequestTitle;
            request.targetProjectId = projectId;
            request.assignee = mainGitLabAssignee;
            request.additionalAssignees = selectedAdditionalAssignees.map(i => i.member).filter((m): m is GitLabProjectMember => m !== undefined);
            request.reviewers = selectedGitLabReviewers.map(i => i.member).filter((m): m is GitLabProjectMember => m !== undefined);
            request.description = pullRequestDescription;
            request.removeSourceBranch = isRemoveSourceBranch;
            request.squash = isSquashing;
            request.allowCollaboration = isAllowingCollaboration;
            try {
                const response = await remoteClient.createMergeRequest(request);
                if (response.isErrorResponse) {
                    setTitle("Error creating Merge Request");
                    setErrorMessage(response.errorMessage);
                    setSuccessfullyCreated(false);
                    setNotification({
                        title: "Failed to create merge request!",
                        message: response.errorMessage,
                        isError: true,
                    });
                    return;
                }
    
                setTitle(`Merge Request ${response.mergeRequest?.id} successfully created at ${response.mergeRequest?.timeStamp}`);
                setErrorMessage(undefined);
                setSuccessfullyCreated(true);
                setPullRequestUrl(response.mergeRequest?.url);
                setNotification(null); 
            }
            catch (e) {
                console.error(e);
                setErrorMessage("Error occurred while creating Pull Request. Check error logs.");
                setTitle("Error creating Pull Request");
                setSuccessfullyCreated(false);
                setNotification({
                    title: "Error creating merge request!",
                    message: "Investigate server logs for more information.",
                    isError: true,
                });
            }
        };
    
        /** Fetches the local and remote branches associated with this repository. **/
        async function fetchAssociatedBranches() {
            const request = new GitBranchRequest();
            request.repositoryId = props.repository.id;
            try {
                const response = await branchClient.getBranches(request);
                if (response.isErrorResponse) {
                    setBranchesList([]);
                    return;
                }
    
                setBranchesList(response.branchNames);
                if (response.branchNames && response.branchNames.length > 0) {
                    const branch = response.branchNames[0];
                    setWorkingBranchName(branch);
                    setDestinationBranch(branch);
                }

                setNotification(null);
            }
            catch (e) {
                console.error(e);
                setErrorMessage("Error occurred while fetching branches. Check console logs.");
            }
        }

    /** Fetches the GitLab project members for the specified repository. **/
    const fetchGitLabProjectMembers = async () => {
        if (props.repository.hostPlatform !== RemoteHostPlatform.GitLab) {
            return;
        }

        try {
            const request = new GetGitLabProjectMembersRequest();
            request.repositoryId = props.repository.id;
            const response = await remoteClient.getGitLabProjectMembers(request);
            if (response.isErrorResponse) {
                setNotification({
                    title: "Error fetching project members!",
                    message: response.errorMessage,
                    isError: true,
                });
                return;
            }

            if (response.projectMembers) {
                setGitLabProjectMembers(response.projectMembers);
            }

            setProjectId(response.projectId);
        }
        catch (e) {
            setNotification({
                title: "Error fetching GitLab project members.!",
                message: "Review the console logs for more information.",
                isError: true,
            });
            console.error(e);
        }
    };

    /** Resets the pull request template data. */
    const resetForm = () => {
        setPullRequestDescription("");
        setPullRequestTitle("");
        setPullRequestUrl("");
        setErrorMessage(undefined);
    };

    /**
     * Handles the change when the user selects a main assignee.
     * @param id The assignee identifier.
     */
    const handleMainAssigneeChange = (id: string) => {
        const assigneeId = Number(id);
        const assignee = gitLabProjectMembers.find(i => i.assigneeId === assigneeId);
        setMainGitLabAssignee(assignee);
    };

    /** Adds the new row to add an additional assignee for a GitLab merge request. */
    const addSelectedAdditionalAssigneeRow = () => {
        setSelectedAdditionalAssignees(prev => [
            ...prev,
            {rowId: crypto.randomUUID(), undefined}
        ])
    };

    /**
     * Handles the additional assignee change when the user selects a new user for the specified row.
     * @param rowId The row identifier.
     * @param userId The assignee identifier.
     */
    const handleSelectedAdditionalAssigneeChange = (rowId: string, userId : string) => {
        const assigneeId = Number(userId);
        const assignee = gitLabProjectMembers.find(i => i.assigneeId === assigneeId);
        if (!assignee) {
            return;
        }

        setSelectedAdditionalAssignees(prev =>
            prev.map(row =>
                row.rowId === rowId ? { ...row, member: assignee } : row
            ));
    };

    /**
     * Deletes the specified additional assignee with the row identifier.
     * @param rowId The row identifier.
     */
    const deleteAdditionalAssigneeRow = (rowId: string) => {
        const contacts = selectedAdditionalAssignees.filter(i => i.rowId !== rowId);
        setSelectedAdditionalAssignees(contacts);
    }

    /** Adds the new row to add a reviewer for a GitLab merge request. */
    const addGitLabReviewerRow = () => {
        setSelectedGitLabReviewers(prev => [
            ...prev,
            {rowId: crypto.randomUUID(), undefined}
        ])
    };

    /**
     * Handles the GitLab reviewer change when the user selects a new user for the specified row.
     * @param rowId The row identifier.
     * @param userId The assignee identifier.
     */
    const handleSelectedGitLabReviewerChange = (rowId: string, userId : string) => {
        const assigneeId = Number(userId);
        const assignee = gitLabProjectMembers.find(i => i.assigneeId === assigneeId);
        if (!assignee) {
            return;
        }

        setSelectedGitLabReviewers(prev =>
            prev.map(row =>
                row.rowId === rowId ? { ...row, member: assignee } : row
            ));
    };

    /**
     * Deletes the specified additional assignee with the row identifier.
     * @param rowId The row identifier.
     */
    const deleteGitLabReviewerRow = (rowId: string) => {
        const reviewers = selectedGitLabReviewers.filter(i => i.rowId !== rowId);
        setSelectedGitLabReviewers(reviewers);
    }

    useEffect(() => {
        fetchGitLabProjectMembers().catch(console.error);
        fetchAssociatedBranches().catch(console.error)
    }, [props.repository]);

    return (
        <>
            <div className="left-panel">
                <header className="batch-page-header">
                <h1 className="page-title">{title}</h1>
                <span><code>{workingBranchName}</code> ➜ <code>{destinationBranch}</code></span>
                {errorMessage &&
                    <p
                        className="page-description"
                        style={{color: "red"}}>
                            {errorMessage}
                    </p>
                }
                </header>
                {props.repository.hostPlatform === RemoteHostPlatform.GitLab &&
                    <>
                    <div style={{display: "flex", flexDirection: "column", gap: "8px"}}>
                        <Checkbox
                            label={"Remove source banch"}
                            onBoxChecked={setIsRemoveSourceBranch}
                            checked={isRemoveSourceBranch}
                            tooltip="Remove the source branch once it is successfully merged into the destination branch." />
                        <Checkbox
                            label={"Squash"}
                            onBoxChecked={setIsSquashing}
                            checked={isSquashing}
                            tooltip="Squash all commits in the source branch when merging into the destination branch." />
                        <Checkbox
                            label={"Allow Collaboration"}
                            onBoxChecked={setIsAllowingCollaboration}
                            checked={isAllowingCollaboration}
                            tooltip="Other people can push commits directly to your merge request's source branch." />
                    </div>
                    <br/>
                    </>
                }
                {branchesList && branchesList.length > 0 && (
                    <div>
                        <label style={{float: "left"}}>Choose working branch:</label>
                        <select value={workingBranchName}
                                onChange={(e) => setWorkingBranchName(e.target.value)}
                                className="input-field"
                        >
                            {branchesList.map((branch) => (
                                <option key={branch} value={branch}>{branch}</option>
                            ))}
                        </select>
                        <br/>
                        <label style={{float: "left"}}>Choose destination branch to merge into:</label>
                        <select value={destinationBranch}
                                onChange={(e) => setDestinationBranch(e.target.value)}
                                className="input-field"
                        >
                            {branchesList.map((branch) => (
                                <option key={branch} value={branch}>{branch}</option>
                            ))}
                        </select>
                    </div>
                )}
                <input
                    type="text"
                    className="input-field"
                    placeholder="Title"
                    value={pullRequestTitle}
                    onChange={(e) => setPullRequestTitle(e.target.value)}
                    required
                />
                <textarea className="textarea-field"
                          placeholder="Description"
                          value={pullRequestDescription}
                          onChange={(e) => setPullRequestDescription(e.target.value)} />
                {props.repository.hostPlatform === RemoteHostPlatform.GitLab && !successfullyCreated &&
                    <>
                        <select
                            className="repo-dropdown input-field"
                            onChange={(e) => handleMainAssigneeChange(e.target.value)}>
                            <option value="">Select Main Assignee</option>
                            {gitLabProjectMembers.map(member => (
                                <option
                                    key={member.assigneeId}
                                    value={member.assigneeId}>
                                        {member.userName} - {member.fullName}
                                </option>
                            ))}
                        </select>
                        <hr className="separator" />
                        <div className="repository-actions">
                            <h3>Additonal Assignees</h3>
                            <button
                                className="add-button modern-add"
                                type="button"
                                onClick={addSelectedAdditionalAssigneeRow}>
                                    +
                            </button>
                        </div>
                        {selectedAdditionalAssignees.map(assigneeRow => (
                            <React.Fragment key={assigneeRow.rowId}>
                                <div key={assigneeRow.rowId} className="command-row modern-input-row">
                                    <select
                                        className="repo-dropdown input-field"
                                        value={assigneeRow.member?.assigneeId ?? ""}
                                        onChange={(e) => handleSelectedAdditionalAssigneeChange(assigneeRow.rowId, e.target.value)}>
                                        <option value="">Select Assignee</option>
                                        {gitLabProjectMembers.map(member => (
                                            <option
                                                key={member.assigneeId}
                                                value={member.assigneeId}>
                                                    {member.userName} - {member.fullName}
                                            </option>
                                        ))}
                                    </select>
                                    <button
                                        className="remove-button modern-remove"
                                        title="Remove shell command"
                                        onClick={() => deleteAdditionalAssigneeRow(assigneeRow.rowId)}
                                    >
                                        −
                                    </button>
                                </div>
                            </React.Fragment>
                        ))}
                        <hr className="separator" />
                        <div className="repository-actions">
                            <h3>Reviewers</h3>
                            <button
                                className="add-button modern-add"
                                type="button"
                                onClick={addGitLabReviewerRow}>
                                    +
                            </button>
                        </div>
                        {selectedGitLabReviewers.map(reviewerRow => (
                            <React.Fragment key={reviewerRow.rowId}>
                                <div key={reviewerRow.rowId} className="command-row modern-input-row">
                                    <select
                                        className="repo-dropdown input-field"
                                        value={reviewerRow.member?.assigneeId ?? ""}
                                        onChange={(e) => handleSelectedGitLabReviewerChange(reviewerRow.rowId, e.target.value)}>
                                        <option value="">Select Reviewer</option>
                                        {gitLabProjectMembers.map(member => (
                                            <option
                                                key={member.assigneeId}
                                                value={member.assigneeId}>
                                                    {member.userName} - {member.fullName}
                                            </option>
                                        ))}
                                    </select>
                                    <button
                                        className="remove-button modern-remove"
                                        title="Remove shell command"
                                        onClick={() => deleteGitLabReviewerRow(reviewerRow.rowId)}
                                    >
                                        −
                                    </button>
                                </div>
                            </React.Fragment>
                        ))}
                    </>
                }
                <br/>
                {successfullyCreated && pullRequestUrl &&
                    <>
                        <div className="panel-card">
                            <h1
                                className="page-title"
                                style={{color : "lightgreen"}}>
                                    Newly Created Pull Request URL
                            </h1>
                            <p className="page-description">Open your {RemoteHostPlatform[props.repository.hostPlatform!]} pull request and see the details in action 🚀</p>
                            <div
                                className="input-field"
                                onClick={() => window.open(`${pullRequestUrl}`, "_blank")}
                                style={{cursor: "pointer"}}>
                                    {pullRequestUrl}
                            </div>
                        </div>
                    </>
                }
                <br/>
                <div className="modal-actions">
                    <button className="modal-button primary"
                            hidden={successfullyCreated}
                            onClick={handleRemotePullRequestOperation}
                    >
                        Create 
                    </button>
                    <button className="modal-button secondary"
                            onClick={resetForm}
                    >
                        Clear
                    </button>
                </div>
                {notification && (
                    <NotificationModal
                        title={notification.title}
                        message={notification.message}
                        isError={notification.isError}
                        loading={notification.loading}
                        onClose={() => setNotification(null)} />
                )}
            </div>
        </>
    )
}

export default RemotePullRequestPage;