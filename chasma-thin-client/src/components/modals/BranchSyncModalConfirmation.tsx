import { createPortal } from "react-dom";

/**
 * The interface defining the branch synchronization modal.
 */
interface IBranchSyncModalConfirmationProps {
    /** Function to call when the modal is being closed. **/
    onClose: () => void;

    /** Function to call when the user selects if they are going to skip retrieving builds. */
    onSelected: (isSkippingBuilds: boolean) => void;
}

/**
 * Initializes a new BranchSyncModalConfirmationModal class.
 * @param props The properties to process branch synchronization confirmation.
 * @constructor
 */
const BranchSyncModalConfirmationModal: React.FC<IBranchSyncModalConfirmationProps> = (props: IBranchSyncModalConfirmationProps) => {
    /**
     * Handles the choice when user wants to skip/retrieve builds.
     * @param isSkipping Flag indicating whether the user is skipping build retrievals.
     */
    const handleBuildChoice = (isSkipping: boolean) => {
        props.onClose();
        props.onSelected(isSkipping);
    }

    return  createPortal(
        <>
            <div className="modal-backdrop" onClick={props.onClose}>
                <div className="modal" onClick={(e) => e.stopPropagation()}>
                    <div className="modal-icon-container">
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

                    </div>
                    <h2 className="modal-title">Do you want to skip retrieving build data?</h2>
                    <h3 className="modal-message">May take longer to query data if getting workflow run data.</h3>
                    <br />
                    <div className="modal-actions">
                        <button
                            className="modal-button primary"
                            onClick={() => handleBuildChoice(true)}
                        >
                            Yes
                        </button>
                        <button
                            className="modal-button secondary"
                            onClick={() => handleBuildChoice(false)}
                        >
                            No
                        </button>
                        <button
                            className="modal-button secondary"
                            onClick={props.onClose}
                        >
                            Close
                        </button>
                    </div>
                </div>
            </div>
        </>,
        document.body
    );
};

export default BranchSyncModalConfirmationModal