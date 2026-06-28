import { useState } from "react";
import { ReportBugsRequest } from "../../API/ChasmaWebApiClient";
import { useCacheStore } from "../../managers/CacheManager";
import { handleApiError } from "../../managers/TransactionHandlerManager";
import { useNavigate } from "react-router-dom";
import { proxyClient } from "../../managers/ApiClientManager";

/**
 * Initializes a new instance of the ReportBugsPage class.
 * @constructor
 */
export const ReportBugsPage: React.FC = () => {
    /** Gets or sets a value indicating whether the bug reporting response was successful. **/
    const [successfullyCreated, setSuccessfullyCreated] = useState<boolean | undefined>(undefined);

    /** Gets or sets the flag indicating whether to disable the send button. */
    const [disabledSendButton, setDisableSendButton] = useState(false);

    /** Gets or sets the bug title of the issue. */
    const [bugTitle, setBugTitle] = useState<string | undefined>(undefined);

    /** Gets or sets the bug description of the issue. */
    const [bugDescription, setBugDescription] = useState<string | undefined>(undefined);

    /** Gets the logged-in user. */
    const user = useCacheStore(state => state.user);

    /** Gets the function for setting the system notification. */
    const setNotification = useCacheStore(state => state.setNotification);

    /** Gets the navigate function. */
    const navigate = useNavigate();

    /**
     * Handles the request to report a system bug to the internal 
     */
    const handleReportBugRequest = async () => {
        if (disabledSendButton) {
            return;
        }

        setNotification({
            title: "Submitting bug report...",
            message: "Sending your report to our support team.",
            isError: false,
            loading: true,
        });
        setDisableSendButton(true);
        const request = new ReportBugsRequest();
        request.userId = user?.userId;
        request.issueTitle = bugTitle;
        request.bugDescription = bugDescription;
        try {
            const response = await proxyClient.reportBugToCloudflareWorker(request);
            if (response.isErrorResponse) {
                setNotification({
                    title: "Failed to submit bug report",
                    message: response.errorMessage,
                    isError: true,
                });
                return;
            }

            setNotification({
                title: "Successfully sent your bug report!",
                message: "Thank you for bringing this to our attention. We look forward to resolving your needs!",
                isError: false,
            });
            setSuccessfullyCreated(true);
        } catch (error) {
            const errorNotification = await handleApiError(error, navigate, "Error reporting bug!", "An error occurred when attempting to submit bug report. Review console and internal server logs.");
            setNotification(errorNotification);
        }
    };

    /**
     * Resets the form details.
     */
    const resetForm = () => {
        setBugTitle("");
        setBugDescription("");
        setSuccessfullyCreated(false);
    };

    return (
        <>
            <div className="workflow-page-header">
                <h1>Spot a Bug? Let’s Fix It🚨</h1>
                <p>Let us know what happened and feel free to paste your logs!</p>
            </div>
            <input
                type="text"
                placeholder="Give us a concise overview of the issue"
                className="input-field"
                value={bugTitle}
                onChange={(e) => setBugTitle(e.target.value)} />
            <textarea
                className="textarea-field"
                placeholder="Tell us what happened..."
                style={{ height: "600px", width: "100%", padding: "16px", boxSizing: "border-box" }}
                value={bugDescription}
                onChange={(e) => setBugDescription(e.target.value)}
            />
            <div className="modal-actions" style={{ marginTop: "16px" }}>
                <button className="modal-button primary"
                    hidden={successfullyCreated}
                    disabled={disabledSendButton}
                    onClick={handleReportBugRequest}
                >
                    Send Report
                </button>
                <button className="modal-button secondary"
                    onClick={resetForm}
                    style={{ marginLeft: "8px" }}
                >
                    {successfullyCreated ? "Reset" : "Clear"}
                </button>
            </div>
        </>
    );
}

export default ReportBugsPage;