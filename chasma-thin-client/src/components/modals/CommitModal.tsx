import React, {useState} from "react";
import "../../css/CommitModal.css";
import {GitCommitRequest, RepositoryStatusClient} from "../../API/ChasmaWebApiClient";

/** The properties to handle commit messages. **/
interface ICommitModalProps {
    /** The confirmation action of the close function. **/
    onClose: () => void;

    /** The repository identifier. **/
    repositoryId: string | undefined;

    /** The logged-in user's email. **/
    email: string | undefined;

    /** The logged-in user's identifier. **/
    userId: number | undefined;
}

/** The status client for the web API. **/
const statusClient = new RepositoryStatusClient()

/**
 * Initializes a new CommitModal class.
 * @param props The properties to handle committing changes.
 * @constructor
 */
const CommitModal: React.FC<ICommitModalProps> = (props: ICommitModalProps) => {
    /** Gets or sets the commit message the user has input. **/
    const [commitMessage, setCommitMessage] = useState<string | undefined>(undefined);

    /** Gets or sets the modal title. **/
    const [title, setTitle] = useState<string>("Commit Changes");

    /** Gets or sets the error message. **/
    const [errorMessage, setErrorMessage] = useState<string | undefined>(undefined);

    /** Gets or sets a value indicating whether the commit response was successful. **/
    const [successfullyCommitted, setSuccessfullyCommitted] = useState<boolean | undefined>(undefined);

    /** Gets or sets a value indicating whether the commit request was sent. **/
    const [commitRequestSent, setCommitRequestSent] = useState<boolean>(false);

    /** Handles the request to commit local changes. **/
    async function handleCommitChangesRequest() {
        try {
            const request = new GitCommitRequest();
            request.repositoryId = props.repositoryId;
            request.userId = props.userId;
            request.email = props.email;
            request.commitMessage = commitMessage;
            const response = await statusClient.commitChanges(request);
            if (response.isErrorResponse) {
                setTitle("Error committing changes!")
                setErrorMessage(response.errorMessage);
                setSuccessfullyCommitted(false);
                return;
            }

            setErrorMessage(undefined);
            setTitle("Successfully committed changes!");
            setSuccessfullyCommitted(true);
        }
        catch (e) {
            setTitle("Error committing changes!")
            setErrorMessage("Check console logs for more information.");
            console.error(e);
            setSuccessfullyCommitted(false);
        }
        finally {
            setCommitRequestSent(true);
            setCommitMessage(undefined);
        }
    }

    return (
        <>
            <div className="modal-backdrop" onClick={props.onClose}>
                <div className="commit-modal" onClick={(e) => e.stopPropagation()}>
                    <div className="commit-modal-icon">
                        {!errorMessage && !successfullyCommitted && (
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
                        {!errorMessage && successfullyCommitted && (
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
                    <h2 className="commit-modal-title"
                        style={{marginTop: "-30px"}}>{title}</h2>
                    {errorMessage && <h3 className="commit-modal-message">{errorMessage}</h3>}
                    <textarea className="input-area"
                              placeholder="Enter commit message:"
                              value={commitMessage}
                              onChange={(e) => setCommitMessage(e.target.value)} />
                    <br/>
                    {!commitRequestSent && <button className="commit-modal-button" onClick={handleCommitChangesRequest}>Commit</button>}
                    {commitRequestSent && <button className="commit-modal-button" onClick={props.onClose}>Close</button>}
                </div>
            </div>
        </>
    )
}

export default CommitModal;