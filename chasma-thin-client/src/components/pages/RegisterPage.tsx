import React, { useEffect, useState } from 'react';
import { Link, useNavigate } from 'react-router-dom';
import { AddUserRequest } from "../../API/ChasmaWebApiClient";
import { useCacheStore } from "../../managers/CacheManager";
import { userClient } from "../../managers/ApiClientManager";
import { handleApiError } from '../../managers/TransactionHandlerManager';
import { validatePassword } from '../../stringHelperUtil';
import { useDocumentTitle } from '../../util/useDocumentTitle';

/**
 * Initializes a new instance of the Register Page class.
 * @constructor
 */
const RegisterPage: React.FC = () => {
    useDocumentTitle("Register");

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

    /** Gets or sets a value indicating whether the request is ready to be sent. */
    const [disableSendButton, setDisableSendButton] = useState(false);

    /** Gets or sets a value indicating whether the password is valid. */
    const [passwordIsValid, setPasswordIsValid] = useState(false);

    /** Gets or sets the fact based security questions. */
    const [factBasedQuestions, setFactBasedQuestions] = useState<string[]>([]);

    /** Gets or sets the personal favorite security questions. */
    const [personalFavoriteQuestions, setPersonalFavoriteQuestions] = useState<string[]>([]);

    /** Gets or sets the family and relationship security questions. */
    const [familyAndRelationshipQuestions, setFamilyAndRelationshipQuestions] = useState<string[]>([]);

    /** Gets or sets the first security question for account recovery  */
    const [firstSecurityQuestion, setFirstSecurityQuestion] = useState<string>("");

    /** Gets or sets the answer to the first security question. */
    const [firstSecurityAnswer, setFirstSecurityAnswer] = useState<string>("");

    /** Gets or sets the second security question for account recovery. */
    const [secondSecurityQuestion, setSecondSecurityQuestion] = useState<string>("");

    /** Gets or sets the answer to the second security question. */
    const [secondSecurityAnswer, setSecondSecurityAnswer] = useState<string>("");

    /** Gets or sets the third security question for account recovery. */
    const [thirdSecurityQuestion, setThirdSecurityQuestion] = useState<string>("");

    /** Gets or sets the answer to the third security question. */
    const [thirdSecurityAnswer, setThirdSecurityAnswer] = useState<string>("");

    /** The navigation function. **/
    const navigate = useNavigate();

    /** Sets the notification modal. */
    const setNotification = useCacheStore(state => state.setNotification);

    /** Flag indicating whether the passwords match. **/
    const passwordsMatch = password === confirmPassword;

    /** Handles the request to register a new user with the system. **/
    const handleRegister = async () => {
        if (disableSendButton) {
            return;
        }

        setDisableSendButton(true);
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
            addUserRequest.firstSecurityQuestion = firstSecurityQuestion;
            addUserRequest.firstSecurityAnswer = firstSecurityAnswer;
            addUserRequest.secondSecurityQuestion = secondSecurityQuestion;
            addUserRequest.secondSecurityAnswer = secondSecurityAnswer;
            addUserRequest.thirdSecurityQuestion = thirdSecurityQuestion;
            addUserRequest.thirdSecurityAnswer = thirdSecurityAnswer;
            console.log(addUserRequest);
            const response = await userClient.addUserAccount(addUserRequest);
            if (response.isErrorResponse || response.user === undefined) {
                setNotification({
                    title: "Could not add user!",
                    message: response.errorMessage,
                    isError: true,
                });
                setDisableSendButton(false);
                return;
            }

            setNotification({
                title: `Successfully added to the system!`,
                message: `Welcome to Emryce Workflow Manager, ${response.user.userName}.`,
                isError: false,
            });
            setDisableSendButton(false);
            useCacheStore.getState().setUser(response.user);
            useCacheStore.getState().setToken(response.token);
            useCacheStore.getState().setRefreshToken(response.refreshToken);
            navigate('/home');
        } catch (error) {
            setDisableSendButton(false);
            const errorNotification = await handleApiError(error, navigate, "Error adding user!", "Review server logs for more information.");
            setNotification(errorNotification);
        }
    };

    useEffect(() => {
        /** Fetches the security questions from the Emryce server. */
        const fetchSecurityQuestions = async () => {
            try {
                const message = await userClient.getSecurityQuestions();
                if (message.securityQuestionsFirstSet) {
                    setFactBasedQuestions(message.securityQuestionsFirstSet);
                }

                if (message.securityQuestionsSecondSet) {
                    setPersonalFavoriteQuestions(message.securityQuestionsSecondSet);
                }

                if (message.securityQuestionsThirdSet) {
                    setFamilyAndRelationshipQuestions(message.securityQuestionsThirdSet);
                }
            } catch (error) {
                const errorNotification = await handleApiError(error, navigate, "Error getting security questions!", "Review server logs for more information.");
                setNotification(errorNotification);
            }
        };

        fetchSecurityQuestions().catch(console.error);
    }, [navigate, setNotification]);

    return (
        <>
            <div className="panel-card" style={{ padding: "15px" }}>
                <div className="workflow-page-header" style={{ paddingTop: "15px" }}>
                    <h1>Register New User</h1>
                    <p>Register your local account to start orchestrating branches, tracking pipelines, and automating your development environment🧬</p>
                    <p style={{ marginTop: '15px', color: '#aaa', textAlign: 'center' }}>
                        Already have an account?{' '}
                        <Link to="/login" style={{ color: '#00bfff', textDecoration: 'none' }}>
                            Login here
                        </Link>
                    </p>
                </div>
                {/* Full Name */}
                <div className="form-row">
                    <label>Enter Full Name:</label>
                    <input
                        type="text"
                        className="input-field"
                        placeholder="Full Name"
                        value={name}
                        onChange={(e) => setName(e.target.value)}
                        required
                    />
                </div>

                {/* Email */}
                <div className="form-row">
                    <label>Enter Email:</label>
                    <input
                        type="email"
                        className="input-field"
                        placeholder="Email"
                        value={email}
                        onChange={(e) => setEmail(e.target.value)}
                        required
                    />
                </div>

                {/* Username */}
                <div className="form-row">
                    <label>Enter Username:</label>
                    <input
                        type="text"
                        className="input-field"
                        placeholder="Username"
                        value={userName}
                        onChange={(e) => setUserName(e.target.value)}
                        required
                    />
                </div>

                {/* Password */}
                <div className="form-row" style={{ alignItems: 'flex-start' }}>
                    <label style={{ paddingTop: '14px' }}>Enter Password:</label>
                    <div style={{ flex: 1, display: 'flex', flexDirection: 'column', gap: '8px' }}>
                        <div className="password-wrapper" style={{ position: 'relative', display: 'flex', alignItems: 'center', width: '100%' }}>
                            <input
                                type={showPassword ? "text" : "password"}
                                className="input-field"
                                placeholder="Password"
                                value={password}
                                style={{ paddingRight: '46px' }} // Keeps text clear of the button
                                onChange={(e) => {
                                    setPassword(e.target.value);
                                    const passwordIsValid = validatePassword(e.target.value);
                                    setPasswordIsValid(passwordIsValid);
                                }}
                                required
                            />
                            <button
                                type="button"
                                className="password-toggle"
                                style={{ position: 'absolute', right: '14px', background: 'transparent', border: 'none', cursor: 'pointer', padding: 0 }}
                                onClick={() => setShowPassword(!showPassword)}
                            >
                                {showPassword ? (
                                    <svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 24 24" width="20" height="20" fill="#22d3ee">
                                        <path d="M12 5c-7 0-11 7-11 7s4 7 11 7 11-7 11-7-4-7-11-7zm0 12c-2.761 0-5-2.239-5-5s2.239-5 5-5 5 2.239 5 5-2.239 5-5 5zm0-8c-1.657 0-3 1.343-3 3s1.343 3 3 3 3-1.343 3-3-1.343-3-3-3z" />
                                    </svg>
                                ) : (
                                    <svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 24 24" width="20" height="20" fill="#22d3ee">
                                        <path d="M12 5c-7 0-11 7-11 7s4 7 11 7c2.386 0 4.574-.715 6.465-1.915l1.489 1.489 1.414-1.414-1.487-1.488c.89-1.034 1.533-2.222 1.533-3.572 0-7-11-7-11-7zm0 12c-2.761 0-5-2.239-5-5 0-.495.088-.965.24-1.402l6.162 6.162c-.437.152-.907.24-1.402.24zm3.76-3.598l-6.162-6.162c.437-.152.907-.24 1.402-.24 2.761 0 5 2.239 5 5 0 .495-.088.965-.24 1.402z" />
                                    </svg>
                                )}
                            </button>
                        </div>
                        {!passwordIsValid && password && (
                            <div className="password-error" style={{ color: '#f87171', fontSize: '0.875rem' }}>
                                <p style={{ margin: '0 0 4px 0' }}>Password needs to meet the following requirements:</p>
                                <ul style={{ margin: 0, paddingLeft: '20px' }}>
                                    <li>At least 1 lowercase character</li>
                                    <li>At least 1 uppercase character</li>
                                    <li>At least 1 symbol</li>
                                    <li>At least 1 digit</li>
                                    <li>At least 10 or more characters</li>
                                </ul>
                            </div>
                        )}
                        {passwordIsValid && password && (
                            <div className="password-success" style={{ color: '#4ade80', fontSize: '0.875rem', fontWeight: 500 }}>
                                Password meets requirements!
                            </div>
                        )}
                    </div>
                </div>

                {/* Confirm Password */}
                <div className="form-row" style={{ alignItems: 'flex-start' }}>
                    <label style={{ paddingTop: '14px' }}>Confirm Password:</label>
                    <div style={{ flex: 1, display: 'flex', flexDirection: 'column', gap: '8px' }}>
                        <div className="password-wrapper" style={{ position: 'relative', display: 'flex', alignItems: 'center', width: '100%' }}>
                            <input
                                type={showConfirmPassword ? "text" : "password"}
                                className="input-field"
                                placeholder="Confirm Password"
                                value={confirmPassword}
                                style={{ paddingRight: '46px', marginBottom: 0 }}
                                onChange={(e) => setConfirmPassword(e.target.value)}
                                required
                            />
                            <button
                                type="button"
                                className="password-toggle"
                                style={{ position: 'absolute', right: '14px', background: 'transparent', border: 'none', cursor: 'pointer', padding: 0 }}
                                onClick={() => setShowConfirmPassword(!showConfirmPassword)}
                            >
                                {showConfirmPassword ? (
                                    <svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 24 24" width="20" height="20" fill="#22d3ee">
                                        <path d="M12 5c-7 0-11 7-11 7s4 7 11 7 11-7 11-7-4-7-11-7zm0 12c-2.761 0-5-2.239-5-5s2.239-5 5-5 5 2.239 5 5-2.239 5-5 5zm0-8c-1.657 0-3 1.343-3 3s1.343 3 3 3 3-1.343 3-3-1.343-3-3-3z" />
                                    </svg>
                                ) : (
                                    <svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 24 24" width="20" height="20" fill="#22d3ee">
                                        <path d="M12 5c-7 0-11 7-11 7s4 7 11 7c2.386 0 4.574-.715 6.465-1.915l1.489 1.489 1.414-1.414-1.487-1.488c.89-1.034 1.533-2.222 1.533-3.572 0-7-11-7-11-7zm0 12c-2.761 0-5-2.239-5-5 0-.495.088-.965.24-1.402l6.162 6.162c-.437.152-.907.24-1.402.24zm3.76-3.598l-6.162-6.162c.437-.152.907-.24 1.402-.24 2.761 0 5 2.239 5 5 0 .495-.088.965-.24 1.402z" />
                                    </svg>
                                )}
                            </button>
                        </div>

                        {!passwordsMatch && confirmPassword && (
                            <div className="password-error" style={{ color: '#f87171', fontSize: '0.875rem' }}>
                                Passwords do not match.
                            </div>
                        )}
                        {passwordsMatch && confirmPassword && (
                            <div className="password-success" style={{ color: '#4ade80', fontSize: '0.875rem', fontWeight: 500 }}>
                                Passwords match! Good to go!
                            </div>
                        )}
                    </div>
                </div>
                <hr className="separator" />

                {/* Security Question 1 */}
                <div className="form-row">
                    <label>Security Question 1:</label>
                    <select
                        className="repo-dropdown input-field"
                        value={firstSecurityQuestion}
                        onChange={(e) => setFirstSecurityQuestion(e.target.value)}
                    >
                        <option value="">Select</option>
                        {factBasedQuestions.map(question => (
                            <option key={question} value={question}>{question}</option>
                        ))}
                    </select>
                </div>

                {/* Security Answer 1 */}
                <div className="form-row">
                    <label>Security Answer 1:</label>
                    <input
                        type="text"
                        className="input-field"
                        value={firstSecurityAnswer}
                        onChange={(e) => setFirstSecurityAnswer(e.target.value)}
                        required
                    />
                </div>
                <br />

                {/* Security Question 2 */}
                <div className="form-row">
                    <label>Security Question 2:</label>
                    <select
                        className="repo-dropdown input-field"
                        value={secondSecurityQuestion}
                        onChange={(e) => setSecondSecurityQuestion(e.target.value)}
                    >
                        <option value="">Select</option>
                        {personalFavoriteQuestions.map(question => (
                            <option key={question} value={question}>{question}</option>
                        ))}
                    </select>
                </div>

                {/* Security Answer 2 */}
                <div className="form-row">
                    <label>Security Answer 2:</label>
                    <input
                        type="text"
                        className="input-field"
                        value={secondSecurityAnswer}
                        onChange={(e) => setSecondSecurityAnswer(e.target.value)}
                        required
                    />
                </div>
                <br />

                {/* Security Question 3 */}
                <div className="form-row">
                    <label>Security Question 3:</label>
                    <select
                        className="repo-dropdown input-field"
                        value={thirdSecurityQuestion}
                        onChange={(e) => setThirdSecurityQuestion(e.target.value)}
                    >
                        <option value="">Select</option>
                        {familyAndRelationshipQuestions.map(question => (
                            <option key={question} value={question}>{question}</option>
                        ))}
                    </select>
                </div>

                {/* Security Answer 3 */}
                <div className="form-row">
                    <label>Security Answer 3:</label>
                    <input
                        type="text"
                        className="input-field"
                        value={thirdSecurityAnswer}
                        onChange={(e) => setThirdSecurityAnswer(e.target.value)}
                        required
                    />
                </div>
                <br />

                <button
                    type="submit"
                    className="submit-button"
                    disabled={!passwordsMatch || !passwordIsValid || disableSendButton}
                    onClick={() => handleRegister()}
                >
                    Register
                </button>
            </div>
        </>
    );
};

export default RegisterPage;