import React, {useEffect, useState} from "react";
import {GitBranchRequest, GitMergeRequest, RepositoryStatusClient} from "../../API/ChasmaWebApiClient";
import {apiBaseUrl} from "../../environmentConstants";

/** The status client for the web API. **/
const statusClient = new RepositoryStatusClient(apiBaseUrl);

/** Defines the properties of the merge branches modal. **/
interface IMergeModal {
    /** The confirmation action of the close function. **/
    onClose: () => void;

    /** The repository identifier. **/
    repositoryId: string | undefined;
    
    /** The user identifier. **/
    userId: number | undefined;
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
            const response = await statusClient.mergeBranch(request);
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
            console.error(e);
            setErrorMessage("Error occurred while attempting to merge changes. Check error logs.");
            setTitle("Error finishing merge!");
            setSuccessfullyMerged(false);
        }
        finally {
            await fetchAssociatedBranches();
        }
    };

    /** Fetches the local and remote branches associated with this repository. **/
    async function fetchAssociatedBranches() {
        const request = new GitBranchRequest();
        request.repositoryId = props.repositoryId;
        try {
            const response = await statusClient.getBranches(request);
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
            console.error(e);
            setErrorMessage("Error occurred while fetching branches. Check console logs.");
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
                    {errorMessage && <h3 className="modal-message">{errorMessage}</h3>}
                    {branchesList && branchesList.length > 0 && (
                        <div hidden={successfullyMerged}>
                            <label style={{float: "left"}}>Choose source branch:</label>
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
                                onClick={handleMergeBranches}
                        >
                            Merge
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