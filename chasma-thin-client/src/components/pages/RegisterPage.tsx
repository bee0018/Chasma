import React, {useState} from 'react';
import {Link, useNavigate} from 'react-router-dom';
import ChasmaLogo from "../logos/ChasmaLogo";
import {AddUserRequest, UserClient} from "../../API/ChasmaWebApiClient";
import NotificationModal from "../modals/NotificationModal";
import {apiBaseUrl} from "../../environmentConstants";
import {User} from "../types/CustomTypes";
import {useCacheStore} from "../../managers/CacheManager";

/** The database client that interacts with the web API. **/
const userClient = new UserClient(apiBaseUrl);

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

    /** Gets or sets the confirmation password of the user. **/
    const [confirmPassword, setConfirmPassword] = useState('');

    /** Gets or sets a value indicating whether the password is visible. **/
    const [showPassword, setShowPassword] = useState(false);

    /** Gets or sets a value indicating whether to show the confirmed password field value. **/
    const [showConfirmPassword, setShowConfirmPassword] = useState(false);

    /** The navigation function. **/
    const navigate = useNavigate();

    /** Gets or sets the notification **/
    const [notification, setNotification] = useState<{
        title: string,
        message: string | undefined,
        isError: boolean | undefined,
        loading?: boolean
    } | null>(null);

    /** Flag indicating whether the passwords match. **/
    const passwordsMatch = password === confirmPassword;

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
            const loggedInUser: User = {
                userId: response.userId,
                username: response.userName,
                email: response.email,
            }
            useCacheStore.getState().setUser(loggedInUser);
            navigate('/home');
        } catch (e) {
            console.error(e);
            setNotification({
                title: "Could not add user!",
                message: "An internal server error has occurred. Review logs.",
                isError: true,
            });
        }
    }

    return (
        <div className="login-page">
            <div className="login-card">
                <div className="login-logo">
                    <ChasmaLogo/>
                </div>
                <h1 className="login-title">Register</h1>
                <form onSubmit={handleRegister}>
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
                    <div className="password-wrapper">
                        <input
                            type={showPassword ? "text" : "password"}
                            className="input-field"
                            placeholder="Password"
                            value={password}
                            onChange={(e) => setPassword(e.target.value)}
                            required
                        />
                        <button
                            type="button"
                            className="password-toggle"
                            onClick={() => setShowPassword(!showPassword)}
                        >
                            {showPassword ? (
                                <svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 24 24" width="20" height="20" fill="#22d3ee">
                                    <path d="M12 5c-7 0-11 7-11 7s4 7 11 7 11-7 11-7-4-7-11-7zm0 12c-2.761 0-5-2.239-5-5s2.239-5 5-5 5 2.239 5 5-2.239 5-5 5zm0-8c-1.657 0-3 1.343-3 3s1.343 3 3 3 3-1.343 3-3-1.343-3-3-3z"/>
                                </svg>
                            ) : (
                                <svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 24 24" width="20" height="20" fill="#22d3ee">
                                    <path d="M12 5c-7 0-11 7-11 7s4 7 11 7c2.386 0 4.574-.715 6.465-1.915l1.489 1.489 1.414-1.414-1.487-1.488c.89-1.034 1.533-2.222 1.533-3.572 0-7-11-7-11-7zm0 12c-2.761 0-5-2.239-5-5 0-.495.088-.965.24-1.402l6.162 6.162c-.437.152-.907.24-1.402.24zm3.76-3.598l-6.162-6.162c.437-.152.907-.24 1.402-.24 2.761 0 5 2.239 5 5 0 .495-.088.965-.24 1.402z"/>
                                </svg>
                            )}
                        </button>
                    </div>

                    <div className="password-wrapper">
                        <input
                            type={showConfirmPassword ? "text" : "password"}
                            className="input-field"
                            placeholder="Confirm Password"
                            value={confirmPassword}
                            onChange={(e) => setConfirmPassword(e.target.value)}
                            required
                        />
                        <button
                            type="button"
                            className="password-toggle"
                            onClick={() => setShowConfirmPassword(!showConfirmPassword)}
                        >
                            {showConfirmPassword ? (
                                <svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 24 24" width="20" height="20" fill="#22d3ee">
                                    <path d="M12 5c-7 0-11 7-11 7s4 7 11 7 11-7 11-7-4-7-11-7zm0 12c-2.761 0-5-2.239-5-5s2.239-5 5-5 5 2.239 5 5-2.239 5-5 5zm0-8c-1.657 0-3 1.343-3 3s1.343 3 3 3 3-1.343 3-3-1.343-3-3-3z"/>
                                </svg>
                            ) : (
                                <svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 24 24" width="20" height="20" fill="#22d3ee">
                                    <path d="M12 5c-7 0-11 7-11 7s4 7 11 7c2.386 0 4.574-.715 6.465-1.915l1.489 1.489 1.414-1.414-1.487-1.488c.89-1.034 1.533-2.222 1.533-3.572 0-7-11-7-11-7zm0 12c-2.761 0-5-2.239-5-5 0-.495.088-.965.24-1.402l6.162 6.162c-.437.152-.907.24-1.402.24zm3.76-3.598l-6.162-6.162c.437-.152.907-.24 1.402-.24 2.761 0 5 2.239 5 5 0 .495-.088.965-.24 1.402z"/>
                                </svg>
                            )}
                        </button>
                    </div>
                    {!passwordsMatch && confirmPassword && (
                        <div className="password-error">
                            Passwords do not match.
                        </div>
                    )}
                    {passwordsMatch && confirmPassword && (
                        <div className="password-success">
                            Passwords match! Good to go!
                        </div>
                    )}
                    <br/>
                    <button
                        type="submit"
                        className="submit-button"
                        disabled={!passwordsMatch}
                    >
                        Register
                    </button>
                </form>
                <p style={{marginTop: '15px', color: '#aaa', textAlign: 'center'}}>
                    Already have an account?{' '}
                    <Link to="/" style={{color: '#00bfff', textDecoration: 'none'}}>
                        Login here
                    </Link>
                </p>
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
