import React, {useCallback, useState} from "react";
import '../../css/DasboardTab.css';
import NotificationModal from "../modals/NotificationModal";
import {isBlankOrUndefined} from "../../StringHelperUtil";
import {DecodeJwtRequest, JwtClient, JwtHeader, JwtPayload} from "../../API/ChasmaWebApiClient";
import JwtInfoTable from "../JwtInfoTable";

/** The JWT controller interface to the Chasma Web API. **/
const jwtController = new JwtClient();

/**
 * The Decode Tab contents and display components.
 * @constructor Initializes a new instance of the DecodeJwtTab.
 */
const DecodeJwtTab: React.FC = () => {
    /** Gets or sets the notification **/
    const [notification, setNotification] = useState<{title: string, message: string | undefined, isError: boolean | undefined, loading?: boolean } | null>(null);

    /** Gets or sets the decoded JWT header. **/
    const [jwtHeader, setJwtHeader] = useState<JwtHeader | undefined>(undefined);

    /** Gets or sets the decoded JWT payload. **/
    const [jwtPayload, setJwtPayload] = useState<JwtPayload | undefined>(undefined);

    /** Gets or sets the secret key. **/
    const [secretKey, setSecretKey] = useState<string>('');

    /** Gets or sets the username. **/
    const [username, setUsername] = useState<string>('');

    /** Gets or sets the audience. **/
    const [audience, setAudience] = useState<string>('');

    /** Gets or sets the issuer. **/
    const [issuer, setIssuer] = useState<string>('');

    /** Flag indicating if all fields are valid **/
    const formIsValid = !isBlankOrUndefined(secretKey)
        && !isBlankOrUndefined(username)
        && !isBlankOrUndefined(audience)
        && !isBlankOrUndefined(issuer);

    /**
     * Closes the modal once the user confirms the message
     */
    const closeModal = () => {
        setNotification(null);
    }

    /**
     * Handles the request to decode a JWT.
     **/
    const handleDecodeJwtRequest = useCallback(async (e: React.FormEvent) => {
        e.preventDefault();
        setNotification({
            title: `Decoding current JWT for user ${username}...`,
            message: "Please wait while your request is being processed.",
            isError: false,
            loading: true
        });

        // Clearing out any data to reset the view.
        setJwtHeader(undefined);
        setJwtPayload(undefined);

        const decodeJwtRequest = new DecodeJwtRequest();
        decodeJwtRequest.username = username;
        decodeJwtRequest.audience = audience;
        decodeJwtRequest.issuer = issuer;
        decodeJwtRequest.secretKey = secretKey;

        try {
            const response = await jwtController.decodeJwt(decodeJwtRequest);
            if (response.isErrorResponse) {
                setNotification({
                    title: "Failed to Decode JWT",
                    message: response.errorMessage,
                    isError: response.isErrorResponse,
                });
            }

            setNotification({
                title: `Successfully generated decoded JWT for user ${username}!`,
                message: "Close this modal and view this JWT's contents.",
                isError: response.isErrorResponse,
            });

            setJwtHeader(response.header);
            setJwtPayload(response.payload);
        } catch (e) {
            setNotification({
                title: "Failed to Decode JWT",
                message: "An internal server error has occurred. Review logs.",
                isError: true,
            });
        }
    }, [username, audience, issuer, secretKey]);

    return (
        <div>
            <h1 className="page-title">JWT Decoder 🔓</h1>
            <p className="page-description">Fill out the following fields to decode the provided JWT.</p>
            <p className="note"><i>Note: Custom claims is still in the process of being implemented</i>.</p>
            <br/>
            <form className="request-form" onSubmit={handleDecodeJwtRequest}>
                <input
                    className="input-field"
                    type="text"
                    placeholder="Enter Secret Key"
                    value={secretKey}
                    onChange={(e) => setSecretKey(e.target.value)}/>
                <input
                    className="input-field"
                    type="text"
                    placeholder="Enter Username"
                    value={username}
                    onChange={(e) => setUsername(e.target.value)}/>
                <input
                    className="input-field"
                    type="text"
                    placeholder="Enter Audience"
                    value={audience}
                    onChange={(e) => setAudience(e.target.value)}/>
                <input
                    className="input-field"
                    type="text"
                    placeholder="Enter Issuer"
                    value={issuer}
                    onChange={(e) => setIssuer(e.target.value)}/>

                <button
                    className="submit-button"
                    type="submit"
                    disabled={!formIsValid}
                >
                    Decode JWT for user: {username}
                </button>
                <br/>
            </form>
            <br/>
            <br/>
            {jwtHeader !== undefined && jwtPayload !== undefined && (
                <div className="request-form">
                    <JwtInfoTable
                        header={jwtHeader}
                        payload={jwtPayload}/>
                </div>
            )}
            {notification && (
                <NotificationModal
                    title={notification.title}
                    message={notification.message}
                    isError={notification.isError}
                    loading={notification.loading}
                    onClose={closeModal} />
            )}
        </div>
    );
}

export default DecodeJwtTab;