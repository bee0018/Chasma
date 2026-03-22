import {
    CreateGitHubIssueRequest,
    CreateGitLabIssueRequest,
    GetGitLabProjectMembersRequest,
    GitLabProjectMember,
    LocalGitRepository,
    RemoteHostPlatform
} from "../../../API/ChasmaWebApiClient";
import React, {useEffect, useState} from "react";
import {remoteClient} from "../../../managers/ApiClientManager";
import Checkbox from "../../Checkbox";
import NotificationModal from "../../modals/NotificationModal";

/** The members of the page to create issues. **/
interface RemoteIssuesPageProps {
    /** The repository to create issues for. **/
    repository : LocalGitRepository
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

    /** Gets or sets the GitLab project members associated with the repository. **/
    const [gitLabProjectMembers, setGitLabProjectMembers] = useState<GitLabProjectMember[]>([]);

    /** Gets or sets the main GitLab assignee of the issue. **/
    const [mainGitLabAssignee, setMainGitLabAssignee] = useState<GitLabProjectMember | undefined>(undefined);

    /** Gets or sets the selected GitLab issue contacts. */
    const [selectedContacts, setSelectedContacts] = useState<{rowId: string, member?: GitLabProjectMember}[]>([]);

    /** Gets or sets a value indicating whether the user is creating a confidential issue. **/
    const [isConfidential, setIsConfidential] = useState(false);

    /** Gets or sets the notification **/
    const [notification, setNotification] = useState<{
        title: string;
        message: string | undefined;
        isError: boolean | undefined;
        loading?: boolean;
    } | null>(null);

    /** Handles the event when the user wants to create a task/story for the specified repository. **/
    const handleIssueCreationRequest = async () => {
        setNotification({
            title: "Attempting to create issue...",
            message: "Please wait while your request is being processed.",
            isError: false,
            loading: true
        });
        if (props.repository.hostPlatform === RemoteHostPlatform.GitHub) {
            await handleCreateGitHubIssueRequest();
            return;
        }
        else if (props.repository.hostPlatform === RemoteHostPlatform.GitLab) {
            await handleCreateGitLabIssueRequest();
            return;
        }
        else {
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
            console.error(e);
            setTitle("Error Creating Issue");
            setSuccessfullyCreatedIssue(false);
            setErrorMessage("An internal server error occurred. Please try again after correcting fields.");
            setNotification({
                title: "Error Creating Issue!",
                message: "Review the console logs for more information.",
                isError: true,
            });
        }
    };

    /** Handles the event when the user wants to create a GitLab issue. **/
    const handleCreateGitLabIssueRequest = async () => {
        try {
            const request = new CreateGitLabIssueRequest();
            request.repositoryId = props.repository.id;
            request.mainAssignee = mainGitLabAssignee;
            request.contacts = request.contacts = selectedContacts.map(i => i.member).filter((m): m is GitLabProjectMember => m !== undefined);
            request.title = issueTitle;
            request.description = issueMessage;
            request.confidential = isConfidential;
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
            setNotification({
                title: "Error creating issue!",
                message: "Review the console logs for more information.",
                isError: true,
            });
            console.error(e);
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
        setSelectedContacts([]);
        setMainGitLabAssignee(undefined);
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

    /** Adds the new row to add a contact for a GitLab issue. */
    const addSelectedContactRow = () => {
        setSelectedContacts(prev => [
            ...prev,
            {rowId: crypto.randomUUID(), undefined}
        ])
    };

    /**
     * Handles the contact change when the user selects a new user for the specified row.
     * @param rowId The row identifier.
     * @param contactId The contact identifier.
     */
    const handleSelectedContactChange = (rowId: string, contactId : string) => {
        const assigneeId = Number(contactId);
        const assignee = gitLabProjectMembers.find(i => i.assigneeId === assigneeId);
        if (!assignee) {
            return;
        }

        setSelectedContacts(prev =>
            prev.map(row =>
                row.rowId === rowId ? { ...row, member: assignee } : row
            ));
    };

    /**
     * Deletes the specified contact with the row identifier.
     * @param rowId The row identifier.
     */
    const deleteSelectedContact = (rowId: string) => {
        const contacts = selectedContacts.filter(i => i.rowId !== rowId);
        setSelectedContacts(contacts);
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

    useEffect(() => {
        fetchGitLabProjectMembers().catch(e => console.error(e));
    }, [props.repository]);

    return (
        <>
          <header className="batch-page-header">
            <h1 className="page-title">{title}</h1>
            {errorMessage &&
                <p
                    className="page-description"
                    style={{color: "red"}}>
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
                    <br/>
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
            {props.repository.hostPlatform === RemoteHostPlatform.GitLab && !successfullyCreatedIssue &&
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
                        <h3>Additional Contacts</h3>
                        <button
                            className="add-button modern-add"
                            type="button"
                            onClick={addSelectedContactRow}>
                                +
                        </button>
                    </div>
                    {selectedContacts.map(contact => (
                        <React.Fragment key={contact.rowId}>
                            <div key={contact.rowId} className="command-row modern-input-row">
                                <select
                                    className="repo-dropdown input-field"
                                    value={contact.member?.assigneeId ?? ""}
                                    onChange={(e) => handleSelectedContactChange(contact.rowId, e.target.value)}>
                                    <option value="">Select Contact</option>
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
                                    onClick={() => deleteSelectedContact(contact.rowId)}
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
                            style={{color : "lightgreen"}}>
                                Newly Created Issue: {issueNumber}
                        </h1>
                        <p className="page-description">Open your {RemoteHostPlatform[props.repository.hostPlatform!]} issue and see the details in action 🚀</p>
                        <div
                            className="modal-input-field"
                            onClick={() => window.open(`${issueHtmlUrl}`, "_blank")}
                            style={{cursor: "pointer"}}>
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
                    onClick={handleIssueCreationRequest}>
                        Create Issue
                </button>
            </div>
            {notification &&
                <NotificationModal
                    title={notification.title}
                    message={notification.message}
                    isError={notification.isError}
                    loading={notification.loading}
                    onClose={() => setNotification(null)} />
            }
        </>
    )
}

export default RemoteIssuesPage;