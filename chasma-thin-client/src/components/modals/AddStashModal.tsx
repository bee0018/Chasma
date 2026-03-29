import React from "react";
import Checkbox from "../Checkbox";
import {
    AddStashRequest,
    StashModifiers
} from "../../API/ChasmaWebApiClient";
import {useCacheStore} from "../../managers/CacheManager";
import {stashClient} from "../../managers/ApiClientManager";
import { useNavigate } from "react-router-dom";
import { handleApiError } from "../../managers/TransactionHandlerManager";

/**
 * The members of the Add stash modal.
 */
interface IAddStashModalProps {
    /** The repository identifier. **/
    repositoryId: string | undefined;

    /** The confirmation action of the close function. **/
    onClose: () => void;
}

/**
 * Initializes a new instance of the AddStashModal component.
 * @param props The properties of the add stash modal.
 * @constructor
 */
const AddStashModal: React.FC<IAddStashModalProps> = (props: IAddStashModalProps) => {
    /** Gets or sets the error message. **/
    const [errorMessage, setErrorMessage] = React.useState<string | undefined>(undefined);

    /** Gets or sets a value indicating whether the changes were successfully stashed. **/
    const [successfullyStashed, setSuccessfullyStashed] = React.useState<boolean | undefined>(undefined);

    /** Gets or sets the modal title. **/
    const [title, setTitle] = React.useState<string>("Stash Options");

    /** Gets or sets the stash option. **/
    const [stashOption, setStashOption] = React.useState<StashModifiers>(StashModifiers.Default);

    /** Gets or sets the stash message the user has input. **/
    const [stashMessage, setStashMessage] = React.useState<string | undefined>(undefined);

    /** The logged-in user. **/
    const user = useCacheStore((state) => state.user);

    /** The navigation function. **/
    const navigate = useNavigate();

   /** Sets the notification modal. */
   const setNotification = useCacheStore(state => state.setNotification);

    /** Handles the event when the user wants to stash current changes. **/
    const handleAddStashRequest = async () => {
        setTitle("Attempting to add stash...");
        try {
            const request = new AddStashRequest();
            request.repositoryId = props.repositoryId;
            request.stashModifier = stashOption;
            request.userId = user?.userId;
            request.message = stashMessage;
            const response = await stashClient.gitStash(request);
            if (response.isErrorResponse) {
                setTitle("Error stashing changes");
                setErrorMessage(response.errorMessage);
                return;
            }

            setSuccessfullyStashed(true);
            setErrorMessage(undefined);
            setTitle("Successfully added stash!");
        }
        catch (e) {
            setTitle("Error adding stash request");
            setErrorMessage("An error occurred when attempting to stash changes. Review console and internal server logs.");
            const errorNotification = handleApiError(e, navigate, "Error adding stash request!", "An error occurred when attempting to stash changes. Review console and internal server logs.");
            setNotification(errorNotification);
        }
    };
    return (
        <>
            <div className="modal-backdrop" onClick={props.onClose}>
                <div className="modal" onClick={(e) => e.stopPropagation()}>
                    <div className="modal-icon-container">
                        {!errorMessage && !successfullyStashed && (
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
                        {!errorMessage && successfullyStashed && (
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
                    <div style={{justifySelf: "left", display: "grid",  rowGap: "8px", marginBottom: "8px"}}>
                        <Checkbox
                            label={"Default"}
                            onBoxChecked={() => setStashOption(StashModifiers.Default)}
                            checked={stashOption === StashModifiers.Default}
                            tooltip={"Default stashing behavior."}
                        />
                        <Checkbox
                            label={"Keep In Index"}
                            onBoxChecked={() => setStashOption(StashModifiers.KeepIndex)}
                            checked={stashOption === StashModifiers.KeepIndex}
                            tooltip={"All changes already added to the index are left intact in the working directory."}
                        />
                        <Checkbox
                            label={"Include Untracked"}
                            onBoxChecked={() => setStashOption(StashModifiers.IncludeUntracked)}
                            checked={stashOption === StashModifiers.IncludeUntracked}
                            tooltip={"All untracked files are also stashed and then cleaned up from the working directory."}
                        />
                        <Checkbox
                            label={"Include Ignored"}
                            onBoxChecked={() => setStashOption(StashModifiers.IncludeIgnored)}
                            checked={stashOption === StashModifiers.IncludeIgnored}
                            tooltip={"All ignored files are also stashed and then cleaned up from the working directory."}
                        />
                    </div>
                    <input className="modal-input-field"
                              placeholder="Enter Stash Title:"
                              value={stashMessage}
                              onChange={(e) => setStashMessage(e.target.value)} />
                    <div className="modal-actions">
                        <button className="modal-button primary"
                                hidden={successfullyStashed}
                                onClick={handleAddStashRequest}
                        >
                            Stash
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

export default AddStashModal;