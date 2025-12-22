import React, { useState } from 'react';
import {Link, useNavigate} from 'react-router-dom';
import ChasmaLogo from "./logos/ChasmaLogo";
import {AddUserRequest, UserClient} from "../API/ChasmaWebApiClient";
import NotificationModal from "./modals/NotificationModal";

/** The database client that interacts with the web API. **/
const userClient = new UserClient();

/**
 * Initializes a new instance of the Register Page class.
 * @constructor
 */
const RegisterPage: React.FC = () => {
    /** Gets or sets the name of the user. **/
    const [name, setName] = useState('');

    /** Gets or sets the email of the user. **/
    const [email, setEmail] = useState('');

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

    /** Handles the request to register a new user with the system. **/
    const handleRegister = async (e: React.FormEvent<HTMLFormElement>) => {
        e.preventDefault();
        setNotification({
            title: "Adding user to the system...",
            message: "Please wait while your request is being processed.",
            isError: false,
            loading: true
        });
        try {
            const addUserRequest = new AddUserRequest();
            addUserRequest.name = name;
            addUserRequest.userName = userName;
            addUserRequest.password = password;
            addUserRequest.email = email;
            const response = await userClient.addUserAccount(addUserRequest);
            if (response.isErrorResponse) {
                setNotification({
                    title: "Could not add user!",
                    message: response.errorMessage,
                    isError: true,
                });
                return;
            }

            setNotification({
                title: `Successfully added to the system!`,
                message: `Welcome to Chasma Git Manager, ${response.userName}.`,
                isError: response.isErrorResponse,
            });
            localStorage.setItem("username", JSON.stringify(response.userName));
            localStorage.setItem("userId", JSON.stringify(response.userId));
            localStorage.setItem("email", JSON.stringify(response.email));
            navigate('/home');
        } catch (e) {
            console.error(e);
            setNotification({
                title: "Could not add user!",
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
                        style={{textAlign: "center"}}>Register</h1>
                    <form onSubmit={handleRegister}
                          style={{textAlign: "center"}}>
                        <input
                            type="text"
                            className="input-field"
                            placeholder="Full Name"
                            value={name}
                            onChange={(e) => setName(e.target.value)}
                            required
                        />
                        <input
                            type="email"
                            className="input-field"
                            placeholder="Email"
                            value={email}
                            onChange={(e) => setEmail(e.target.value)}
                            required
                        />
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
                            Register
                        </button>
                    </form>
                    <p style={{ marginTop: '15px', color: '#aaa', textAlign: 'center' }}>
                        Already have an account?{' '}
                        <Link to="/" style={{ color: '#00bfff', textDecoration: 'none' }}>
                            Login here
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

export default RegisterPage;
