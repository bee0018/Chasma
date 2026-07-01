import { useState } from "react";
import { ChangeRepositoryDisplayNameRequest, LocalGitRepository } from "../../API/ChasmaWebApiClient";
import { configClient } from "../../managers/ApiClientManager";
import { handleApiError } from "../../managers/TransactionHandlerManager";
import { useNavigate } from "react-router-dom";
import { useCacheStore } from "../../managers/CacheManager";
import { createPortal } from "react-dom";

/**
 * Interface containing the members of the ChangeRepositoryDisplayNameModal.
 */
interface IChangeRepositoryDisplayNameModal {
    /** The confirmation action of the close function. **/
    onClose: () => void;

    /** The selected git repository. */
    repository: LocalGitRepository;
}

/**
 * Intitializes a new instance of the ChangeRepositoryDisplayNameModal.
 * @param props The properties of the modal.
 * @constructor
 */
const ChangeRepositoryDisplayNameModal: React.FC<IChangeRepositoryDisplayNameModal> = (props: IChangeRepositoryDisplayNameModal) => {
    /** Gets or sets the modal title. **/
    const [title, setTitle] = useState<string>(`Change Current Display Name: ${props.repository.displayName ? props.repository.displayName : props.repository.name}`);

    /** Gets or sets the error message. **/
    const [errorMessage, setErrorMessage] = useState<string | undefined>(undefined);

    /** Gets or sets a value indicating whether the change display name response was successful. **/
    const [successfullyChanged, setSuccessfullyChanged] = useState<boolean | undefined>(undefined);

    /** Gets or sets the display name that will be applied to the repository. */
    const [displayName, setDisplayName] = useState<string | undefined>(undefined);

    /** Gets or sets the flag indicating whether to disable the send button. */
    const [disabledSendButton, setDisableSendButton] = useState(false);

    /** Sets the notification modal. */
    const setNotification = useCacheStore(state => state.setNotification);

    /** The navigation function. **/
    const navigate = useNavigate();

    /**
     * Handles the event when the user wants to change the display of a repository.
     */
    const handleChangeDisplayNameRequest = async () => {
        if (disabledSendButton) {
            return;
        }

        if (!displayName || displayName.length === 0) {
            setTitle("Failed to change display name!");
            setErrorMessage("A display name must be entered.");
            setSuccessfullyChanged(false);
            return;
        }

        setDisableSendButton(true);
        const request = new ChangeRepositoryDisplayNameRequest();
        request.repositoryId = props.repository.id;
        request.newName = displayName;
        try {
            const response = await configClient.changeRepositoryDisplayName(request);
            if (response.isErrorResponse) {
                setTitle("Failed to change display name!");
                setErrorMessage(response.errorMessage);
                setSuccessfullyChanged(false);
                return;
            }

            if (response.repository) {
                setTitle(`Successfully changed display name to: ${response.repository?.displayName}!`)
                setErrorMessage(undefined);
                useCacheStore.getState().updateLocalGitRepository(response.repository);
                setSuccessfullyChanged(true);
            }
        } catch (error) {
            setTitle("Error changing display name!")
            setErrorMessage("Check console logs for more information.");
            setSuccessfullyChanged(false);
            const errorNotification = await handleApiError(error, navigate, "Error changing display name!", "Check console logs for more information.");
            setNotification(errorNotification);
        }
        finally {
            setDisableSendButton(false);
        }
    };

    return createPortal(
        <div className="modal-backdrop" onClick={props.onClose}>
            <div className="modal" onClick={(e) => e.stopPropagation()}>
                <div className="modal-icon-container">
                    {!errorMessage && !successfullyChanged && (
                        <svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 24 24" width="48" height="48" fill="none">
                            <circle cx="12" cy="12" r="10" fill="#00bfff" />
                            <rect x="11" y="10" width="2" height="7" fill="#ffffff" />
                            <rect x="11" y="7" width="2" height="2" fill="#ffffff" />
                        </svg>
                    )}
                    {!errorMessage && successfullyChanged && (
                        <svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 24 24" width="48" height="48" fill="none">
                            <circle cx="12" cy="12" r="10" fill="#4caf50" />
                            <path d="M16 9l-5.2 6L8 11.5" fill="none" stroke="#fff" strokeWidth="2" strokeLinecap="round" strokeLinejoin="round" />
                        </svg>
                    )}
                    {errorMessage && (
                        <svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 24 24" width="48" height="48" fill="none">
                            <circle cx="12" cy="12" r="10" fill="#ff4c4c" />
                            <rect x="11" y="6" width="2" height="8" fill="#fff" />
                            <rect x="11" y="16" width="2" height="2" fill="#fff" />
                        </svg>
                    )}
                </div>
                <h2 className="modal-title">{title}</h2>
                {errorMessage && <h3 className="modal-message">{errorMessage}</h3>}
                <input className="modal-input-field"
                    placeholder="Enter new display name:"
                    value={displayName || ""}
                    onChange={(e) => setDisplayName(e.target.value)} />
                <br />
                <div className="modal-actions">
                    {!successfullyChanged &&
                        <button className="modal-button primary" onClick={handleChangeDisplayNameRequest} disabled={disabledSendButton}>
                            Change
                        </button>
                    }
                    <button className="modal-button secondary" onClick={props.onClose}>
                        Close
                    </button>
                </div>
            </div>
        </div>,
        document.body
    );
}

export default ChangeRepositoryDisplayNameModal;