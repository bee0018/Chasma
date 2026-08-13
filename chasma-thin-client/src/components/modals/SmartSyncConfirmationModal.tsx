import { BranchCheckoutMode } from "../../API/ChasmaWebApiClient";
import React, { useEffect, useState } from "react";
import Checkbox from "../Checkbox";
import { createPortal } from "react-dom";

/**
 * The members of the checkout modal.
 */
interface ISmartSyncConfirmationModalProps {
    /** Function to call when the modal is being closed. **/
    onClose: () => void;

    /** Function to call when the checkout mode has been confirmed. */
    onSelected: (checkoutMode: BranchCheckoutMode) => void,
}

/**
 * Initializes a new SmartSyncConfirmationModal class.
 * @param props The properties to handle checking out changes.
 * @constructor
 */
const SmartSyncConfirmationModal: React.FC<ISmartSyncConfirmationModalProps> = (props: ISmartSyncConfirmationModalProps) => {
    /** Gets or sets the branch checkout mode. */
    const [branchCheckoutMode, setBranchCheckoutMode] = useState<BranchCheckoutMode>(BranchCheckoutMode.Default);

    useEffect(() => {
        const handler = (e: KeyboardEvent) => {
            if (e.key === "Escape") {
                props.onClose();
            }
        };

        window.addEventListener("keydown", handler);
        return () => window.removeEventListener("keydown", handler);
    }, []);

    return createPortal(
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
                    <h2 className="modal-title">Choose what you would like to do with the current changes in your working directories</h2>
                    <div style={{ justifySelf: "left", display: "grid", rowGap: "8px", marginBottom: "8px" }}>
                        <Checkbox
                            label={"Default"}
                            onBoxChecked={() => setBranchCheckoutMode(BranchCheckoutMode.Default)}
                            checked={branchCheckoutMode === BranchCheckoutMode.Default}
                            tooltip="Will attempt to pull changes directly into the workspace as-is."
                        />
                        <Checkbox
                            label={"Stash Only"}
                            onBoxChecked={() => setBranchCheckoutMode(BranchCheckoutMode.StashOnly)}
                            checked={branchCheckoutMode === BranchCheckoutMode.StashOnly}
                            tooltip="Save your current workspace and then pull changes."
                        />
                        <Checkbox
                            label={"Keep Changes"}
                            onBoxChecked={() => setBranchCheckoutMode(BranchCheckoutMode.KeepChanges)}
                            checked={branchCheckoutMode === BranchCheckoutMode.KeepChanges}
                            tooltip="Keep your changes in place after you pull the latest changes in."
                        />
                        <Checkbox
                            label={"Discard Changes"}
                            onBoxChecked={() => setBranchCheckoutMode(BranchCheckoutMode.DiscardAll)}
                            checked={branchCheckoutMode === BranchCheckoutMode.DiscardAll}
                            tooltip="Discard all your current changes in your workspace."
                        />
                    </div>
                    <br />
                    <div className="modal-actions">
                        <button className="modal-button primary"
                            onClick={() => props.onSelected(branchCheckoutMode)}
                        >
                            Sync
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

export default SmartSyncConfirmationModal;