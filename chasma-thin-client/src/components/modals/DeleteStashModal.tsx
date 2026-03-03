import React from "react";
import {
    DeleteStashRequest,
    StashClient,
} from "../../API/ChasmaWebApiClient";
import {apiBaseUrl} from "../../environmentConstants";

/**
 * The members of the Add stash modal.
 */
interface IDeleteStashModalProps {
    /** The repository identifier. **/
    repositoryId: string | undefined;

    /** The stash index. **/
    stashIndex: number | undefined;

    /** The confirmation action of the close function. **/
    onClose: () => void;

    /** The action to invoke when the stash is successfully applied. **/
    onSuccess: () => void;
}

/** The repository stashing client for the web API. **/
const stashClient = new StashClient(apiBaseUrl)

/**
 * Initializes a new instance of the DeleteStashModal component.
 * @param props The properties of the delete stash modal.
 * @constructor
 */
const DeleteStashModal: React.FC<IDeleteStashModalProps> = (props: IDeleteStashModalProps) => {
    /** Gets or sets the error message. **/
    const [errorMessage, setErrorMessage] = React.useState<string | undefined>(undefined);

    /** Gets or sets a value indicating whether the changes were successfully deleted. **/
    const [successfullyDeletedStashed, setSuccessfullyDeletedStashed] = React.useState<boolean | undefined>(undefined);

    /** Gets or sets the modal title. **/
    const [title, setTitle] = React.useState<string>(`Delete Stash ${props.stashIndex}?`);

    /** Handles the event when the user wants to delete the specified stash. **/
    const handleDeleteStashRequest = async () => {
        setTitle("Attempting to delete the stash...");
        try {
            const request = new DeleteStashRequest();
            request.repositoryId = props.repositoryId;
            request.stashIndex = props.stashIndex;
            const response = await stashClient.deleteStash(request);
            if (response.isErrorResponse) {
                setTitle(`Error deleting stash ${props.stashIndex}`);
                setErrorMessage(response.errorMessage);
                return;
            }

            setSuccessfullyDeletedStashed(true);
            setErrorMessage(undefined);
            setTitle("Successfully deleted!");
            props.onSuccess()
        }
        catch (e) {
            setTitle("Error deleting stash!");
            setErrorMessage("An error occurred when attempting to stash changes. Review console and internal server logs.");
            console.error(e);
        }
    };
    return (
        <>
            <div className="modal-backdrop" onClick={props.onClose}>
                <div className="modal" onClick={(e) => e.stopPropagation()}>
                    <div className="modal-icon-container">
                        {!errorMessage && !successfullyDeletedStashed && (
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
                        {!errorMessage && successfullyDeletedStashed && (
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
                        <button className="modal-button primary"
                                hidden={successfullyDeletedStashed}
                                onClick={handleDeleteStashRequest}
                        >
                            Delete
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

export default DeleteStashModal;