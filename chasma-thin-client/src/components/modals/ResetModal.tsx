import React from "react";
import Checkbox from "../Checkbox";
import {GitResetRequest, ResetMode,} from "../../API/ChasmaWebApiClient";
import {statusClient} from "../../managers/ApiClientManager";

/**
 * The members of the Add stash modal.
 */
interface IResetModalProps {
    /** The repository identifier. **/
    repositoryId: string | undefined;

    /** The confirmation action of the close function. **/
    onClose: () => void;
}

/**
 * Initializes a new instance of the ResetModal component.
 * @param props The properties of the reset changes modal.
 * @constructor
 */
const ResetModal: React.FC<IResetModalProps> = (props: IResetModalProps) => {
    /** Gets or sets the error message. **/
    const [errorMessage, setErrorMessage] = React.useState<string | undefined>(undefined);

    /** Gets or sets the revision that the repository was reset to. **/
    const [currentRevision, setCurrentRevision] = React.useState<string | undefined>(undefined);

    /** Gets or sets a value indicating whether the changes were successfully reset. **/
    const [successfullyReset, setSuccessfullyReset] = React.useState<boolean | undefined>(undefined);

    /** Gets or sets the modal title. **/
    const [title, setTitle] = React.useState<string>("Reset Changes");

    /** Gets or sets the reset mode option. **/
    const [resetMode, setResetMode] = React.useState<ResetMode>(ResetMode.Soft);

    /** Gets or sets the revision to reset back to. **/
    const [revParseSpec, setRevParseSpec] = React.useState<string>("");

    /** Handles the event when the user wants to reset to the current changes. **/
    const handleGitResetRequest = async () => {
        setTitle("Attempting to reset changes...");
        try {
            const request = new GitResetRequest();
            request.repositoryId = props.repositoryId;
            request.resetMode = resetMode;
            request.revParseSpec = revParseSpec;
            const response = await statusClient.resetChanges(request);
            if (response.isErrorResponse) {
                setTitle("Error resetting changes");
                setErrorMessage(response.errorMessage);
                return;
            }

            setSuccessfullyReset(true);
            setErrorMessage(undefined);
            setTitle("Successfully reset changes!");
            setCurrentRevision(response.commitMessage);
        }
        catch (e) {
            setTitle("Error resetting changes!");
            setErrorMessage("An error occurred when attempting to reset changes. Review console and internal server logs.");
            console.error(e);
        }
    };
    return (
        <>
            <div className="modal-backdrop" onClick={props.onClose}>
                <div className="modal" onClick={(e) => e.stopPropagation()}>
                    <div className="modal-icon-container">
                        {!errorMessage && !successfullyReset && (
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
                        {!errorMessage && successfullyReset && (
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
                    {currentRevision && <h3 className="modal-message">Successfully reset back to: {currentRevision}</h3>}
                    <div style={{justifySelf: "left", display: "grid",  rowGap: "8px", marginBottom: "8px"}}>
                        <Checkbox
                            label={"Soft"}
                            onBoxChecked={() => setResetMode(ResetMode.Soft)}
                            checked={resetMode === ResetMode.Soft}
                            tooltip="Moves the branch pointed to by HEAD to the specified commit revision."
                        />
                        <Checkbox
                            label={"Mixed"}
                            onBoxChecked={() => setResetMode(ResetMode.Mixed)}
                            checked={resetMode === ResetMode.Mixed}
                            tooltip="Moves the branch pointed to by HEAD to the specified commit object and resets the index to the tree recorded by the commit."
                        />
                        <Checkbox
                            label={"Hard"}
                            onBoxChecked={() => setResetMode(ResetMode.Hard)}
                            checked={resetMode === ResetMode.Hard}
                            tooltip="Moves the branch pointed to by HEAD to the specified commit object, resets the index to the tree recorded by the commit and updates the working directory to match the content of the index."
                        />
                    </div>
                    <br/>
                    <input
                        type="text"
                        className="modal-input-field"
                        placeholder="Commit hash (e.g., d1a17e1): HEAD is default"
                        value={revParseSpec}
                        onChange={(e) => setRevParseSpec(e.target.value)}
                    />
                    <div className="modal-actions">
                        <button className="modal-button primary"
                                hidden={successfullyReset}
                                onClick={handleGitResetRequest}
                        >
                            Reset
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

export default ResetModal;