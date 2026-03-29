import React from "react";
import Checkbox from "../Checkbox";
import {
    ApplyStashRequest,
    StashApplyModifiers,
} from "../../API/ChasmaWebApiClient";
import {stashClient} from "../../managers/ApiClientManager";
import { useNavigate } from "react-router-dom";
import { useCacheStore } from "../../managers/CacheManager";
import { handleApiError } from "../../managers/TransactionHandlerManager";

/**
 * The members of the Add stash modal.
 */
interface IApplyStashModalProps {
    /** The repository identifier. **/
    repositoryId: string | undefined;

    /** The stash index. **/
    stashIndex: number | undefined;

    /** The confirmation action of the close function. **/
    onClose: () => void;

    /** The action to invoke when the stash is successfully applied. **/
    onSuccess: () => void;
}

/**
 * Initializes a new instance of the ApplyStashModal component.
 * @param props The properties of the apply stash modal.
 * @constructor
 */
const ApplyStashModal: React.FC<IApplyStashModalProps> = (props: IApplyStashModalProps) => {
    /** Gets or sets the error message. **/
    const [errorMessage, setErrorMessage] = React.useState<string | undefined>(undefined);

    /** Gets or sets a value indicating whether the changes were successfully applied. **/
    const [successfullyAppliedStashed, setSuccessfullyAppliedStashed] = React.useState<boolean | undefined>(undefined);

    /** Gets or sets the modal title. **/
    const [title, setTitle] = React.useState<string>("Apply Stash Options");

    /** Gets or sets the apply stash option. **/
    const [applyStashOption, setApplyStashOption] = React.useState<StashApplyModifiers>(StashApplyModifiers.Default);

    /** The navigation function. **/
    const navigate = useNavigate();

   /** Sets the notification modal. */
   const setNotification = useCacheStore(state => state.setNotification);

    /** Handles the event when the user wants to apply the stash to the current changes. **/
    const handleApplyStashRequest = async () => {
        setTitle("Attempting to apply stash...");
        try {
            const request = new ApplyStashRequest();
            request.repositoryId = props.repositoryId;
            request.applyStashModifier = applyStashOption;
            request.stashIndex = props.stashIndex;
            const response = await stashClient.applyStash(request);
            if (response.isErrorResponse) {
                setTitle(`Error applying stash ${props.stashIndex}`);
                setErrorMessage(response.errorMessage);
                return;
            }

            setSuccessfullyAppliedStashed(true);
            setErrorMessage(undefined);
            setTitle("Successfully applied stash!");
            props.onSuccess()
        }
        catch (e) {
            setTitle("Error applying stash!");
            setErrorMessage("An error occurred when attempting to stash changes. Review console and internal server logs.");
            const errorNotification = handleApiError(e, navigate, "Error applying stash!", "An error occurred when attempting to stash changes. Review console and internal server logs.");
            setNotification(errorNotification);
        }
    };
    return (
        <>
            <div className="modal-backdrop" onClick={props.onClose}>
                <div className="modal" onClick={(e) => e.stopPropagation()}>
                    <div className="modal-icon-container">
                        {!errorMessage && !successfullyAppliedStashed && (
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
                        {!errorMessage && successfullyAppliedStashed && (
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
                            onBoxChecked={() => setApplyStashOption(StashApplyModifiers.Default)}
                            checked={applyStashOption === StashApplyModifiers.Default}
                            tooltip={"Will apply the stash and result in an index with conflicts if any arise."}
                        />
                        <Checkbox
                            label={"Reinstate Index"}
                            onBoxChecked={() => setApplyStashOption(StashApplyModifiers.ReinstateIndex)}
                            checked={applyStashOption === StashApplyModifiers.ReinstateIndex}
                            tooltip={"In case any conflicts arise, this will not apply the stash."}
                        />
                    </div>
                    <div className="modal-actions">
                        <button className="modal-button primary"
                                hidden={successfullyAppliedStashed}
                                onClick={handleApplyStashRequest}
                        >
                            Apply
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

export default ApplyStashModal;