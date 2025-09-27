import React, { useState, useCallback } from 'react';
import '../css/LoginForm.css';
import NotificationModal from "./Modals/NotificationModal";
import {LoginClient, LoginRequest} from "../API/ChasmaWebApiClient";

/**
 * The login client for the Chasma Web API.
 */
const loginClient = new LoginClient();

/**
 * Initializes a new instance of the LoginForm class.
 * @constructor
 */
const LoginForm: React.FC = () => {
    const [username, setUsername] = useState<string>('');
    const [password, setPassword] = useState<string>('');
    const [notification, setNotification] = useState<{ title: string; message: string | undefined, isError: boolean | undefined, loading?: boolean } | null>(null);

    /**
     * Closes the modal once the user confirms the message.
     */
    const closeModal = () => {
        setNotification(null);
    };

    /**
     * Handles the username change after the user enters a change.
     */
    const handleUsernameChange = useCallback((e: React.ChangeEvent<HTMLInputElement>) => {
        setUsername(e.target.value);
    }, []); // Empty dependency array means this function is only created once

    /**
     * Handles the password change after the user enters a change.
     */
    const handlePasswordChange = useCallback((e: React.ChangeEvent<HTMLInputElement>) => {
        setPassword(e.target.value);
    }, []);

    /**
     * Handles the login attempt from the user.
     */
    const handleLogin = useCallback(async (e: React.FormEvent) => {
        e.preventDefault();
        setNotification({
            title: "Logging in...",
            message: "Please wait while we verify your credentials.",
            isError: false,
            loading: true
        });

        const loginRequest = new LoginRequest();
        loginRequest.userName = username;
        loginRequest.password = password;

        try {
            const response = await loginClient.handleLoginRequest(loginRequest);
            if (response.isErrorMessage) {
                setNotification({
                    title: "Failed to Login",
                    message: response.message,
                    isError: response.isErrorMessage
                });
                return;
            }

            setNotification({
                title: `Login Successful, ${response.userName}!`,
                message: `Welcome to Chasma, ${response.name}.`,
                isError: response.isErrorMessage
            });
        } catch (e) {
            console.error(e);
            setNotification({
                title: "Failed to Login",
                message: "Internal Server Error. Review console error logs.",
                isError: true
            });
        }
    }, [username, password]); // Dependencies ensure the function updates if username/password change

    return (
        <div>
            <h1 className="landing-page-title">Chasma</h1>
            <form className="login-form" onSubmit={handleLogin}>
                <input
                    className="input-field"
                    type="text"
                    value={username}
                    onChange={handleUsernameChange}
                    placeholder="Username"
                />
                <input
                    className="input-field"
                    type="password"
                    value={password}
                    onChange={handlePasswordChange}
                    placeholder="Password"
                />
                <button className="submit-button" type="submit">Login</button>
            </form>
            {notification && (
                <NotificationModal
                    title={notification.title}
                    isError={notification.isError}
                    message={notification.message}
                    onClose={closeModal}
                    loading={notification.loading}
                />
            )}
        </div>
    );
};

export default LoginForm;