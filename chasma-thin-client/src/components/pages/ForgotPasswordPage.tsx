import { useState } from "react";
import { useCacheStore } from "../../managers/CacheManager";
import { useDocumentTitle } from "../../util/useDocumentTitle";
import { GetRandomSecurityQuestionRequest, ResetPasswordRequest, ValidateSecurityAnswerRequest } from "../../API/ChasmaWebApiClient";
import { userClient } from "../../managers/ApiClientManager";
import { validatePassword } from "../../stringHelperUtil";
import { Link, useNavigate } from "react-router-dom";

/**
 * Initializes a new instance of the ForgotPasswordPage class.
 * @constructor
 */
const ForgotPasswordPage: React.FC = () => {
    useDocumentTitle("Forgot Password");

    /** Sets the notification modal. */
    const setNotification = useCacheStore(state => state.setNotification);

    /** Gets or sets a value indicating whether the random security question has been received. */
    const [randomSecurityQuestionReceived, setRandomSecurityQuestionsReceived] = useState<boolean>(false);

    /** Gets or sets the security question to answer. */
    const [securityQuestion, setSecurityQuestion] = useState<string | undefined>(undefined);

    /** Gets or sets the security answer. */
    const [securityAnswer, setSecurityAnswer] = useState<string | undefined>(undefined);

    /** Gets or sets the username of the user. **/
    const [userName, setUserName] = useState('');

    /** Gets or sets a value indicating whether the user's security question has been validated. */
    const [answerValidated, setAnswerValidated] = useState<boolean>(false);

    /** Gets or sets a value indicating whether the password is visible. **/
    const [showPassword, setShowPassword] = useState(false);

    /** Gets or sets the password of the user. **/
    const [password, setPassword] = useState('');

    /** Gets or sets a value indicating whether the password is valid. */
    const [passwordIsValid, setPasswordIsValid] = useState(false);

    /** Gets or sets a value indicating whether to show the confirmed password field value. **/
    const [showConfirmPassword, setShowConfirmPassword] = useState(false);

    /** Gets or sets the confirmation password of the user. **/
    const [confirmPassword, setConfirmPassword] = useState('');

    /** Flag indicating whether the passwords match. **/
    const passwordsMatch = password === confirmPassword;

    /** The navigation function. **/
    const navigate = useNavigate();

    /** Gets the security question for the specified user. */
    const handleGetRandomSecurityQuestionRequest = async () => {
        const request = new GetRandomSecurityQuestionRequest();
        request.userName = userName;
        try {
            const response = await userClient.getRandomSecurityQuestion(request);
            if (response.isErrorResponse) {
                setNotification({
                    title: "Failed to get security question!",
                    message: response.errorMessage,
                    isError: true,
                });
                return;
            }

            if (response.securityQuestion) {
                setRandomSecurityQuestionsReceived(true);
                setSecurityQuestion(response.securityQuestion);
            }
        } catch (error) {
            setNotification({
                title: "Error getting security question!",
                message: "Review server logs for more information.",
                isError: true,
            });
        }
    };

    /** Handles the event when the user wants to validate the security answer. */
    const handleValidateSecurityAnswerRequest = async () => {
        const request = new ValidateSecurityAnswerRequest();
        request.userName = userName;
        request.securityQuestion = securityQuestion;
        request.securityAnswer = securityAnswer;
        try {
            const response = await userClient.validateSecurityQuestionAnswer(request);
            if (response.isErrorResponse) {
                setNotification({
                    title: "Validation Failed!",
                    message: response.errorMessage,
                    isError: true,
                });
                return;
            }

            if (!response.isAnswerValid) {
                setNotification({
                    title: "Validation Failed!",
                    message: "The answer provided was incorrect.",
                    isError: true,
                });
                return;
            }

            setAnswerValidated(response.isAnswerValid);
        } catch (error) {
            setNotification({
                title: "Error validating security question!",
                message: "Review server logs for more information.",
                isError: true,
            });
        }
    };

    /** Handles the event when the user wants to reset the password. */
    const handleResetPasswordRequest = async () => {
        const request = new ResetPasswordRequest();
        request.userName = userName;
        request.password = password;

        try {
            const response = await userClient.resetPassword(request);
            if (response.isErrorResponse) {
                setNotification({
                    title: "Password Reset Failed!",
                    message: response.errorMessage,
                    isError: true,
                });
                return;
            }

            if (!response.successfullyReset) {
                setNotification({
                    title: "Password Reset Failed!",
                    message: "Review server logs for more information.",
                    isError: true,
                });
                return;
            }

            setNotification({
                title: "Password Reset Successful!",
                message: "Rerouting back to login page now...",
                isError: false,
                loading: true,
            });

            setTimeout(() => {
                setNotification(null);
                navigate("/login");
            }, 4000);

        } catch (error) {
            setNotification({
                title: "Error resetting password!",
                message: "Review server logs for more information.",
                isError: true,
            });
        }
    };

    return (
        <>
            <div className="panel-card" style={{ padding: "15px" }}>
                <div className="workflow-page-header" style={{ paddingTop: "15px" }}>
                    <h1>Forgot Password?</h1>
                    <p>Let’s get you back into your account 🔑</p>
                    <p style={{ marginTop: '15px', color: '#aaa', textAlign: 'center' }}>
                        Already have an account?{' '}
                        <Link to="/login" style={{ color: '#00bfff', textDecoration: 'none' }}>
                            Login here
                        </Link>
                    </p>
                </div>
                <br />

                {/* Step 1 */}
                {!randomSecurityQuestionReceived && !answerValidated &&
                    <>
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
                        <button
                            type="submit"
                            className="submit-button"
                            onClick={handleGetRandomSecurityQuestionRequest}
                        >
                            Get Security Question
                        </button>
                    </>
                }

                {/* Step 2 */}
                {randomSecurityQuestionReceived && !answerValidated &&
                    <>
                        <h3>Question: {securityQuestion}</h3>
                        <input
                            type="text"
                            className="input-field"
                            placeholder="Security Answer"
                            value={securityAnswer}
                            onChange={(e) => setSecurityAnswer(e.target.value)}
                            required
                        />
                        <button
                            type="submit"
                            className="submit-button"
                            onClick={handleValidateSecurityAnswerRequest}
                        >
                            Validate Answer
                        </button>
                    </>
                }

                {/* Step 3 */}
                {answerValidated &&
                    <>
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
                        <button
                            type="submit"
                            className="submit-button"
                            onClick={handleResetPasswordRequest}
                        >
                            Reset Password
                        </button>
                    </>
                }
            </div>
        </>
    );
}

export default ForgotPasswordPage;