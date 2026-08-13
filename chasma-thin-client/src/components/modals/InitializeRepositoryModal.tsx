import { useEffect, useState } from "react";
import { useNavigate } from "react-router-dom";
import { useCacheStore } from "../../managers/CacheManager";
import { InitializeRepositoryRequest } from "../../API/ChasmaWebApiClient";
import { configClient } from "../../managers/ApiClientManager";
import { handleApiError } from "../../managers/TransactionHandlerManager";

/**
 * Interface defining the members of the IntializeRepositoryModal.
 */
interface IInitializeRepositoryModal {
    /** The confirmation action of the close function. **/
    onClose: () => void;

    /** The repository identifier. **/
    repositoryId: string | undefined;

    /** The logged-in user's identifier. **/
    userId: number | undefined;

    /** Function to call when the response is successful. **/
    onSuccess: () => void,
}

/**
 * Initializes a new InitializeRepositoryModal class.
 * @param props The properties to initialize new repository.
 * @constructor
 */
const InitializeRepositoryModal: React.FC<IInitializeRepositoryModal> = (props: IInitializeRepositoryModal) => {

    /** Gets or sets the modal title. **/
    const [title, setTitle] = useState<string>("Initialize Repository");

    /** Gets or sets the commit message for the intialize commit. */
    const [commitMessage, setCommitMessage] = useState<string | undefined>(undefined);

    /** Gets or sets the head branch name of the repository. */
    const [headBranchName, setHeadBranchName] = useState<string | undefined>(undefined);

    /** Gets or sets the flag indicating whether to disable the send button. */
    const [disabledSendButton, setDisableSendButton] = useState(false);

    /** Gets or sets the error message. **/
    const [errorMessage, setErrorMessage] = useState<string | undefined>(undefined);

    /** Gets or sets a value indicating whether the initialize response was successful. **/
    const [successfullyInitialized, setSuccessfullyInitialized] = useState<boolean | undefined>(undefined);

    /** Gets or sets a value indicating whether the intialize request was sent. **/
    const [initializeRequestSent, setInitializeRequestSent] = useState<boolean>(false);

    /** The navigation function. **/
    const navigate = useNavigate();

    /** Sets the notification modal. */
    const setNotification = useCacheStore(state => state.setNotification);

    /** Handles the request to initialize repository request. **/
    async function handleInitializeRepositoryRequest() {
        if (disabledSendButton) {
            return;
        }

        setDisableSendButton(true);
        setTitle("Attempting to initialize repository...");
        try {
            const request = new InitializeRepositoryRequest();
            request.userId = props.userId;
            request.repositoryId = props.repositoryId;
            request.headBranchName = headBranchName;
            request.commitMessage = commitMessage;
            const response = await configClient.initializeNewRepository(request);
            if (response.isErrorResponse) {
                setTitle("Failed to initialize repository!");
                setErrorMessage(response.errorMessage);
                setSuccessfullyInitialized(false);
                return;
            }

            setErrorMessage(undefined);
            setTitle("Successfully initialized repository!");
            setSuccessfullyInitialized(true);
            props.onSuccess();
        } catch (error) {
            setTitle("Error initializing repository!")
            setErrorMessage("Check server logs for more information.");
            setSuccessfullyInitialized(false);
            const errorNotification = await handleApiError(error, navigate, "Error initializing repository!", "Check server logs for more information.");
            setNotification(errorNotification);
        }
        finally {
            setInitializeRequestSent(true);
            setCommitMessage(undefined);
            setDisableSendButton(false);
        }
    };

    useEffect(() => {
        const handler = (e: KeyboardEvent) => {
            if (e.key === "Escape") {
                props.onClose();
            }
        };

        window.addEventListener("keydown", handler);
        return () => window.removeEventListener("keydown", handler);
    }, []);

    return (
        <>
            <div className="modal-backdrop" onClick={props.onClose}>
                <div className="modal" onClick={(e) => e.stopPropagation()}>
                    <div className="modal-icon-container">
                        {!errorMessage && !successfullyInitialized && (
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
                        {!errorMessage && successfullyInitialized && (
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
                    <input className="modal-input-field"
                        placeholder="Enter HEAD branch name (Optional)"
                        value={headBranchName}
                        onChange={(e) => setHeadBranchName(e.target.value)} />
                    <br />
                    <textarea className="modal-input-area"
                        placeholder="Enter commit message (Optional)"
                        value={commitMessage}
                        onChange={(e) => setCommitMessage(e.target.value)} />
                    <br />
                    <div className="modal-actions">
                        {!initializeRequestSent &&
                            <button
                                className="modal-button primary"
                                disabled={disabledSendButton}
                                onClick={handleInitializeRepositoryRequest}
                            >
                                Initialize
                            </button>
                        }
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


export default InitializeRepositoryModal;