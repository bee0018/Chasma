import React, {useState} from "react";
import { useNavigate } from "react-router-dom";
import { useCacheStore } from "../../managers/CacheManager";
import { handleApiError } from "../../managers/TransactionHandlerManager";
import { LogoutRequest } from "../../API/ChasmaWebApiClient";
import { userClient } from "../../managers/ApiClientManager";

/**
 * Defines the properties/members of the logout modal props.
 */
interface ILogoutModalProps {
    /** The confirmation action of the close function. **/
    onClose: () => void;

    /** Function to call when the response is successful. **/
    onSuccess: () => void,
}

/**
 * Initializes a new instance of the LogoutModal class.
 * @param props The properties of the logout modal.
 * @constructor
 */
const LogoutModal: React.FC<ILogoutModalProps> = (props: ILogoutModalProps) => {

    /** Gets or sets the modal title. **/
    const [title, setTitle] = useState<string>("Are you sure you want to logout?");

    /** Gets or sets the error message. **/
    const [errorMessage, setErrorMessage] = useState<string | undefined>(undefined);

    /** Gets or sets a value indicating whether the logout response was successful. **/
    const [successfullyLoggedOut, setSuccessfullyLoggedOut] = useState<boolean | undefined>(undefined);

    /** Gets or sets the flag indicating whether to disable the send button. */
    const [disabledSendButton, setDisableSendButton] = useState(false);

    /** The navigation function. **/
    const navigate = useNavigate();

   /** Sets the notification modal. */
   const setNotification = useCacheStore(state => state.setNotification);

   /** Gets the user that is currently logged in. */
   const user = useCacheStore(state => state.user);

    /**
     * Handles the logout changes request.
     */
    const handleLogoutRequest = async () => {
        setDisableSendButton(true);
        setTitle("Attempting to logout. May take a few moments...");
        const request = new LogoutRequest();
        request.userId = user?.userId;
        try {
            const response = await userClient.logout(request);
            if (response.isErrorResponse) {
                setTitle("Could not logout!");
                setErrorMessage(response.errorMessage);
                setSuccessfullyLoggedOut(false);
                setDisableSendButton(false);
                return;
            }

            setTitle("Successfully logged out!");
            setErrorMessage(undefined);
            setSuccessfullyLoggedOut(true);
            setDisableSendButton(false);
            props.onSuccess();
        }
        catch (e) {
            setTitle("Could not logout!");
            setErrorMessage("Check console logs for more information.");
            setSuccessfullyLoggedOut(false);
            setDisableSendButton(false);
            const errorNotification = handleApiError(e, navigate, "Could not logout!", "Check console logs for more information.");
            setNotification(errorNotification);
        }
    };
    return (
        <>
            <div className="modal-backdrop" onClick={props.onClose}>
                <div className="modal" onClick={(e) => e.stopPropagation()}>
                    <div className="modal-icon-container">
                        {!errorMessage && !successfullyLoggedOut && (
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
                        {!errorMessage && successfullyLoggedOut && (
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
                            disabled={disabledSendButton}
                            hidden={successfullyLoggedOut}
                            onClick={handleLogoutRequest}
                        >
                            Logout
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

export default LogoutModal;