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

    /** Gets or sets the GitLab issue contacts. **/
    const [gitLabIssueContacts, setGitLabIssueContacts] = useState<GitLabProjectMember[]>([]);

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
                title: "Could create Issue!",
                message: `The host platform: ${props.repository.hostPlatform} is not supported!`,
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

            setTitle(`Successfully Created Issue, ${response.issueId}`);
            setSuccessfullyCreatedIssue(true);
            setIssueHtmlUrl(response.issueUrl);
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
            request.contacts = gitLabIssueContacts;
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

            setTitle(`Successfully Created Issue, ${response.issue.issueId}`);
            setSuccessfullyCreatedIssue(true);
            setIssueHtmlUrl(response.issue.url);
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

    /** Resets the modal form user input data. **/
    const resetForm = () => {
        setTitle("Create Issue");
        setSuccessfullyCreatedIssue(false);
        setIssueTitle("");
        setErrorMessage("");
        setIssueHtmlUrl("");
        setIssueMessage("");
    };

    return (
        <>
        </>
    )
}

export default RemoteIssuesPage;