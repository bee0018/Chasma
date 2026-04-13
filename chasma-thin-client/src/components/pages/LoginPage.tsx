import React, { useState } from 'react';
import {Link, useNavigate} from 'react-router-dom';
import ChasmaLogo from "../logos/ChasmaLogo";
import {LoginRequest} from "../../API/ChasmaWebApiClient";
import {useCacheStore} from "../../managers/CacheManager";
import {userClient} from "../../managers/ApiClientManager";
import { handleApiError } from '../../managers/TransactionHandlerManager';

/**
 * Creates a new instance of the Login Page class.
 * @constructor
 */
const LoginPage: React.FC = () => {
    /** Gets or sets the username of the user. **/
    const [userName, setUserName] = useState('');

    /** Gets or sets the password of the user. **/
    const [password, setPassword] = useState('');

    /** Gets or sets the flag indicating whether to disable the send button. */
    const [disabledSendButton, setDisableSendButton] = useState(false);

    /** The navigation function. **/
    const navigate = useNavigate();

    /** Sets the notification modal. */
    const setNotification = useCacheStore(state => state.setNotification);

    /**
     * Handles the request to log in a user to the system.
     */
    const handleLogin = async (e: React.FormEvent<HTMLFormElement>) => {
        if (disabledSendButton) {
            return;
        }
        
        e.preventDefault();
        setDisableSendButton(true);
        setNotification({
            title: "Logging into the system...",
            message: "Please wait while your request is being processed.",
            isError: false,
            loading: true
        });
        try {
            const loginUserRequest = new LoginRequest();
            loginUserRequest.userName = userName;
            loginUserRequest.password = password;
            const response = await userClient.login(loginUserRequest);
            if (response.isErrorResponse) {
                setNotification({
                    title: "Cannot login!",
                    message: response.errorMessage,
                    isError: true,
                });
                setDisableSendButton(false);
                return;
            }

            if (!response.refreshToken) {
                setNotification({
                    title: "Login error",
                    message: "No refresh token received.",
                    isError: true,
                });
                return;
            }

            useCacheStore.getState().setUser(response.user);
            useCacheStore.getState().setToken(response.token);
            useCacheStore.getState().setRefreshToken(response.refreshToken);
            setDisableSendButton(false);
            navigate('/home');
            setNotification(null);
        } catch (e) {
            setDisableSendButton(false);
            const errorNotification = handleApiError(e, navigate, "Could not log in!", "An internal server error has occurred. Review logs.");
            setNotification(errorNotification);
        }
    }

    return (
        <div className="login-page">
            <button
                className="help-button"
                onClick={() => window.open("help", "_blank")}>
                    Help
            </button>
            <button
                className="config-button"
                onClick={() => navigate("/setup")}>
                    Configure
            </button>
            <div className="login-card">
                <div className="login-logo">
                    <ChasmaLogo />
                </div>
                <h1 className="login-title">Sign In</h1>
                <form onSubmit={handleLogin}>
                    <input
                        type="text"
                        className="input-field"
                        placeholder="Username"
                        value={userName}
                        onChange={(e) => setUserName(e.target.value)}
                        required
                    />
                    <input
                        type="password"
                        className="input-field"
                        placeholder="Password"
                        value={password}
                        onChange={(e) => setPassword(e.target.value)}
                        required
                    />
                    <button
                        type="submit"
                        className="submit-button"
                        disabled={disabledSendButton}>
                            Login
                    </button>
                </form>
                <p className="login-footer">
                    Don’t have an account?{" "}
                    <Link to="/register">Register here</Link>
                </p>
            </div>
        </div>
    );
}

export default LoginPage;
