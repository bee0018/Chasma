import React, {useCallback, useState} from "react";
import '../../css/DasboardTab.css';
import NotificationModal from "../modals/NotificationModal";
import {isBlankOrUndefined} from "../../stringHelperUtil";
import {DecodeJwtRequest, JwtClient, JwtHeader, JwtPayload} from "../../API/ChasmaWebApiClient";
import JwtInfoTable from "../JwtInfoTable";

/** The JWT controller interface to the Chasma Web API. **/
const jwtClient = new JwtClient();

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

    /** Gets or sets the encoded token. **/
    const [encodedToken, setEncodedToken] = useState<string>('');

    /** Gets or sets the audience. **/
    const [audience, setAudience] = useState<string>('');

    /** Gets or sets the issuer. **/
    const [issuer, setIssuer] = useState<string>('');

    /** Flag indicating if all fields are valid **/
    const formIsValid = !isBlankOrUndefined(secretKey)
        && !isBlankOrUndefined(encodedToken)
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
            title: "Decoding provided JSON Web Token...",
            message: "Please wait while your request is being processed.",
            isError: false,
            loading: true
        });

        const decodeJwtRequest = new DecodeJwtRequest();
        decodeJwtRequest.encodedToken = encodedToken;
        decodeJwtRequest.audience = audience;
        decodeJwtRequest.issuer = issuer;
        decodeJwtRequest.secretKey = secretKey;

        try {
            const response = await jwtClient.decodeJwt(decodeJwtRequest);
            if (response.isErrorResponse) {
                setNotification({
                    title: "Failed to Decode JWT",
                    message: response.errorMessage,
                    isError: response.isErrorResponse,
                });

                return;
            }

            setNotification({
                title: "Successfully decoded JWT!",
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
    }, [encodedToken, audience, issuer, secretKey]);

    return (
        <div>
            <h1 className="page-title">JWT Decoder 🔓</h1>
            <p className="page-description">Fill out the following fields to decode the provided JWT.</p>
            <p className="note"><i>Note: Displaying claims are still in the process of being implemented.</i>.</p>
            <br/>
            <form className="info-container" onSubmit={handleDecodeJwtRequest}>
                <input
                    className="input-field"
                    type="text"
                    placeholder="Enter Encoded Token"
                    value={encodedToken}
                    onChange={(e) => setEncodedToken(e.target.value)}/>
                <input
                    className="input-field"
                    type="text"
                    placeholder="Enter Secret Key"
                    value={secretKey}
                    onChange={(e) => setSecretKey(e.target.value)}/>
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
                    Decode JSON Web Token
                </button>
                <br/>
            </form>
            <br/>
            <br/>
            {jwtHeader !== undefined && jwtPayload !== undefined && (
                <div className="info-container">
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