import React, {useState} from "react";
import '../../css/DasboardTab.css';
import {UuidClient} from "../../API/ChasmaWebApiClient";
import {copyToClipboard, isBlankOrUndefined} from "../../stringHelperUtil";
import NotificationModal from "../modals/NotificationModal";

/**
 * Gets the UUID client that interfaces with the web API.
 */
const uuidClient = new UuidClient();

/**
 * Initializes a new instance of the UUID Generator tab.
 * @constructor
 */
const UuidGeneratorTab: React.FC = () => {
    /** Gets or sets the notification **/
    const [notification, setNotification] = useState<{title: string, message: string | undefined, isError: boolean | undefined, loading?: boolean } | null>(null);

    /** Gets or sets the UUID. **/
    const [uuid, setUuid] = useState<string | null>('');

    /**
     * Closes the modal once the user confirms the message
     */
    const closeModal = () => {
        setNotification(null);
    }

    /**
     * Handles the request to generate a UUID.
     */
    const handleGetUuid = async () => {
        setNotification({
            title: "Generating Encoded UUID...",
            message: "Please wait while your request is being processed.",
            isError: false,
            loading: true
        });

        try {
            const response = await uuidClient.generateUuid();
            setNotification({
                title: "UUID Generation Successful!",
                message: "Close the modal to view your UUID.",
                isError: false,
            });
            setUuid(response);
        }
        catch (e) {
            setNotification({
                title: "UUID Generation Failed!",
                message: "Review server logs for more information.",
                isError: true,
            });
            setUuid('');
            console.error(`Could not get UUID: ${e}`);
        }
    }

    /**
     * Handles the event when the user wants to copy the UUID to the clipboard.
     * @param uuid The UUID.
     */
    const handleCopyTextToClipboard = async (uuid: string) => {
        const textCopiedSuccessfully = await copyToClipboard(uuid)
        if (textCopiedSuccessfully) {
            setNotification({
                title: "Successfully copied!",
                message: "UUID has been successfully copied to your clipboard.",
                isError: false,
            });
        } else {
            setNotification({
                title: "Failed to copy",
                message: "Check console log for more information.",
                isError: true,
            });
        }
    }

    return (
        <>
            <h1 className="page-title">UUID Generator 🔄</h1>
            <div style={{ textAlign: "center" }}>
                <p className="page-description">Click the button below to generate a UUID.</p>
                <br/>

                <button
                    className="submit-button"
                    type="submit"
                    onClick={handleGetUuid}
                >
                    Generate UUID
                </button>
            </div>
            {notification && (
                <NotificationModal
                    title={notification.title}
                    message={notification.message}
                    isError={notification.isError}
                    loading={notification.loading}
                    onClose={closeModal} />
            )}
            {uuid && !isBlankOrUndefined(uuid) && (
                <div>
                    <br/>
                    <h1>Generated UUID</h1>
                    <div className="info-container">
                        <input
                            className="input-field"
                            type="text"
                            disabled={true}
                            value={uuid} />
                        <button
                            className="submit-button"
                            type="submit"
                            onClick={() => handleCopyTextToClipboard(uuid)}
                        >
                            Copy to Clipboard
                        </button>
                    </div>
                </div>
            )}
        </>
    );
}

export default UuidGeneratorTab;