import React, { useState } from 'react';
import {Link, useNavigate} from 'react-router-dom';
import ChasmaLogo from "./logos/ChasmaLogo";
import NotificationModal from "./modals/NotificationModal";
import {LoginRequest, UserClient} from "../API/ChasmaWebApiClient";

/** Gets the database client that interfaces with the web API. **/
const userClient = new UserClient();

/**
 * Creates a new instance of the Login Page class.
 * @constructor
 */
const LoginPage: React.FC = () => {
    /** Gets or sets the username of the user. **/
    const [userName, setUserName] = useState('');

    /** Gets or sets the password of the user. **/
    const [password, setPassword] = useState('');

    /** The navigation function. **/
    const navigate = useNavigate();

    /** Gets or sets the notification **/
    const [notification, setNotification] = useState<{
        title: string,
        message: string | undefined,
        isError: boolean | undefined,
        loading?: boolean
    } | null>(null);

    /**
     * Closes the modal once the user confirms the message
     */
    const closeModal = () => {
        setNotification(null);
    }

    /**
     * Handles the request to log in a user to the system.
     */
    const handleLogin = async (e: React.FormEvent<HTMLFormElement>) => {
        e.preventDefault();
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
                return;
            }

            localStorage.setItem("username", JSON.stringify(response.userName));
            localStorage.setItem("userId", JSON.stringify(response.userId));
            localStorage.setItem("email", JSON.stringify(response.email));
            navigate('/home');
        } catch (e) {
            console.error(e);
            setNotification({
                title: "Could not log in!",
                message: "An internal server error has occurred. Review logs.",
                isError: true,
            });
            localStorage.removeItem("username");
            localStorage.removeItem("userId");
            localStorage.removeItem("email");
        }
    }

    return (
        <div className="dashboard-container page">
            <div className="page-body power-container">
                <div className="info-container" style={{ maxWidth: '400px' }}>
                    <div className="project-logo">
                        <ChasmaLogo />
                    </div>
                    <h1 className="page-title"
                        style={{textAlign: "center"}}>Sign In</h1>
                    <br/>
                    <form onSubmit={handleLogin}
                        style={{textAlign: "center"}}>
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
                        <br/>
                        <button type="submit" className="submit-button">
                            Login
                        </button>
                    </form>
                    <p style={{ marginTop: '15px', color: '#aaa', alignSelf: 'center' }}>
                        Don't have an account?{' '}
                        <Link to="/register" style={{ color: '#00bfff', textDecoration: 'none' }}>
                            Register here
                        </Link>
                    </p>
                </div>
            </div>
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

export default LoginPage;
