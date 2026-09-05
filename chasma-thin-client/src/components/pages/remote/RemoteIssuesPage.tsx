import {
    CreateGitHubIssueRequest,
    CreateGitLabIssueRequest,
    RemoteProjectMember,
    LocalGitRepository,
    RemoteHostPlatform,
    GetRemoteProjectMembersRequest,
    GetLabelsRequest
} from "../../../API/ChasmaWebApiClient";
import React, { useEffect, useState } from "react";
import { remoteClient } from "../../../managers/ApiClientManager";
import Checkbox from "../../Checkbox";
import { useNavigate } from "react-router-dom";
import { useCacheStore } from "../../../managers/CacheManager";
import { handleApiError } from "../../../managers/TransactionHandlerManager";
import { isBlankOrUndefined } from "../../../stringHelperUtil";

/** The members of the page to create issues. **/
interface RemoteIssuesPageProps {
    /** The repository to create issues for. **/
    repository: LocalGitRepository
}

/**
 * Initializes a new instance of the RemoteIssuesPage.
 * @param props The properties of the RemoteIssuesPage.
 * @constructor
 */
const RemoteIssuesPage: React.FC<RemoteIssuesPageProps> = (props: RemoteIssuesPageProps) => {
    /** Gets or sets the modal title. **/
    const [title, setTitle] = useState<string>("Create Issue");

    /** Gets or sets the issue title the user has input. **/
    const [issueTitle, setIssueTitle] = useState<string | undefined>(undefined);

    /** Gets or sets the issue HTML URL. **/
    const [issueHtmlUrl, setIssueHtmlUrl] = useState<string | undefined>(undefined);

    /** Gets or sets the name of the newly created issue. */
    const [issueNumber, setIssueNumber] = useState<number | undefined>(undefined);

    /** Gets or sets the issue message the user has input. **/
    const [issueMessage, setIssueMessage] = useState<string | undefined>(undefined);

    /** Gets or sets the error message. **/
    const [errorMessage, setErrorMessage] = useState<string | undefined>(undefined);

    /** Gets or sets a value indicating whether the create issue response was successful. **/
    const [successfullyCreatedIssue, setSuccessfullyCreatedIssue] = useState<boolean | undefined>(undefined);

    /** Gets or sets the project members associated with the repository. **/
    const [projectMembers, setProjectMembers] = useState<RemoteProjectMember[]>([]);

    /** Gets or sets the main assignee of the issue. **/
    const [mainAssignee, setMainAssignee] = useState<RemoteProjectMember | undefined>(undefined);

    /** Gets or sets the selected issue's selected assignees. */
    const [selectedAssignees, setSelectedAssignees] = useState<{ rowId: string, member?: RemoteProjectMember }[]>([]);

    /** Gets or sets a value indicating whether the user is creating a confidential issue. **/
    const [isConfidential, setIsConfidential] = useState(false);

    /** Gets or sets the flag indicating whether to disable the send button. */
    const [disabledSendButton, setDisableSendButton] = useState(false);

    /** Gets or sets the repository's issue labels. */
    const [labels, setLabels] = useState<string[]>([]);

    /** Gets or sets the selected project issue labels. */
    const [selectedLabels, setSelectedLabels] = useState<{ rowId: string, label?: string }[]>([]);

    /** The navigation function. **/
    const navigate = useNavigate();

    /** Sets the notification modal. */
    const setNotification = useCacheStore(state => state.setNotification);

    /** Handles the event when the user wants to create a task/story for the specified repository. **/
    const handleIssueCreationRequest = async () => {
        setDisableSendButton(true);
        setNotification({
            title: "Attempting to create issue...",
            message: "Please wait while your request is being processed.",
            isError: false,
            loading: true
        });
        if (props.repository.hostPlatform === RemoteHostPlatform.GitHub) {
            await handleCreateGitHubIssueRequest();
            setDisableSendButton(false);
            return;
        }
        else if (props.repository.hostPlatform === RemoteHostPlatform.GitLab) {
            await handleCreateGitLabIssueRequest();
            setDisableSendButton(false);
            return;
        }
        else {
            setDisableSendButton(false);
            setNotification({
                title: "Could not create Issue!",
                message: `The host platform: ${RemoteHostPlatform[props.repository.hostPlatform!]} is not supported!`,
                isError: true,
            });
        }
    }

    /** Handles the event when user attempts to create a GitHub issue. **/
    const handleCreateGitHubIssueRequest = async () => {
        setTitle("Creating issue. May take a few moments...");
        const request = new CreateGitHubIssueRequest();
        request.repositoryName = props.repository.name;
        request.repositoryOwner = props.repository.owner;
        request.title = issueTitle;
        request.body = issueMessage;
        request.assignees = selectedAssignees.map(i => i.member).filter((m): m is RemoteProjectMember => m !== undefined);
        const labels: string[] = [];
        selectedLabels.forEach(labelRow => {
            if (!isBlankOrUndefined(labelRow.label)) {
                labels.push(labelRow.label!);
            }
        });
        request.labels = labels;

        try {
            const response = await remoteClient.createGitHubIssue(request);
            if (response.isErrorResponse) {
                setTitle("Cannot Create Issue");
                setSuccessfullyCreatedIssue(false);
                setErrorMessage(response.errorMessage);
                return;
            }

            performSuccessAction(response.issueUrl, response.issueId);
        } catch (e) {
            setTitle("Error Creating Issue");
            setSuccessfullyCreatedIssue(false);
            setErrorMessage("An internal server error occurred. Please try again after correcting fields.");
            const errorNotification = await handleApiError(e, navigate, "Error Creating Issue!", "An internal server error occurred. Please try again after correcting fields.");
            setNotification(errorNotification);
        }
    };

    /** Handles the event when the user wants to create a GitLab issue. **/
    const handleCreateGitLabIssueRequest = async () => {
        if (isBlankOrUndefined(issueTitle)) {
            setNotification({
                title: "Failed to create issue!",
                message: "Title needs to be populated.",
                isError: true,
            });
            return;
        }

        try {
            const request = new CreateGitLabIssueRequest();
            request.repositoryId = props.repository.id;
            request.mainAssignee = mainAssignee;
            request.contacts = selectedAssignees.map(i => i.member).filter((m): m is RemoteProjectMember => m !== undefined);
            request.title = issueTitle;
            request.description = issueMessage;
            request.confidential = isConfidential;
            const labels: string[] = [];
            selectedLabels.forEach(labelRow => {
                if (!isBlankOrUndefined(labelRow.label)) {
                    labels.push(labelRow.label!);
                }
            });
            request.labels = labels;

            const response = await remoteClient.createGitLabIssue(request);
            if (response.isErrorResponse) {
                setTitle("Cannot Create Issue");
                setSuccessfullyCreatedIssue(false);
                setErrorMessage(response.errorMessage);
                return;
            }

            if (response.issue === undefined) {
                setNotification({
                    title: "Failed to create issue!",
                    message: "Please try again and review error logs.",
                    isError: true,
                });
                return;
            }

            performSuccessAction(response.issue.url, response.issue.issueId);
        }
        catch (e) {
            const errorNotification = await handleApiError(e, navigate, "Error creating issue!", "Review the console logs for more information.");
            setNotification(errorNotification);
        }
    }

    /**
     * Performs the success action on resources when the issue is successfully created.
     * @param url The newly created issue url.
     * @param issueId The newly created issue identifier.
     */
    const performSuccessAction = (url: string | undefined, issueId: number | undefined) => {
        setTitle(`Successfully Created Issue, ${issueId}`);
        setSuccessfullyCreatedIssue(true);
        setIssueHtmlUrl(url);
        setIssueNumber(issueId);
        setErrorMessage(undefined);
        setNotification(null);
    };

    /** Resets the modal form user input data. **/
    const resetForm = () => {
        setTitle("Create Issue");
        setSuccessfullyCreatedIssue(false);
        setIssueTitle("");
        setErrorMessage("");
        setIssueHtmlUrl("");
        setIssueMessage("");
        setSelectedAssignees([]);
        setMainAssignee(undefined);
        setSelectedLabels([]);
    };

    /**
     * Handles the change when the user selects a main assignee.
     * @param id The assignee identifier.
     */
    const handleMainAssigneeChange = (id: string) => {
        const assigneeId = Number(id);
        const assignee = projectMembers.find(i => i.assigneeId === assigneeId);
        setMainAssignee(assignee);
    };

    /** Adds the new row to add an additional assignee for an issue. */
    const addSelectedAdditionalAssigneeRow = () => {
        setSelectedAssignees(prev => [
            ...prev,
            { rowId: crypto.randomUUID(), undefined }
        ])
    };

    /** Adds the new row to add a contact for a GitLab issue. */
    const addSelectedLabelRow = () => {
        let label: string = "";
        setSelectedLabels(prev => [
            ...prev,
            { rowId: crypto.randomUUID(), label }
        ]);
    };

    /**
     * Handles the assignee change when the user selects a new user for the specified row.
     * @param rowId The row identifier.
     * @param contactId The assignee identifier.
     */
    const handleSelectedAssigneeChange = (rowId: string, contactId: string) => {
        const assigneeId = Number(contactId);
        const assignee = projectMembers.find(i => i.assigneeId === assigneeId);
        if (!assignee) {
            return;
        }

        setSelectedAssignees(prev =>
            prev.map(row =>
                row.rowId === rowId ? { ...row, member: assignee } : row
            ));
    };

    /**
     * Handles the label change when the user selects a new user for the specified row.
     * @param rowId The row identifier.
     * @param label The project label.
     */
    const handleSelectedLabelChange = (rowId: string, label: string) => {
        setSelectedLabels(prev =>
            prev.map(row =>
                row.rowId === rowId ? { ...row, label: label } : row
            ));
    };

    /**
     * Deletes the specified contact with the row identifier.
     * @param rowId The row identifier.
     */
    const deleteSelectedAssignee = (rowId: string) => {
        const contacts = selectedAssignees.filter(i => i.rowId !== rowId);
        setSelectedAssignees(contacts);
    }

    /**
     * Deletes the specified label with the row identifier.
     * @param rowId The row identifier.
     */
    const deleteSelectedLabel = (rowId: string) => {
        const projectLabels = selectedLabels.filter(i => i.rowId !== rowId);
        setSelectedLabels(projectLabels);
    }

    /** Fetches the GitLab project members for the specified repository. **/
    const fetchGitLabProjectMembers = async () => {
        if (props.repository.hostPlatform !== RemoteHostPlatform.GitLab) {
            return;
        }

        try {
            const request = new GetRemoteProjectMembersRequest();
            request.repositoryId = props.repository.id;
            const response = await remoteClient.getRemoteProjectMembers(request);
            if (response.isErrorResponse) {
                setNotification({
                    title: "Error fetching project members!",
                    message: response.errorMessage,
                    isError: true,
                });
                return;
            }

            if (response.projectMembers) {
                setProjectMembers(response.projectMembers);
            }
        }
        catch (e) {
            const errorNotification = await handleApiError(e, navigate, "Error fetching GitLab project members!", "Review the console logs for more information.");
            setNotification(errorNotification);
        }
    };

    /** Fetches the GitHub project members for the specified repository. **/
    const fetchGitHubProjectMembers = async () => {
        if (props.repository.hostPlatform !== RemoteHostPlatform.GitHub) {
            return;
        }

        try {
            const request = new GetRemoteProjectMembersRequest();
            request.repositoryId = props.repository.id;
            const response = await remoteClient.getRemoteProjectMembers(request);
            if (response.isErrorResponse) {
                setNotification({
                    title: "Error fetching project members!",
                    message: response.errorMessage,
                    isError: true,
                });
                return;
            }

            if (response.projectMembers) {
                setProjectMembers(response.projectMembers);
            }
        }
        catch (e) {
            const errorNotification = await handleApiError(e, navigate, "Error fetching GitHub project members!", "Review the console logs for more information.");
            setNotification(errorNotification);
        }
    };

    /** Fetches the project members from the prospective cloud provider. */
    const fetchProjectMembers = async () => {
        if (props.repository.hostPlatform === RemoteHostPlatform.GitLab) {
            await fetchGitLabProjectMembers();
            return;
        }

        if (props.repository.hostPlatform === RemoteHostPlatform.GitHub) {
            await fetchGitHubProjectMembers();
            return;
        }
    };

    /** Fetches the project labels from the prospective cloud provider. */
    const fetchLabels = async () => {
        const request = new GetLabelsRequest();
        request.repositoryId = props.repository.id;
        try {
            const response = await remoteClient.retrieveLabels(request);
            if (response.isErrorResponse) {
                setNotification({
                    title: "Error fetching project labels!",
                    message: response.errorMessage,
                    isError: true,
                });

                return;
            }

            if (response.labels) {
                setLabels(response.labels);
            }
        }
        catch (e) {
            const errorNotification = await handleApiError(e, navigate, "Error fetching GitHub project labels!", "Review the console logs for more information.");
            setNotification(errorNotification);
        }
    };

    useEffect(() => {
        fetchProjectMembers().catch(e => console.error(e));
        fetchLabels().catch(e => console.error(e));
    }, [props.repository]);

    return (
        <div className="expandable-pane">
            <header className="batch-page-header">
                <h1 className="page-title">{title}</h1>
                {errorMessage &&
                    <p
                        className="page-description"
                        style={{ color: "red" }}>
                        {errorMessage}
                    </p>
                }
                {!errorMessage &&
                    <p className="page-description">
                        Describe the problem or feature → Steps to reproduce → Expected vs actual behavior → logs if any → Additional notes
                    </p>
                }
            </header>
            {props.repository.hostPlatform === RemoteHostPlatform.GitLab && !successfullyCreatedIssue &&
                <>
                    <div className="repository-actions">
                        <Checkbox
                            label={"Is Confidential"}
                            onBoxChecked={setIsConfidential} />
                    </div>
                    <br />
                </>
            }
            {!successfullyCreatedIssue &&
                <>
                    <input
                        type="text"
                        className="input-field"
                        placeholder="Issue Title"
                        value={issueTitle}
                        onChange={(e) => setIssueTitle(e.target.value)}
                        required />
                    <textarea
                        className="textarea-field"
                        placeholder="Issue Description"
                        value={issueMessage}
                        onChange={(e) => setIssueMessage(e.target.value)} />
                </>
            }
            {!successfullyCreatedIssue &&
                <>
                    <hr className="separator" />
                    {props.repository.hostPlatform === RemoteHostPlatform.GitLab &&
                        <>
                        <h3>Main Assignee</h3>
                            <select
                                className="repo-dropdown input-field"
                                onChange={(e) => handleMainAssigneeChange(e.target.value)}>
                                <option value="">Select Main Assignee</option>
                                {projectMembers.map(member => (
                                    <option
                                        key={member.assigneeId}
                                        value={member.assigneeId}>
                                        {isBlankOrUndefined(member.fullName) ? member.userName : `${member.userName} - ${member.fullName}`}
                                    </option>
                                ))}
                            </select>
                            <hr className="separator" />
                        </>
                    }
                    <div className="repository-actions">
                        <h3>Additional {props.repository.hostPlatform === RemoteHostPlatform.GitLab ? "Contacts" : "Assignees"}</h3>
                        <button
                            className="add-button modern-add"
                            type="button"
                            onClick={addSelectedAdditionalAssigneeRow}>
                            +
                        </button>
                    </div>
                    {selectedAssignees.map(contact => (
                        <React.Fragment key={contact.rowId}>
                            <div key={contact.rowId} className="command-row modern-input-row">
                                <select
                                    className="repo-dropdown input-field"
                                    value={contact.member?.assigneeId ?? ""}
                                    onChange={(e) => handleSelectedAssigneeChange(contact.rowId, e.target.value)}>
                                    <option value="">Select Contact</option>
                                    {projectMembers.map(member => (
                                        <option
                                            key={member.assigneeId}
                                            value={member.assigneeId}>
                                            {isBlankOrUndefined(member.fullName) ? member.userName : `${member.userName} - ${member.fullName}`}
                                        </option>
                                    ))}
                                </select>
                                <button
                                    className="remove-button modern-remove"
                                    title="Remove shell command"
                                    onClick={() => deleteSelectedAssignee(contact.rowId)}
                                >
                                    −
                                </button>
                            </div>
                        </React.Fragment>
                    ))}
                    <hr className="separator" />
                    <div className="repository-actions">
                        <h3>Labels</h3>
                        <button
                            className="add-button modern-add"
                            type="button"
                            onClick={addSelectedLabelRow}>
                            +
                        </button>
                    </div>
                    {selectedLabels.map(projectIssueLabel => (
                        <React.Fragment key={projectIssueLabel.rowId}>
                            <div key={projectIssueLabel.rowId} className="command-row modern-input-row">
                                <select
                                    className="repo-dropdown input-field"
                                    value={projectIssueLabel.label ?? ""}
                                    onChange={(e) => handleSelectedLabelChange(projectIssueLabel.rowId, e.target.value)}>
                                    <option value="">Select Label</option>
                                    {labels.map(projectLabel => (
                                        <option
                                            key={projectLabel}
                                            value={projectLabel}>
                                            {projectLabel}
                                        </option>
                                    ))}
                                </select>
                                <button
                                    className="remove-button modern-remove"
                                    title="Remove shell command"
                                    onClick={() => deleteSelectedLabel(projectIssueLabel.rowId)}
                                >
                                    −
                                </button>
                            </div>
                        </React.Fragment>
                    ))}
                </>
            }
            {successfullyCreatedIssue &&
                <>
                    <div className="panel-card">
                        <h1
                            className="page-title"
                            style={{ color: "lightgreen" }}>
                            Newly Created Issue: {issueNumber}
                        </h1>
                        <p className="page-description">Open your {RemoteHostPlatform[props.repository.hostPlatform!]} issue and see the details in action 🚀</p>
                        <div
                            className="modal-input-field"
                            onClick={() => window.open(`${issueHtmlUrl}`, "_blank")}
                            style={{ cursor: "pointer" }}>
                            {issueHtmlUrl}
                        </div>
                    </div>
                </>
            }
            <div className="repository-actions">
                <button
                    type="submit"
                    className="submit-button"
                    onClick={resetForm}>
                    Clear
                </button>
                <button
                    type="submit"
                    className="submit-button"
                    disabled={disabledSendButton}
                    onClick={handleIssueCreationRequest}>
                    Create Issue
                </button>
            </div>
        </div>
    )
}

export default RemoteIssuesPage;