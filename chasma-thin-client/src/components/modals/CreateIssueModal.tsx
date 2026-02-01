import {CreateGitHubIssueRequest, RepositoryStatusClient} from "../../API/ChasmaWebApiClient";
import React, {useState} from "react";
import {apiBaseUrl} from "../../environmentConstants";
import {useCacheStore} from "../../managers/CacheManager";

/** The status client for the web API. **/
const statusClient = new RepositoryStatusClient(apiBaseUrl);

/** The members of the modal to create issues. **/
interface CreateIssueModalProps {
    /** Function to call when the modal is being closed. **/
    onClose: () => void;

    /** The repository name. **/
    repoName: string | undefined;

    /** The repository identifier. **/
    repositoryId: string | undefined;
}

/**
 * Initializes a new instance of the CreateIssueModal
 * @param props The properties of the modal.
 * @constructor
 */
const CreateIssueModal: React.FC<CreateIssueModalProps> = (props: CreateIssueModalProps) => {
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

    /** Gets the local git repositories. **/
    const localGitRepositories = useCacheStore((state) => state.repositories);

    /** Handles the event when user attempts to create a GitHub issue. **/
    const handleCreateIssueRequest = async () => {
        setTitle("Creating issue. May take a few moments...");
        const repository = localGitRepositories.find(i => i.id == props.repositoryId);
        if (!repository) {
            setTitle("Cannot Create Issue");
            setSuccessfullyCreatedIssue(false);
            setErrorMessage(`Repository not found for ${props.repositoryId}`);
            return;
        }

        const request = new CreateGitHubIssueRequest();
        request.repositoryName = props.repoName;
        request.repositoryOwner = repository.owner;
        request.title = issueTitle;
        request.body = issueMessage;
        try {
            const response = await statusClient.createGitHubIssue(request);
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
            setTitle("Cannot Create Issue");
            setSuccessfullyCreatedIssue(false);
            setErrorMessage("Review the console logs for more information.");
        }
    };
    return (
        <>
            <div className="modal-backdrop" onClick={props.onClose}>
                <div className="modal" onClick={(e) => e.stopPropagation()}>
                    <div className="modal-icon-container">
                        {!errorMessage && !successfullyCreatedIssue && (
                            <svg
                                xmlns="http://www.w3.org/2000/svg"
                                viewBox="0 0 24 24"
                                width="48"
                                height="48"
                                fill="none"
                            >
                                <circle cx="12" cy="12" r="10" fill="#00bfff" />
                                <rect x="11" y="10" width="2" height="7" fill="#ffffff" />
                                <rect x="11" y="7" width="2" height="2" fill="#ffffff" />
                            </svg>
                        )}
                        {!errorMessage && successfullyCreatedIssue && (
                            <svg
                                xmlns="http://www.w3.org/2000/svg"
                                viewBox="0 0 24 24"
                                width="48"
                                height="48"
                                fill="none"
                            >
                                <circle cx="12" cy="12" r="10" fill="#4caf50" />
                                <path
                                    d="M16 9l-5.2 6L8 11.5"
                                    fill="none"
                                    stroke="#fff"
                                    strokeWidth="2"
                                    strokeLinecap="round"
                                    strokeLinejoin="round"
                                />
                            </svg>
                        )}
                        {errorMessage && (
                            <svg
                                xmlns="http://www.w3.org/2000/svg"
                                viewBox="0 0 24 24"
                                width="48"
                                height="48"
                                fill="none"
                            >
                                <circle cx="12" cy="12" r="10" fill="#ff4c4c" />
                                <rect x="11" y="6" width="2" height="8" fill="#fff" />
                                <rect x="11" y="16" width="2" height="2" fill="#fff" />
                            </svg>
                        )}
                    </div>
                    <h2 className="modal-title">{title}</h2>
                    {errorMessage && <h3 className="modal-message">{errorMessage}</h3>}
                    <br/>
                    <input
                        type="text"
                        className="modal-input-field"
                        placeholder="Issue Title"
                        value={issueTitle}
                        onChange={(e) => setIssueTitle(e.target.value)}
                        required
                    />
                    <textarea className="modal-input-area"
                              placeholder="Enter Issue Description:"
                              value={issueMessage}
                              onChange={(e) => setIssueMessage(e.target.value)} />
                    <br/>
                    {successfullyCreatedIssue && issueHtmlUrl && (
                        <div className="modal-input-field"
                             onClick={() => window.open(`${issueHtmlUrl}`, "_blank")}
                             style={{cursor: "pointer"}}
                        >
                            GitHub Issue Url: {issueHtmlUrl}
                        </div>
                    )}
                    <br/>
                    <div className="modal-actions">
                        <button className="modal-button primary"
                                disabled={successfullyCreatedIssue}
                                onClick={handleCreateIssueRequest}
                        >
                            Create Issue
                        </button>
                        <button className="modal-button secondary"
                                onClick={props.onClose}
                        >
                            Close
                        </button>
                    </div>
                </div>
            </div>
        </>
    )
}

export default CreateIssueModal;