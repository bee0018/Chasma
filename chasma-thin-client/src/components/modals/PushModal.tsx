import React, { useState } from "react";
import { ConnectRemoteRepositoryRequest, GitPushRequest, LocalGitRepository, RemoteHostPlatform } from "../../API/ChasmaWebApiClient";
import { configClient, statusClient } from "../../managers/ApiClientManager";
import { useNavigate } from "react-router-dom";
import { useCacheStore } from "../../managers/CacheManager";
import { handleApiError } from "../../managers/TransactionHandlerManager";
import { isBlankOrUndefined } from "../../stringHelperUtil";

/**
 * Defines the properties/members of the push modal props.
 */
interface IPushModalProps {
    /** The confirmation action of the close function. **/
    onClose: () => void;

    /** Function to call when the response is successful. **/
    onSuccess: () => void,

    /** The repository. **/
    repository: LocalGitRepository | undefined;
}

/**
 * Initializes a new instance of the PushModal class.
 * @param props The properties of the push modal.
 * @constructor
 */
const PushModal: React.FC<IPushModalProps> = (props: IPushModalProps) => {

    /** Gets or sets the modal title. **/
    const [title, setTitle] = useState<string>(
        props.repository?.hostPlatform !== RemoteHostPlatform.Local
            ? "Are you sure you want to push changes?"
            : "Do you want to push changes to remote repository?"
    );

    /** Gets or sets the error message. **/
    const [errorMessage, setErrorMessage] = useState<string | undefined>(undefined);

    /** Gets or sets a value indicating whether the push response was successful. **/
    const [successfullyPushed, setSuccessfullyPushed] = useState<boolean | undefined>(undefined);

    /** Gets or sets the flag indicating whether to disable the send button. */
    const [disabledSendButton, setDisableSendButton] = useState(false);

    /** Gets or sets the remote git URL to connect the local to remote git repository. **/
    const [remoteGitUrl, setRemoteGitUrl] = useState<string | undefined>(undefined);

    /** Gets or sets the HEAD branch of the repository branch HEAD. **/
    const [headBranchName, setHeadBranchName] = useState<string | undefined>(undefined);

    /** The navigation function. **/
    const navigate = useNavigate();

    /** Sets the notification modal. */
    const setNotification = useCacheStore(state => state.setNotification);

    /** Gets the logged-in user. */
    const user = useCacheStore(state => state.user);

    /**
     * Handles the push changes request.
     */
    const handlePushChangesRequest = async () => {
        setDisableSendButton(true);
        setTitle("Attempting to push changes. May take a few moments...");
        const request = new GitPushRequest();
        request.repositoryId = props.repository?.id;
        try {
            const response = await statusClient.pushChanges(request);
            if (response.isErrorResponse) {
                setTitle("Could not push changes!");
                setErrorMessage(response.errorMessage);
                setSuccessfullyPushed(false);
                setDisableSendButton(false);
                return;
            }

            setTitle("Successfully Pushed!");
            setErrorMessage(undefined);
            setSuccessfullyPushed(true);
            setDisableSendButton(false);
            props.onSuccess();
        }
        catch (e) {
            setTitle("Could not push changes!");
            setErrorMessage("Check server logs for more information.");
            setSuccessfullyPushed(false);
            setDisableSendButton(false);
            const errorNotification = await handleApiError(e, navigate, "Could not push changes!", "Check console logs for more information.");
            setNotification(errorNotification);
        }
    };

    /**
     * Handles request to push changes to remote repoository from an offline repository.
     */
    const handleConnectRemoteRepositoryRequest = async () => {
        if (isBlankOrUndefined(remoteGitUrl)) {
            setTitle("Cannot connect to remote repository!");
            setErrorMessage("The remote Git url must be set.");
            setSuccessfullyPushed(false);
            setDisableSendButton(false);
            return;
        }

        setDisableSendButton(true);
        setTitle("Attempting to push to remote repository. May take a few moments...");
        const request = new ConnectRemoteRepositoryRequest();
        request.headBranchName = headBranchName;
        request.repositoryId = props.repository?.id;
        request.url = remoteGitUrl;
        request.userId = user?.userId;
        try {
            const response = await configClient.connectRemoteRepository(request);
            if (response.isErrorResponse) {
                setTitle("Could not connect remote repository!");
                setErrorMessage(response.errorMessage);
                setSuccessfullyPushed(false);
                setDisableSendButton(false);
                return;
            }

            setTitle("Successfully connected to remote repository!");
            setErrorMessage(undefined);
            setSuccessfullyPushed(true);
            setDisableSendButton(false);
            if (response.repository) {
                useCacheStore.getState().updateLocalGitRepository(response.repository);
            }

            props.onSuccess();
        } catch (error) {
            setTitle("Could not connect to remote repository!");
            setErrorMessage("Check server logs for more information.");
            setSuccessfullyPushed(false);
            setDisableSendButton(false);
            const errorNotification = await handleApiError(error, navigate, "Could not connect to remote repository!", "Check server logs for more information.");
            setNotification(errorNotification);
        }
    };

    /** Handles the event when the user wants to push changes to a remote repository. */
    const handlePushAction = async () => {
        if (props.repository?.hostPlatform === RemoteHostPlatform.Local) {
            await handleConnectRemoteRepositoryRequest();
            return;
        }

        await handlePushChangesRequest();
    }
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
                    {props.repository?.hostPlatform === RemoteHostPlatform.Local &&
                        <>
                            <input
                                type="text"
                                className="modal-input-field"
                                placeholder="Enter remote URL: (Required)"
                                value={remoteGitUrl}
                                onChange={(e) => setRemoteGitUrl(e.target.value)}
                            />
                            <input
                                type="text"
                                className="modal-input-field"
                                placeholder="Enter HEAD branch name: (Optional)"
                                value={headBranchName}
                                onChange={(e) => setHeadBranchName(e.target.value)}
                            />
                        </>
                    }
                    <div className="modal-actions">
                        <button
                            className="modal-button primary"
                            disabled={disabledSendButton}
                            hidden={successfullyPushed}
                            onClick={handlePushAction}
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