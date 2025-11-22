import React, {useCallback, useState} from "react";
import '../../css/DasboardTab.css';
import {EncodeJwtRequest, JwtClient} from "../../API/ChasmaWebApiClient";
import NotificationModal from "../modals/NotificationModal";
import {isBlankOrUndefined} from "../../StringHelperUtil";

/** The JWT controller interface to the Chasma Web API. **/
const jwtController = new JwtClient();

/**
 * The Encode Tab contents and display components.
 * @constructor Initializes a new instance of the EncodeJwtTab.
 */
const EncodeJwtTab: React.FC = () => {
    /** Gets or sets the secret key. **/
    const [secretKey, setSecretKey] = useState<string>('');

    /** Gets or sets the username. **/
    const [username, setUsername] = useState<string>('');

    /** Gets or sets the name. **/
    const [name, setName] = useState<string>('');

    /** Gets or sets the role. **/
    const [role, setRole] = useState<string>('');

    /** Gets or sets the audience. **/
    const [audience, setAudience] = useState<string>('');

    /** Gets or sets the issuer. **/
    const [issuer, setIssuer] = useState<string>('');

    /** Gets or sets the custom claims.
     * todo const [customClaims, setCustomClaims] = useState<Map<string, string | null>>();
     */

    /** Gets or sets the minutes in which the token will expire. **/
    const [expirationMinutes, setExpirationMinutes] = useState<number>(0);

    /** Gets or sets the notification **/
    const [notification, setNotification] = useState<{title: string, message: string | undefined, isError: boolean | undefined, loading?: boolean } | null>(null);

    /**
     * Closes the modal once the user confirms the message
     */
    const closeModal = () => {
        setNotification(null);
        // display table
    }

    /** Flag indicating if all fields are valid **/
    const formIsValid = !isBlankOrUndefined(secretKey)
        && !isBlankOrUndefined(username)
        && !isBlankOrUndefined(name)
        && !isBlankOrUndefined(role)
        && !isBlankOrUndefined(audience)
        && !isBlankOrUndefined(issuer)
        && expirationMinutes > 0;

    /**
     * Handles the request to encode a JWT.
     **/
    const handleEncodeJwtRequest = useCallback(async (e: React.FormEvent) => {
        e.preventDefault();
        setNotification({
            title: "Generating Encoded JWT...",
            message: "Please wait while your request is being processed.",
            isError: false,
            loading: true
        });

        const encodeJwtRequest = new EncodeJwtRequest();
        encodeJwtRequest.secretKey = secretKey;
        encodeJwtRequest.username = username;
        encodeJwtRequest.role = role;
        encodeJwtRequest.audience = audience;
        encodeJwtRequest.name = name;
        encodeJwtRequest.issuer = issuer;
        encodeJwtRequest.expireInMinutes = expirationMinutes;

        try {
            const response = await jwtController.encodeJwt(encodeJwtRequest);
            if (response.isErrorResponse) {
                setNotification({
                    title: "Failed to Generate JWT",
                    message: response.errorMessage,
                    isError: response.isErrorResponse,
                });
            }

            setNotification({
                title: `Successfully generated encoded JWT for user ${encodeJwtRequest.username}!`,
                message: `The encoded token is: ${response.token}`,
                isError: response.isErrorResponse,
            });
        } catch (e) {
            setNotification({
                title: "Failed to Generate JWT",
                message: "An internal server error has occurred. Review logs.",
                isError: true,
            });
        }
    }, [secretKey, username, role, audience, name, issuer, expirationMinutes]);

    return (
        <div>
            <h1 className="page-title">JWT Encoder 🔒</h1>
            <p className="page-description">
                Fill out the following fields to generate an encoded JWT.
            </p>
            <p className="note"><i>Note: Custom claims is still in the process of being implemented</i>.</p>
            <br/>
            <form className="request-form" onSubmit={handleEncodeJwtRequest}>
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
                    placeholder="Enter Name"
                    value={name}
                    onChange={(e) => setName(e.target.value)}/>
                <input
                    className="input-field"
                    type="text"
                    placeholder="Enter Role"
                    value={role}
                    onChange={(e) => setRole(e.target.value)}/>
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
                <input
                    className="input-field"
                    type="text"
                    placeholder="Enter Expiration Minutes"
                    value={expirationMinutes}
                    onChange={(e) => setExpirationMinutes(Number(e.target.value))}/>
                <button
                    className="submit-button"
                    type="submit"
                    disabled={!formIsValid}
                >
                    Generate Encoded JWT
                </button>
            </form>
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

export default EncodeJwtTab;