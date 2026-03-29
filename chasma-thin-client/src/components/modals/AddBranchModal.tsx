import React, {useState} from "react";
import {
    AddBranchSimulationEntry,
    AddNewBranchRequest,
    SimulateAddBranchRequest,
    SimulatedAddBranchResult
} from "../../API/ChasmaWebApiClient";
import Checkbox from "../Checkbox";
import {branchClient, dryRunClient} from "../../managers/ApiClientManager";
import { useNavigate } from "react-router-dom";
import { useCacheStore } from "../../managers/CacheManager";
import { handleApiError } from "../../managers/TransactionHandlerManager";

/** The properties to handle commit messages. **/
interface IAddBranchModalProps {
    /** The confirmation action of the close function. **/
    onClose: () => void;

    /** The repository identifier. **/
    repositoryId: string | undefined;

    /** The user identifier. **/
    userId: number | undefined;

    /** Flag indicating whether the mode is safe. **/
    isSafeMode: boolean;

    /** Event to fire when the results are received. **/
    onSuccess: (simulationResults: SimulatedAddBranchResult[]) => void;
}

/**
 * Initializes a new AddBranchModal class.
 * @param props The properties to handle adding branches changes.
 * @constructor
 */
const AddBranchModal: React.FC<IAddBranchModalProps> = (props: IAddBranchModalProps) => {
    /** Gets or sets the branch name the user has input. **/
    const [branchName, setBranchName] = useState<string | undefined>(undefined);

    /** Gets or sets the modal title. **/
    const [title, setTitle] = useState<string>("Add Branch:");

    /** Gets or sets the error message. **/
    const [errorMessage, setErrorMessage] = useState<string | undefined>(undefined);

    /** Gets or sets a value indicating whether the add branch response was successful. **/
    const [successfullyAdded, setSuccessfullyAdded] = useState<boolean | undefined>(undefined);

    /** Gets or sets a value indicating whether the add branch request was sent. **/
    const [addBranchRequestSent, setAddBranchRequestSent] = useState<boolean>(false);

    /** Gets or sets a value indicating whether to check out the branch after successful creation. **/
    const [isCheckingOutBranch, setIsCheckingOutBranch] = useState<boolean>(false);

    /** The navigation function. **/
    const navigate = useNavigate();

   /** Sets the notification modal. */
   const setNotification = useCacheStore(state => state.setNotification);

    /**
     * Handles the event when the user wants to add branches.
     */
    const handleAddBranchOperation = () => {
        if (props.isSafeMode) {
            handleAddBranchDryRunRequest()
            return;
        }

        handleAddBranchRequest();
    }

    /** Handles the request to add branch. **/
    async function handleAddBranchRequest() {
        setTitle("Attempting to add branch...");
        try {
            const request = new AddNewBranchRequest();
            request.repositoryId = props.repositoryId;
            request.branchName = branchName;
            request.userId = props.userId;
            request.isCheckingOutNewBranch = isCheckingOutBranch;
            const response = await branchClient.addNewBranch(request);
            setAddBranchRequestSent(true);
            if (response.isErrorResponse) {
                setTitle("Error adding new branch!")
                setErrorMessage(response.errorMessage);
                setSuccessfullyAdded(false);
                return;
            }

            setErrorMessage(undefined);
            setTitle(`Successfully added ${branchName}`);
            setSuccessfullyAdded(true);
        }
        catch (e) {
            setTitle("Error creating branch!")
            setErrorMessage("Check console logs for more information.");
            setSuccessfullyAdded(false);
            const errorNotification = handleApiError(e, navigate, "Error creating branch!", "Check console logs for more information.");
            setNotification(errorNotification);
        }
        finally {
            setAddBranchRequestSent(false);
            setBranchName(undefined);
            setIsCheckingOutBranch(false);
        }
    }

    /** Handles the request to simulate adding a branch. **/
    async function handleAddBranchDryRunRequest() {
        setTitle("Simulating adding branch...");
        try {
            const request = new SimulateAddBranchRequest();
            const entry = new AddBranchSimulationEntry();
            entry.repositoryId = props.repositoryId;
            entry.branchToAdd = branchName;
            request.entries = [entry];
            const response = await dryRunClient.simulateAddBranch(request);
            setAddBranchRequestSent(true);
            if (response.isErrorResponse) {
                setTitle("Error simulating branch addition!")
                setErrorMessage(response.errorMessage);
                setSuccessfullyAdded(false);
                return;
            }

            setErrorMessage(undefined);
            setTitle("Branch addition simulation complete!");
            setSuccessfullyAdded(true);
            if (response.simulationResults) {
                props.onSuccess(response.simulationResults);
            }
        }
        catch (e) {
            setTitle("Error creating branch!")
            setErrorMessage("Check console logs for more information.");
            setSuccessfullyAdded(false);
            const errorNotification = handleApiError(e, navigate, "Error creating branch!", "Check console logs for more information.");
            setNotification(errorNotification);
        }
        finally {
            setAddBranchRequestSent(false);
            setBranchName(undefined);
        }
    }

    return (
        <>
            <div className="modal-backdrop" onClick={props.onClose}>
                <div className="modal" onClick={(e) => e.stopPropagation()}>
                    <div className="modal-icon-container">
                        {!errorMessage && !successfullyAdded && (
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
                        {!errorMessage && successfullyAdded && (
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
                    {!props.isSafeMode &&
                        <Checkbox
                            label={"Checkout branch after creation"}
                            onBoxChecked={setIsCheckingOutBranch}
                        />
                    }
                    <input className="modal-input-field"
                              placeholder="Enter branch name:"
                              value={branchName}
                              onChange={(e) => setBranchName(e.target.value)} />
                    <br/>
                    <div className="modal-actions">
                        {!successfullyAdded &&
                            <button
                                className="modal-button primary"
                                onClick={handleAddBranchOperation}
                                disabled={addBranchRequestSent}
                            >
                                {props.isSafeMode ? "Simulating " : ""}Add Branch
                            </button>
                        }
                        {successfullyAdded &&
                            <button
                                className="modal-button secondary"
                                onClick={props.onClose}
                            >
                                Close
                            </button>
                        }
                    </div>
                </div>
            </div>
        </>
    )
}

export default AddBranchModal;