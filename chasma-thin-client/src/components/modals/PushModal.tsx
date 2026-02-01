import React, {useState} from "react";
import {GitPushRequest, RepositoryStatusClient} from "../../API/ChasmaWebApiClient";
import {apiBaseUrl} from "../../environmentConstants";

/**
 * Defines the properties/members of the push modal props.
 */
interface IPushModalProps {
    /** The confirmation action of the close function. **/
    onClose: () => void;

    /** Function to call when the response is successful. **/
    onSuccess: () => void,

    /** The repository identifier. **/
    repositoryId: string | undefined;
}

/** The repository status client. **/
const statusClient = new RepositoryStatusClient(apiBaseUrl);

/**
 * Initializes a new instance of the PushModal class.
 * @param props The properties of the push modal.
 * @constructor
 */
const PushModal: React.FC<IPushModalProps> = (props: IPushModalProps) => {

    /** Gets or sets the modal title. **/
    const [title, setTitle] = useState<string>("Are you sure you want to push changes?");

    /** Gets or sets the error message. **/
    const [errorMessage, setErrorMessage] = useState<string | undefined>(undefined);

    /** Gets or sets a value indicating whether the push response was successful. **/
    const [successfullyPushed, setSuccessfullyPushed] = useState<boolean | undefined>(undefined);

    /** Gets or sets a value indicating whether the push request was sent. **/
    const [pushRequestSent, setPushRequestSent] = useState<boolean>(false);

    /**
     * Handles the push changes request.
     */
    const handlePushChangesRequest = async () => {
        setTitle("Attempting to push changes. May take a few moments...");
        const request = new GitPushRequest();
        request.repositoryId = props.repositoryId;
        try {
            const response = await statusClient.pushChanges(request);
            if (response.isErrorResponse) {
                setTitle("Could not push changes!");
                setErrorMessage(response.errorMessage);
                setSuccessfullyPushed(false);
                return;
            }

            setTitle("Successfully Pushed!");
            setErrorMessage(undefined);
            setSuccessfullyPushed(true);
            props.onSuccess();
        }
        catch (e) {
            setTitle("Could not push changes!");
            setErrorMessage("Check console logs for more information.");
            setSuccessfullyPushed(false);
        }
        finally {
            setPushRequestSent(true);
        }
    };
    return (
        <>
            <div className="modal-backdrop" onClick={props.onClose}>
                <div className="modal" onClick={(e) => e.stopPropagation()}>
                    <div className="modal-icon-container">
                        {!errorMessage && !successfullyPushed && (
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
                        {!errorMessage && successfullyPushed && (
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
                    <div className="modal-actions">
                        <button
                            className="modal-button primary"
                            disabled={pushRequestSent}
                            onClick={handlePushChangesRequest}
                        >
                            Push
                        </button>
                        <button
                            className="modal-button secondary"
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

export default PushModal;