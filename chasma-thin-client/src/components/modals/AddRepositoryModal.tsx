import React, {useState} from "react";

/**
 * The members of the add repository modal.
 */
interface IAddRepositoryModalProps {
    /** Function to call when the modal is being closed. **/
    onClose: () => void,

    /** Function to call when the user has selected a directory to add. **/
    onRepositorySelected: (repoPath : string) => void
}

/**
 * Initializes a new AddRepositoryModal class.
 * @param props The properties to include repositories.
 * @constructor
 */
const AddRepositoryModal: React.FC<IAddRepositoryModalProps> = (props: IAddRepositoryModalProps) => {
    /** Gets or sets the repository path. **/
    const [repoPath, setRepoPath] = useState("");
    return (
        <>
            <div className="modal-backdrop" onClick={props.onClose}>
                <div className="commit-modal" onClick={(e) => e.stopPropagation()}>
                    <div className="commit-modal-icon">
                        <svg
                            xmlns="http://www.w3.org/2000/svg"
                            viewBox="0 0 24 24"
                            width="48"
                            height="48"
                            fill="none"
                        >
                            <circle cx="12" cy="12" r="10" fill="#00bfff"/>
                            <rect x="11" y="10" width="2" height="7" fill="#ffffff"/>
                            <rect x="11" y="7" width="2" height="2" fill="#ffffff"/>
                       </svg>
                    </div>
                    <h2 className="commit-modal-title"
                        style={{marginTop: "-30px"}}>Enter a repository to add:</h2>
                    <input
                        type="text"
                        className="input-field"
                        placeholder="Enter repository path"
                        value={repoPath}
                        onChange={(e) => setRepoPath(e.target.value)}
                        style={{width: "100%"}}
                        required
                    />
                    <div>
                        <button className="commit-modal-button"
                                style={{marginRight: "50px"}}
                                onClick={() => {
                                    props.onRepositorySelected(repoPath);
                                    props.onClose();
                                }}
                        >
                            Add
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

export default AddRepositoryModal;