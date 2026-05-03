import React, {useState} from "react";
import {ApplicationUser, GitPullRequest, PullSimulationEntry, SimulatedGitPullResult, SimulateGitPullRequest} from "../../API/ChasmaWebApiClient";
import {dryRunClient, statusClient} from "../../managers/ApiClientManager";
import { useNavigate } from "react-router-dom";
import { useCacheStore } from "../../managers/CacheManager";
import { handleApiError } from "../../managers/TransactionHandlerManager";

/**
 * Defines the properties/members of the pull modal props.
 */
interface IPullModalProps {
    /** The confirmation action of the close function. **/
    onClose: () => void;

    /** Function to call when the dry run response is successful. **/
    onSimulationSuccess: (simResults: SimulatedGitPullResult[]) => void;

    /** The repository identifier. **/
    repositoryId: string | undefined;

    /** Flag indicating whether the system is in safe mode. */
    isSafeMode: boolean;

    /** The logged-in user. **/
    user: ApplicationUser | null;
}

/**
 * Initializes a new instance of the PullModal class.
 * @param props The properties of the pull modal.
 * @constructor
 */
const PullModal: React.FC<IPullModalProps> = (props: IPullModalProps) => {

    /** Gets or sets the modal title. **/
    const [title, setTitle] = useState<string>("Are you sure you want to pull changes?");

    /** Gets or sets the error message. **/
    const [errorMessage, setErrorMessage] = useState<string | undefined>(undefined);

    /** Gets or sets a value indicating whether the pull response was successful. **/
    const [successfullyPulled, setSuccessfullyPulled] = useState<boolean | undefined>(undefined);

    /** Gets or sets the flag indicating whether to disable the send button. */
    const [disabledSendButton, setDisableSendButton] = useState(false);

    /** The navigation function. **/
    const navigate = useNavigate();

   /** Sets the notification modal. */
   const setNotification = useCacheStore(state => state.setNotification);

    /**
     * Handles the event when the user wants to pull changes.
     */
    const handlePullRequestOperation = () => {
        setDisableSendButton(true);
        if (props.isSafeMode) {
            handleGitPullRequestDryRun();
            return;
        }

        handlePullRequest();
        setDisableSendButton(false);
    }

    /**
     * Handles the event when the user wants to pull latest changes.
     */
    const handlePullRequest = async () => {
        setTitle("Pulling changes...");
        const request = new GitPullRequest();
        request.repositoryId = props.repositoryId;
        request.email = props.user?.email;
        request.userId = props.user?.userId;
        try {
            const response = await statusClient.pullChanges(request);
            if (response.isErrorResponse) {
                setTitle("Could not pull changes!");
                setSuccessfullyPulled(false);
                setErrorMessage(response.errorMessage);
                return;
            }

            setSuccessfullyPulled(true);
            setTitle("Successfully pull changes!");
        } catch (e) {
            setTitle("Could not pull changes!");
            setSuccessfullyPulled(false);
            setErrorMessage("An internal server error has occurred. Review logs.");
            const errorNotification = handleApiError(e, navigate, "Could not pull changes!", "An internal server error has occurred. Review logs.");
            setNotification(errorNotification);
        }
    };

    /**
     * Handles the event when the user wants to simulate pulling changes.
     */
    const handleGitPullRequestDryRun = async () => {
        setTitle("Performing pull simulation. Please wait...");
        const request = new SimulateGitPullRequest();
        const entry = new PullSimulationEntry();
        entry.repositoryId = props.repositoryId;
        entry.branchToPull = "";
        request.entries = [entry];
        try {
            const response = await dryRunClient.simulateGitPull(request);
            if (response.isErrorResponse) {
                setTitle("Failed to perform pull simulation!");
                setErrorMessage(response.errorMessage);
                setSuccessfullyPulled(false);
                return;
            }

            if (!response.pullResults) {
                setTitle("Failed to perform pull simulation!");
                setErrorMessage("There were no simulation results.");
                setSuccessfullyPulled(false);
                return;
            }

            setTitle("Successfully performed pull simulation!");
            setSuccessfullyPulled(true);
            props.onSimulationSuccess(response.pullResults);
        } catch (e) {
            setTitle("Error during pull simulation!");
            setErrorMessage("An internal server error has occurred. Review logs.");
            const errorNotification = handleApiError(e, navigate, "Could not simulate pull simulation!", "An internal server error has occurred. Review logs.");
            setNotification(errorNotification);
        }
    }

    return (
        <>
            <div className="modal-backdrop" onClick={props.onClose}>
                <div className="modal" onClick={(e) => e.stopPropagation()}>
                    <div className="modal-icon-container">
                        {!errorMessage && !successfullyPulled && (
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
                        {!errorMessage && successfullyPulled && (
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
                            hidden={successfullyPulled}
                            onClick={handlePullRequestOperation}
                        >
                            {props.isSafeMode ? "Simulate " : ""}Pull
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

export default PullModal;