import { BranchCheckoutMode, GitBranchRequest, GitCheckoutRequest } from "../../API/ChasmaWebApiClient";
import React, { useEffect, useState } from "react";
import { branchClient } from "../../managers/ApiClientManager";
import { useNavigate } from "react-router-dom";
import { useCacheStore } from "../../managers/CacheManager";
import { handleApiError } from "../../managers/TransactionHandlerManager";
import Checkbox from "../Checkbox";
import { createPortal } from "react-dom";

/**
 * The members of the checkout modal.
 */
interface ICheckoutModalProps {
    /** The repository identifier. **/
    repositoryId: string | undefined;

    /** Function to call when the modal is being closed. **/
    onClose: () => void;

    /** Function to call when the response is successful. **/
    onSuccess?: () => void,

    /** The targeted branch to checkout. */
    targetedBranch?: string | undefined;
}

/**
 * Initializes a new CheckoutModal class.
 * @param props The properties to handle checking out changes.
 * @constructor
 */
const CheckoutModal: React.FC<ICheckoutModalProps> = (props: ICheckoutModalProps) => {
    /** Gets or sets the error message. **/
    const [errorMessage, setErrorMessage] = React.useState<string | undefined>(undefined);

    /** Gets or sets a value indicating whether the branch was successfully checked out. **/
    const [successfullyCheckedOut, setSuccessfullyCheckedOut] = React.useState<boolean | undefined>(undefined);

    /** Gets or sets the modal title. **/
    const [title, setTitle] = React.useState<string>(
        props.targetedBranch === undefined
        ? "Select branch to checkout: "
        : `Checkout branch ${props.targetedBranch}?`
    );

    /** Gets or sets the remote branches to checkout. **/
    const [branchesList, setBranchesList] = React.useState<string[] | undefined>([]);

    /** Gets or sets the branch name. **/
    const [branchName, setBranchName] = React.useState<string>("");

    /** Gets or sets a value indicating whether the checkout request was sent. **/
    const [checkoutRequestSent, setCheckoutRequestSent] = useState<boolean>(false);

    /** The navigation function. **/
    const navigate = useNavigate();

    /** Sets the notification modal. */
    const setNotification = useCacheStore(state => state.setNotification);

    /** Gets the logged-in user. */
    const user = useCacheStore(state => state.user);

    /** Gets or sets the branch checkout mode. */
    const [branchCheckoutMode, setBranchCheckoutMode] = useState<BranchCheckoutMode>(BranchCheckoutMode.Default);

    /** Gets or sets the stash message the user has input. **/
    const [stashMessage, setStashMessage] = useState<string | undefined>(undefined);

    /** Handles the event when the user requests to check out changes. **/
    const handleCheckoutChangesRequest = async () => {
        if (branchCheckoutMode === BranchCheckoutMode.StashOnly && (!stashMessage || stashMessage.length === 0)) {
            setErrorMessage("Must include a stash message.");
            setTitle("Could not check out branch!");
            return;
        }

        const request = new GitCheckoutRequest();
        request.repositoryId = props.repositoryId;
        request.branchName = props.targetedBranch !== undefined ? props.targetedBranch : branchName;
        request.userId = user?.userId;
        request.checkoutMode = branchCheckoutMode;
        request.stashMessage = stashMessage;
        try {
            const response = await branchClient.checkoutBranch(request);
            if (response.isErrorResponse) {
                setErrorMessage(response.errorMessage);
                setTitle("Could not check out branch!")
                return;
            }

            setTitle("Check out successful!");
            setErrorMessage(undefined);
            setSuccessfullyCheckedOut(true);
            if (props.onSuccess) {
                props.onSuccess();
            }
        }
        catch (e) {
            setTitle("Could not check out branch!")
            setErrorMessage("Check console logs for more information.");
            const errorNotification = await handleApiError(e, navigate, "Could not check out branch!", "Check console logs for more information.");
            setNotification(errorNotification);
        }
        finally {
            setCheckoutRequestSent(true);
        }
    };

    /** Fetches the local and remote branches associated with this repository. **/
    async function fetchAssociatedBranches() {
        const request = new GitBranchRequest();
        request.repositoryId = props.repositoryId;
        try {
            const response = await branchClient.getBranches(request);
            if (response.isErrorResponse) {
                setErrorMessage(response.errorMessage);
                setBranchesList([]);
                setTitle("Cannot get branches!")
                return;
            }

            setBranchesList(response.branchNames);
            if (!response.branchNames) {
                setErrorMessage("Cannot get branches for this repository. Ensure there are branches created!");
                setTitle("Cannot get branches!");
                return;
            }

            if (response.branchNames.length > 0) {
                setBranchName(response.branchNames[0]);
            }

            setErrorMessage(undefined);
        }
        catch (e) {
            setTitle("Cannot get branches!")
            setErrorMessage("Error occurred while fetching branches. Check console logs.");
            const errorNotification = await handleApiError(e, navigate, "Cannot get branches!", "Error occurred while fetching branches. Check console logs.");
            setNotification(errorNotification);
        }
    }

    useEffect(() => {
        if (!props.targetedBranch) {
            fetchAssociatedBranches().catch(e => console.error(e));
        }
    }, [props.targetedBranch]);
    return createPortal (
        <>
            <div className="modal-backdrop" onClick={props.onClose}>
                <div className="modal" onClick={(e) => e.stopPropagation()}>
                    <div className="modal-icon-container">
                        {!errorMessage && !successfullyCheckedOut && (
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
                        {!errorMessage && successfullyCheckedOut && (
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
                    <div style={{ justifySelf: "left", display: "grid", rowGap: "8px", marginBottom: "8px" }}>
                        <Checkbox
                            label={"Default"}
                            onBoxChecked={() => setBranchCheckoutMode(BranchCheckoutMode.Default)}
                            checked={branchCheckoutMode === BranchCheckoutMode.Default}
                            tooltip="The default git checkout behavior."
                        />
                        <Checkbox
                            label={"Stash Only"}
                            onBoxChecked={() => setBranchCheckoutMode(BranchCheckoutMode.StashOnly)}
                            checked={branchCheckoutMode === BranchCheckoutMode.StashOnly}
                            tooltip="Save your current workspace and start fresh."
                        />
                        <Checkbox
                            label={"Keep Changes"}
                            onBoxChecked={() => setBranchCheckoutMode(BranchCheckoutMode.KeepChanges)}
                            checked={branchCheckoutMode === BranchCheckoutMode.KeepChanges}
                            tooltip="Transfer your changes from your current branch to the branch you're switching to."
                        />
                        <Checkbox
                            label={"Discard Changes"}
                            onBoxChecked={() => setBranchCheckoutMode(BranchCheckoutMode.DiscardAll)}
                            checked={branchCheckoutMode === BranchCheckoutMode.DiscardAll}
                            tooltip="Discard all your current changes in your workspace."
                        />
                    </div>
                    <br />
                    {branchesList && branchesList.length > 0 && props.targetedBranch === undefined && (
                        <select value={branchName}
                            onChange={(e) => setBranchName(e.target.value)}
                            className="modal-input-field"
                        >
                            {branchesList.map((branch) => (
                                <option key={branch} value={branch}>{branch}</option>
                            ))}
                        </select>
                    )}
                    <br />
                    {branchCheckoutMode === BranchCheckoutMode.StashOnly &&
                        <>
                            <input className="modal-input-field"
                                placeholder="Enter Stash Title:"
                                value={stashMessage}
                                onChange={(e) => setStashMessage(e.target.value)} />

                            <br />
                        </>
                    }
                    <div className="modal-actions">
                        <button className="modal-button primary"
                            disabled={checkoutRequestSent}
                            onClick={handleCheckoutChangesRequest}
                        >
                            Checkout
                        </button>
                        <button className="modal-button secondary"
                            onClick={props.onClose}
                        >
                            Close
                        </button>
                    </div>
                </div>
            </div>
        </>,
        document.body
    )
}

export default CheckoutModal;