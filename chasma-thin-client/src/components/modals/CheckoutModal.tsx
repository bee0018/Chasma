import {GitBranchRequest, GitCheckoutRequest, RepositoryStatusClient} from "../../API/ChasmaWebApiClient";
import React, {useState} from "react";
import {apiBaseUrl} from "../../environmentConstants";

/** The status client for the web API. **/
const statusClient = new RepositoryStatusClient(apiBaseUrl)

/**
 * The members of the checkout modal.
 */
interface ICheckoutModalProps {
    /** The repository identifier. **/
    repositoryId: string | undefined;
    /** Function to call when the modal is being closed. **/
    onClose: () => void;
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
    const [title, setTitle] = React.useState<string>("Select branch to checkout: ");

    /** Gets or sets the remote branches to checkout. **/
    const [branchesList, setBranchesList] = React.useState<string[] | undefined>([]);

    /** Gets or sets the branch name. **/
    const [branchName, setBranchName] = React.useState<string>("");

    /** Gets or sets a value indicating whether the checkout request was sent. **/
    const [checkoutRequestSent, setCheckoutRequestSent] = useState<boolean>(false);

    /** Handles the event when the user requests to check out changes. **/
    const handleCheckoutChangesRequest = async () => {
        const request = new GitCheckoutRequest();
        request.repositoryId = props.repositoryId;
        request.branchName = branchName;
        try {
            const response = await statusClient.checkoutBranch(request);
            if (response.isErrorResponse) {
                setErrorMessage(response.errorMessage);
                setTitle("Could not check out branch!")
                return;
            }

            setTitle("Check out successful!");
            setErrorMessage(undefined);
            setSuccessfullyCheckedOut(true);
        }
        catch (e) {
            console.error(e);
            setTitle("Could not check out branch!")
            setErrorMessage("Check console logs for more information.");
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
            const response = await statusClient.getBranches(request);
            if (response.isErrorResponse) {
                setErrorMessage(response.errorMessage);
                setBranchesList([]);
                setTitle("Cannot get branches!")
                return;
            }

            setBranchesList(response.branchNames);
            setErrorMessage(undefined);
        }
        catch (e) {
            console.error(e);
            setTitle("Cannot get branches!")
            setErrorMessage("Error occurred while fetching branches. Check console logs.");
        }
    }

    fetchAssociatedBranches().catch(e => console.error(e));
    return (
        <>
            <div className="modal-backdrop" onClick={props.onClose}>
                <div className="commit-modal" onClick={(e) => e.stopPropagation()}>
                    <div className="commit-modal-icon">
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
                    <h2 className="commit-modal-title"
                        style={{marginTop: "-30px"}}>{title}</h2>
                    {errorMessage && <h3 className="commit-modal-message">{errorMessage}</h3>}
                    {branchesList && branchesList.length > 0 && (
                        <select value={branchName}
                                onChange={(e) => setBranchName(e.target.value)}
                                className="input-field"
                                style={{width: "-webkit-fill-available"}}
                        >
                            {branchesList.map((branch) => (
                                <option key={branch} value={branch}>{branch}</option>
                            ))}
                        </select>
                    )}
                    <br/>
                    <div>
                        <button className="commit-modal-button"
                                style={{marginRight: "50px"}}
                                disabled={checkoutRequestSent}
                                onClick={handleCheckoutChangesRequest}
                        >
                            Checkout
                        </button>
                        <button className="commit-modal-button"
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

export default CheckoutModal;