import React, {useCallback, useState} from "react";
import '../../css/DasboardTab.css';
import {EncodeJwtRequest, JwtClient} from "../../API/ChasmaWebApiClient";
import NotificationModal from "../modals/NotificationModal";
import {copyToClipboard, isBlankOrUndefined} from "../../stringHelperUtil";
import {Row} from "../types/CustomTypes";

/** The JWT controller interface to the Chasma Web API. **/
const jwtClient = new JwtClient();

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

    /** Gets or sets the custom claims. **/
    const [rows, setRows] = useState<Row[]>([]);

    /** Gets or sets the minutes in which the token will expire. **/
    const [expirationMinutes, setExpirationMinutes] = useState<number>(0);

    /** Gets or sets the notification **/
    const [notification, setNotification] = useState<{title: string, message: string | undefined, isError: boolean | undefined, loading?: boolean } | null>(null);

    /** Gets or sets a value indicating whether a valid encoded token has been received. **/
    const [encodedTokenReceived, setEncodedTokenReceived] = useState<boolean>(false);

    /** Gets or sets the encoded token. **/
    const [encodedToken, setEncodedToken] = useState<string | undefined>('');

    /**
     * Closes the modal once the user confirms the message
     */
    const closeModal = () => {
        setNotification(null);
    }

    /** Flag indicating if all fields are valid **/
    const formIsValid = !isBlankOrUndefined(secretKey)
        && !isBlankOrUndefined(username)
        && !isBlankOrUndefined(name)
        && !isBlankOrUndefined(role)
        && !isBlankOrUndefined(audience)
        && !isBlankOrUndefined(issuer)
        && expirationMinutes > 0;

    /** Adds a custom claim row to the form. **/
    const addCustomClaimRow = () => {
        setRows(prev => [
            ...prev,
            { id: crypto.randomUUID(), first: "", second: "" }
        ]);
    };

    /**
     * Deletes the row with the specified row identifier.
     * @param rowId The row identifier.
     */
    function deleteClaimRow(rowId: string) {
        const filteredRows = rows.filter(row => row.id !== rowId);
        setRows(filteredRows);
    }

    /** Handles custom claim row changes in the form. **/
    const handleClaimChange = (
        id: string,
        field: "first" | "second",
        value: string
    ) => {
        setRows(prev =>
            prev.map(row =>
                row.id === id ? { ...row, [field]: value } : row
            )
        );
    };

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
        encodeJwtRequest.customClaimTypes = [];
        encodeJwtRequest.customClaimValues = []
        rows.forEach(row => {
            if (!isBlankOrUndefined(row.first) &&  !isBlankOrUndefined(row.second) && encodeJwtRequest.customClaimTypes && encodeJwtRequest.customClaimValues) {
                encodeJwtRequest.customClaimTypes.push(row.first);
                encodeJwtRequest.customClaimValues.push(row.second);
            }
        });

        try {
            const response = await jwtClient.encodeJwt(encodeJwtRequest);
            if (response.isErrorResponse) {
                setNotification({
                    title: "Failed to Generate JWT",
                    message: response.errorMessage,
                    isError: response.isErrorResponse,
                });
                setEncodedTokenReceived(false);
                setEncodedToken(undefined);
                return;
            }

            setNotification({
                title: "Encoded JWT Request Successful",
                message: `Successfully generated encoded JWT for user ${encodeJwtRequest.username}!`,
                isError: response.isErrorResponse,
            });
            setEncodedTokenReceived(true);
            setEncodedToken(response.token);
        } catch (e) {
            setNotification({
                title: "Failed to Generate JWT",
                message: "An internal server error has occurred. Review logs.",
                isError: true,
            });
            setEncodedTokenReceived(false);
            setEncodedToken(undefined);
        }
    }, [secretKey, username, role, audience, name, issuer, expirationMinutes, rows]);

    /**
     * Handles the event when the user wants to copy encoded token to clipboard
     * @param encodedToken The encoded token.
     */
    const handleCopyTextToClipboard = async (encodedToken: string) => {
        const textCopiedSuccessfully = await copyToClipboard(encodedToken)
        if (textCopiedSuccessfully) {
            setNotification({
                title: "Successfully copied!",
                message: "Token has been successfully copied to your clipboard.",
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
        <div>
            <h1 className="page-title">JWT Encoder 🔒</h1>
            <p className="page-description">
                Fill out the following fields to generate an encoded JSON Web Token.
            </p>
            <br/>
            <form className="info-container" onSubmit={handleEncodeJwtRequest}>
                <h2>Default Token Properties</h2>
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
                <br/>
                <div className="header-row">
                    <h2>Custom Claims</h2>
                    <button
                        type="button"
                        id="addClaimButton"
                        className="circle-button"
                        onClick={addCustomClaimRow}
                    >
                        +
                    </button>
                </div>
                <br/>
                <div id="customClaimsContainer">
                    {rows.map(row => (
                        <div
                            key={row.id}
                            style={{
                                display: "flex",
                                gap: "10px",
                                marginBottom: "10px"
                            }}
                        >
                            <input
                                type="text"
                                placeholder="Claim Type"
                                className="input-field"
                                value={row.first}
                                onChange={e => handleClaimChange(row.id, "first", e.target.value)}
                            />
                            <input
                                type="text"
                                placeholder="Claim Value"
                                className="input-field"
                                value={row.second}
                                onChange={e => handleClaimChange(row.id, "second", e.target.value)}
                            />
                            <button
                                className="delete-button"
                                type="button"
                                onClick={() => deleteClaimRow(row.id)}
                            >
                                Remove
                            </button>
                        </div>
                    ))}
                    <br/>
                </div>
                <br/>
                <button
                    className="submit-button"
                    type="submit"
                    disabled={!formIsValid}
                >
                    Generate Encoded JSON Web Token
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
            {encodedTokenReceived && encodedToken !== undefined && (
                <div>
                    <br/>
                    <h1>Encoded Token</h1>
                    <div className="info-container">
                        <input
                            className="input-field"
                            type="text"
                            disabled={true}
                            value={encodedToken} />
                        <button
                            className="submit-button"
                            type="submit"
                            onClick={() => handleCopyTextToClipboard(encodedToken)}
                        >
                            Copy to Clipboard
                        </button>
                    </div>
                </div>
            )}
        </div>
    );
}

export default EncodeJwtTab;