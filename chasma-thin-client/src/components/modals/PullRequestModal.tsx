import React, {useState} from "react";
import {CreatePRRequest, GitBranchRequest, RepositoryStatusClient} from "../../API/ChasmaWebApiClient";

/** The status client for the web API. **/
const statusClient = new RepositoryStatusClient();

/** Defines the properties of the pull request modal. **/
interface IPullRequestProps {
    /** The confirmation action of the close function. **/
    onClose: () => void;

    /** The repository identifier. **/
    repositoryId: string | undefined;

    /** The repository name. **/
    repoName: string | undefined;
}

/**
 * Initializes a new instance of the PullRequestModal class.
 * @param props The properties of the pull request modal.
 * @constructor
 */
const PullRequestModal: React.FC<IPullRequestProps> = (props: IPullRequestProps) => {
    /** Gets or sets the modal title. **/
    const [title, setTitle] = useState<string>("Create Pull Request");

    /** Gets or sets the error message. **/
    const [errorMessage, setErrorMessage] = useState<string | undefined>(undefined);

    /** Gets or sets a value indicating whether the pull request response was successful. **/
    const [successfullyCreated, setSuccessfullyCreated] = useState<boolean | undefined>(undefined);

    /** Gets or sets the pull request description. **/
    const [pullRequestDescription, setPullRequestDescription] = useState<string | undefined>(undefined);

    /** Gets or sets the pull request title. **/
    const [pullRequestTitle, setPullRequestTitle] = useState<string | undefined>(undefined);

    /** Gets or sets the destination branch. **/
    const [destinationBranch, setDestinationBranch] = useState<string | undefined>(undefined);

    /** Gets or sets the working branch name. **/
    const [workingBranchName, setWorkingBranchName] = useState<string | undefined>(undefined);

    /** Gets or sets the remote branches to checkout. **/
    const [branchesList, setBranchesList] = React.useState<string[] | undefined>([]);

    /** Gets or sets the pull request URL. **/
    const [pullRequestUrl, setPullRequestUrl] = useState<string | undefined>(undefined);

    /**
     * Handles the event when a user intends to create a pull request on GitHub.
     */
    const handleCreatePrRequest = async () => {
        setTitle("Creating pull request. May take a few moments...");
        const request = new CreatePRRequest();
        request.repositoryId = props.repositoryId;
        request.pullRequestBody = pullRequestDescription;
        request.pullRequestTitle = pullRequestTitle;
        request.repositoryName = props.repoName;
        request.destinationBranchName = destinationBranch
        request.workingBranchName = workingBranchName;
        try {
            const response = await statusClient.createPullRequest(request);
            if (response.isErrorResponse) {
                setTitle("Error creating Pull Request");
                setErrorMessage(response.errorMessage);
                setSuccessfullyCreated(false);
                return;
            }

            setTitle(`Pull Request ${response.pullRequestId} successfully created at ${response.timeStamp}`);
            setErrorMessage(undefined);
            setSuccessfullyCreated(true);
            setPullRequestUrl(response.pullRequestUrl);
        }
        catch (e) {
            console.error(e);
            setErrorMessage("Error occurred while creating Pull Request. Check error logs.");
            setTitle("Error creating Pull Request");
            setSuccessfullyCreated(false);
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
        }
        catch (e) {
            console.error(e);
            setErrorMessage("Error occurred while fetching branches. Check console logs.");
        }
    }

    fetchAssociatedBranches().catch(e => console.error(e));
    return (
        <>
            <div className="modal-backdrop" onClick={props.onClose}>
                <div className="commit-modal" onClick={(e) => e.stopPropagation()}>
                    <div className="commit-modal-icon">
                        {!errorMessage && !successfullyCreated && (
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
                        {!errorMessage && successfullyCreated && (
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
                        <div>
                            <label style={{float: "left"}}>Choose working branch:</label>
                            <select value={workingBranchName}
                                    onChange={(e) => setWorkingBranchName(e.target.value)}
                                    className="input-field"
                                    style={{width: "-webkit-fill-available"}}
                            >
                                {branchesList.map((branch) => (
                                    <option key={branch} value={branch}>{branch}</option>
                                ))}
                            </select>
                            <br/>
                            <label style={{float: "left"}}>Choose destination branch to merge into:</label>
                            <select value={destinationBranch}
                                    onChange={(e) => setDestinationBranch(e.target.value)}
                                    className="input-field"
                                    style={{width: "-webkit-fill-available"}}
                            >
                                {branchesList.map((branch) => (
                                    <option key={branch} value={branch}>{branch}</option>
                                ))}
                            </select>
                        </div>
                )}
                    <br/>
                    <input
                        type="text"
                        className="input-field"
                        placeholder="Pull Request Title"
                        value={pullRequestTitle}
                        onChange={(e) => setPullRequestTitle(e.target.value)}
                        style={{width: "100%"}}
                        required
                    />
                    <textarea className="input-area"
                              placeholder="Enter Pull Request Description:"
                              value={pullRequestDescription}
                              onChange={(e) => setPullRequestDescription(e.target.value)} />
                    <br/>
                    {successfullyCreated && pullRequestUrl && (
                        <div className="input-field"
                             onClick={() => window.open(`${pullRequestUrl}`, "_blank")}>
                            GitHub Pull Request Url: {pullRequestUrl}
                        </div>
                    )}
                    <br/>
                    <div>
                        <button className="commit-modal-button"
                                style={{marginRight: "50px"}}
                                disabled={successfullyCreated}
                                onClick={handleCreatePrRequest}
                        >
                            Create Pull Request
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

export default PullRequestModal;