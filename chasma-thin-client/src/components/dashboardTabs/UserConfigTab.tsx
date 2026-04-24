import { useState } from "react";
import { useCacheStore } from "../../managers/CacheManager";
import { validatePassword } from "../../stringHelperUtil";
import { CheckUsernameAvailabilityRequest, ModifyUserRequest } from "../../API/ChasmaWebApiClient";
import { userClient } from "../../managers/ApiClientManager";
import { handleApiError } from "../../managers/TransactionHandlerManager";
import { useNavigate } from "react-router-dom";

/**
 * Initializes a new UserConfigTab class.
 * @constructor
 */
const UserConfigTab: React.FC = () => {
    /** Gets the logged-in user. */
    const user = useCacheStore(state => state.user);

    /** Sets the notification modal. */
    const setNotification = useCacheStore(state => state.setNotification);

    /** The navigation function. **/
    const navigate = useNavigate();

    /** Gets or sets the username of the user. */
    const [username, setUsername] = useState<string | undefined>(user?.userName);

    /** Gets or sets the username of the user. */
    const [fullName, setFullName] = useState<string | undefined>(user?.name);

    /** Gets or sets the email of the user. */
    const [email, setEmail] = useState<string | undefined>(user?.email);

    /** Gets or sets the saved username. */
    const [lastSavedUsername, setLastSavedUsername] = useState<string | undefined>(user?.userName);

    /** Gets or sets the user's configured password. */
    const [password, setPassword] = useState<string>("");

    /** Gets or sets the confirmed password. */
    const [confirmPassword, setConfirmedPassword] = useState<string>("");

    /** Gets or sets a value indicating whether the password is valid. */
    const [passwordIsValid, setPasswordIsValid] = useState(true);

    /** Gets or sets the username validation error. */
    const [usernameValidationError, setUsernameValidationError] = useState<string | undefined>(undefined);

    /** Flag indicating whether the passwords match. **/
    const passwordsMatch = password === confirmPassword;

    /**
     * Handles the input that the user makes to the password.
     * @param passwordInput The password entered by the user.
     */
    const handlePasswordChange = (passwordInput: string) => {
        setPassword(passwordInput);
        let isValid;
        if (passwordInput.length > 0) {
            isValid = validatePassword(passwordInput);
        }
        else {
            isValid = true;
        }
        
        setPasswordIsValid(isValid);
    };

    /**
     * Handles the action when the user wants to modify the system user.
     */
    const handleModifyUserAction = async () => {
        setNotification({
            title: "Modifying user in the system...",
            message: "Please wait while your request is being processed.",
            isError: false,
            loading: true
        });
        
        try {
            const request = new ModifyUserRequest();
            request.userId = user?.userId;
            request.username = username;
            request.email = email;
            request.name = fullName;
            request.password = password;
            const response = await userClient.modifyUser(request);
            if (response.isErrorResponse) {
                setNotification({
                    title: "Failed to modify user!",
                    message: response.errorMessage,
                    isError: true,
                });
                return;
            }

            setNotification({
                title: "Successfully modified the user!",
                message: "You may close the modal.",
                isError: false,
            });
            useCacheStore.getState().setUser(response.user);
            useCacheStore.getState().setToken(response.token);
            useCacheStore.getState().setRefreshToken(response.refreshToken);
            setPassword("");
            setConfirmedPassword("");
            setEmail(response.user?.email);
            setUsername(response.user?.userName);
            setFullName(response.user?.name);
            setLastSavedUsername(response.user?.userName);
        } catch (error) {
            const errorNotification = handleApiError(error, navigate, "Error modifying user!", "An error occurred when attempting to modify user. Review console and internal server logs.");
            setNotification(errorNotification);
        }
    };

    /**
     * Checks whether the username is available.
     * @param usernameInput The username input by the user.
     */
    const checkUsernameAvailability = async (usernameInput: string) => {
        try {
            const request = new CheckUsernameAvailabilityRequest();
            request.userName = usernameInput;
            const response = await userClient.checkUserNameAvailability(request);
            setUsernameValidationError(response.errorMessage);
        } catch (error) {
            const errorNotification = handleApiError(error, navigate, "Error checking username!", "An error occurred when attempting to check user name. Review console and internal server logs.");
            setNotification(errorNotification);
        }
    };
    return (
        <>
            <div className="workflow-page-header">
                <h1>Edit User Profile</h1>
                <p>Refactor your identity—your profile is your most important production build. 🛠️</p>
            </div>
            <div className="form-row">
                <label>Edit Username:</label>
                <input
                    type="text"
                    className="input-field"
                    placeholder="Username"
                    value={username}
                    onChange={(e) => {
                        setUsername(e.target.value);
                        checkUsernameAvailability(e.target.value);
                    }}
                />
            </div>
            {usernameValidationError && (
                <>
                    <div className="password-error">
                        <p>{usernameValidationError}</p>
                    </div>
                    <br/>
                </>
            )}
            {!usernameValidationError
                && lastSavedUsername
                && username
                && lastSavedUsername === username && (
                <>
                    <div className="password-success">
                        Your current username {username} is available!
                    </div>
                    <br/>
                </>
            )}
            {!usernameValidationError
                && lastSavedUsername
                && username
                && lastSavedUsername !== username
                && (
                <>
                    <div className="password-success">
                        {username} is available!
                    </div>
                    <br/>
                </>
            )}
            <div className="form-row">
                <label>Edit Full Name:</label>
                <input
                    type="text"
                    className="input-field"
                    placeholder="Full Name"
                    value={fullName}
                    onChange={(e) => setFullName(e.target.value)} />
            </div>
            <div className="form-row">
                <label>Edit Email:</label>
                <input
                    type="text"
                    className="input-field"
                    placeholder="Email"
                    value={email}
                    onChange={(e) => setEmail(e.target.value)} />
            </div>
            <div className="form-row">
                <label>Change Password:</label>
                <input
                    type="password"
                    className="input-field"
                    placeholder="*************"
                    value={password}
                    onChange={(e) => handlePasswordChange(e.target.value)} />
            </div>
            {!passwordIsValid && password && (
                <>
                    <div className="password-error">
                        <p>Password needs to meet the following requirements:</p>
                        <ul>
                            <li>At least 1 lowercase character</li>
                            <li>At least 1 uppercase character</li>
                            <li>At least 1 symbol</li>
                            <li>At least 1 digit</li>
                            <li>At least 10 or more characters</li>
                        </ul>
                    </div>
                    <br/>
                </>
            )}
            {passwordIsValid && password && (
                <>
                    <div className="password-success">
                        Password meets requirements!
                    </div>
                    <br/>
                </>
            )}
            {password.length > 0 && (
                <>
                    <div className="form-row">
                        <label>Confirm Password:</label>
                            <input
                                type="password"
                                className="input-field"
                                placeholder="Confirm Password"
                                value={confirmPassword}
                                onChange={(e) => setConfirmedPassword(e.target.value)} />
                    </div>
                {password !== confirmPassword && (
                <>
                    <div className="password-error">
                        <p>Passwords do not match!</p>
                    </div>
                    <br/>
                </>
                )}
                {password === confirmPassword && (
                    <>
                        <div className="password-success">
                            Passwords match! You're good to go!
                        </div>
                        <br/>
                    </>
                )}
            </>
            )}
            <button
                type="submit"
                className="submit-button"
                disabled={!passwordsMatch || !passwordIsValid}
                onClick={() => handleModifyUserAction()}
            >
                Save
            </button>
        </>
    )
}

export default UserConfigTab;