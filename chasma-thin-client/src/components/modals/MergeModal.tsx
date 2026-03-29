import React, {useEffect, useState} from "react";
import {
    GitBranchRequest,
    GitMergeRequest,
    MergeSimulationEntry,
    SimulateBranchMergeRequest,
    SimulatedMergeResult
} from "../../API/ChasmaWebApiClient";
import {branchClient, dryRunClient} from "../../managers/ApiClientManager";
import { useNavigate } from "react-router-dom";
import { useCacheStore } from "../../managers/CacheManager";
import { handleApiError } from "../../managers/TransactionHandlerManager";

/** Defines the properties of the merge branches modal. **/
interface IMergeModal {
    /** The confirmation action of the close function. **/
    onClose: () => void;

    /** The repository identifier. **/
    repositoryId: string | undefined;
    
    /** The user identifier. **/
    userId: number | undefined;

    /** Flag indicating whether the mode is safe. **/
    isSafeMode: boolean;

    /** Event to fire when the results are received. **/
    onSuccess: (simulationResults: SimulatedMergeResult[]) => void;
}

/**
 * Initializes a new instance of the MergeModal class.
 * @param props The properties of the merge modal.
 * @constructor
 */
const MergeModal: React.FC<IMergeModal> = (props: IMergeModal) => {
    /** Gets or sets the modal title. **/
    const [title, setTitle] = useState<string>("Merge Changes:");

    /** Gets or sets the error message. **/
    const [errorMessage, setErrorMessage] = useState<string | undefined>(undefined);

    /** Gets or sets a value indicating whether the merge response was successful. **/
    const [successfullyMerged, setSuccessfullyMerged] = useState<boolean | undefined>(undefined);

    /** Gets or sets the destination branch. **/
    const [destinationBranch, setDestinationBranch] = useState<string | undefined>(undefined);

    /** Gets or sets the working branch name. **/
    const [workingBranchName, setWorkingBranchName] = useState<string | undefined>(undefined);

    /** Gets or sets the remote branches to merge. **/
    const [branchesList, setBranchesList] = React.useState<string[] | undefined>([]);

    /** The navigation function. **/
    const navigate = useNavigate();

   /** Sets the notification modal. */
   const setNotification = useCacheStore(state => state.setNotification);

   /** Gets or sets the flag indicating whether to disable the send button. */
    const [disabledSendButton, setDisableSendButton] = useState(false);

    /**
     * Handles the event when the user wants to merge changes.
     */
    const handleMergeOperation = () => {
        setDisableSendButton(true);
        if (props.isSafeMode) {
            handleMergeDryRun();
            setDisableSendButton(false);
            return;
        }

        handleMergeBranches();
        setDisableSendButton(false);
    }

    /**
     * Handles the event when a user intends to merge one branch into another.
     */
    const handleMergeBranches = async () => {
        setTitle("Attempting to merge changes. May take a few moments...");
        const request = new GitMergeRequest();
        request.repositoryId = props.repositoryId;
        request.destinationBranch = destinationBranch
        request.sourceBranch = workingBranchName;
        request.userId = props.userId;
        try {
            const response = await branchClient.mergeBranch(request);
            if (response.isErrorResponse) {
                setTitle("Error creating merging changes!");
                setErrorMessage(response.errorMessage);
                setSuccessfullyMerged(false);
                return;
            }

            setTitle(`${workingBranchName} was successfully merged into ${destinationBranch}`);
            setErrorMessage(undefined);
            setSuccessfullyMerged(true);
        }
        catch (e) {
            setErrorMessage("Error occurred while attempting to merge changes. Check error logs.");
            setTitle("Error finishing merge!");
            setSuccessfullyMerged(false);
            const errorNotification = handleApiError(e, navigate, "Error finishing merge!", "Error occurred while attempting to merge changes. Check error logs.");
            setNotification(errorNotification);
        }
        finally {
            await fetchAssociatedBranches();
        }
    };

    /**
     * Handles the event when the user wants to dry run a merge.
     */
    const handleMergeDryRun = async () => {
        setTitle("Simulating merge operation. May take a few moments...");
        const request = new SimulateBranchMergeRequest();
        const mergeEntry = new MergeSimulationEntry();
        mergeEntry.repositoryId = props.repositoryId;
        mergeEntry.destinationBranch = destinationBranch
        mergeEntry.sourceBranch = workingBranchName;
        mergeEntry.userId = props.userId;
        request.mergeEntries = [mergeEntry];
        try {
            const response = await dryRunClient.simulateMergeBranches(request);
            if (response.isErrorResponse) {
                setTitle("Error simulating merge operation");
                setErrorMessage(response.errorMessage);
                setSuccessfullyMerged(false);
                return;
            }

            setTitle("Merge simulation complete.");
            setErrorMessage(undefined);
            setSuccessfullyMerged(true);
            if (response.simulationResults) {
                props.onSuccess(response.simulationResults);
            }
        }
        catch (e) {
            setErrorMessage("Error occurred while attempting to simulate merging changes. Check error logs.");
            setTitle("Error simulating merge operation");
            setSuccessfullyMerged(false);
            const errorNotification = handleApiError(e, navigate, "Error simulating merge operation!", "Error occurred while attempting to simulate merging changes. Check error logs.");
            setNotification(errorNotification);
        }
    }

    /** Fetches the local and remote branches associated with this repository. **/
    async function fetchAssociatedBranches() {
        const request = new GitBranchRequest();
        request.repositoryId = props.repositoryId;
        try {
            const response = await branchClient.getBranches(request);
            if (response.isErrorResponse) {
                setBranchesList([]);
                return;
            }

            setBranchesList(response.branchNames);
            if (response.branchNames && response.branchNames.length > 0){
                const branch = response.branchNames[0];
                setWorkingBranchName(branch);
                setDestinationBranch(branch);
            }
        }
        catch (e) {
            setErrorMessage("Error occurred while fetching branches. Check console logs.");
            const errorNotification = handleApiError(e, navigate, "Could not fetch branches!", "Error occurred while fetching branches. Check console logs.");
            setNotification(errorNotification);
        }
    }

    useEffect(() => {
        fetchAssociatedBranches().catch(e => console.error(e));
    }, []);

    return (
        <>
            <div className="modal-backdrop" onClick={props.onClose}>
                <div className="modal" onClick={(e) => e.stopPropagation()}>
                    <div className="modal-icon-container">
                        {!errorMessage && !successfullyMerged && (
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
                        {!errorMessage && successfullyMerged && (
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
                    <span><code>{workingBranchName}</code> ➜ <code>{destinationBranch}</code></span>
                    {errorMessage && <h3 className="modal-message">{errorMessage}</h3>}
                    {branchesList && branchesList.length > 0 && (
                        <div hidden={successfullyMerged}>
                            <label style={{float: "left", marginTop: "30px"}}>Choose source branch:</label>
                            <select value={workingBranchName}
                                    onChange={(e) => setWorkingBranchName(e.target.value)}
                                    className="modal-input-field"
                            >
                                {branchesList.map((branch) => (
                                    <option key={branch} value={branch}>{branch}</option>
                                ))}
                            </select>
                            <br/>
                            <label style={{float: "left"}}>Choose destination branch to merge into:</label>
                            <select value={destinationBranch}
                                    onChange={(e) => setDestinationBranch(e.target.value)}
                                    className="modal-input-field"
                            >
                                {branchesList.map((branch) => (
                                    <option key={branch} value={branch}>{branch}</option>
                                ))}
                            </select>
                        </div>
                    )}
                    <div className="modal-actions">
                        <button className="modal-button primary"
                                hidden={successfullyMerged}
                                disabled={disabledSendButton}
                                onClick={handleMergeOperation}
                        >
                            {props.isSafeMode ? "Simulate ": ""}Merge
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

export default MergeModal;