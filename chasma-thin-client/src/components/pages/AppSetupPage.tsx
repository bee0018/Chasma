import { useEffect, useState } from "react";
import { useCacheStore } from "../../managers/CacheManager";
import { isBlankOrUndefined } from "../../stringHelperUtil";
import { ChasmaWebApiConfigurations, ModifyApiConfigRequest } from "../../API/ChasmaWebApiClient";
import { appConfigClient } from "../../managers/ApiClientManager";
import { useNavigate } from "react-router-dom";

/**
 * Initializes a new instance of the parent AppSetupPage class.
 * @constructor
 */
export const AppSetupPage: React.FC = () => {
    /** Sets the notification modal. */
    const setNotification = useCacheStore(state => state.setNotification);

    /** Gets or sets the JWT secret key. */
    const [jwtSecretKey, setJwtSecretKey] = useState<string>("");

    /** Gets or sets a value indicating whether the JWT secret key is valid. */
    const [jwtIsValid, setJwtIsValid] = useState<boolean | undefined>(undefined);

    /** Gets or sets a value indicating whether the JWT secret key is configured. */
    const [jwtIsConfigured, setJwtIsConfigured] = useState<boolean | undefined>(undefined);

    /** Gets or sets the API binding port. */
    const [bindingPort, setBindingPort] = useState<string | undefined>(undefined);

    /** Gets or sets a value indicating whether the binding port is valid. */
    const [portIsValid, setPortIsValid] = useState<boolean | undefined>(undefined);

    /** Gets or sets the GitHub API access token. */
    const [gitHubApiToken, setGitHubApiToken] = useState<string | undefined>(undefined);

    /** Gets or sets a value indicating whether the GitHub API token is configured. */
    const [gitHubApiTokenIsConfigured, setGitHubApiTokenIsConfigured] = useState<boolean | undefined>(undefined);

    /** Gets or sets the workflow run report threshold. */
    const [workflowRunReportThreshold, setWorkflowRunReportThreshold] = useState<string | undefined>(undefined);

    /** Gets or sets the GitHub pull request scan interval. */
    const [gitHubPullRequestScanIntervalSeconds, setGitHubPullRequestScanIntervalSeconds] = useState<string | undefined>(undefined);

    /** Gets or sets the GitLab API access token. */
    const [gitlabApiToken, setGitLabApiToken] = useState<string | undefined>(undefined);

    /** Gets or sets a value indicating whether the GitLab API token is configured. */
    const [gitLabApiTokenIsConfigured, setGitLabApiTokenIsConfigured] = useState<boolean | undefined>(undefined);

    /** Gets or sets the self hosted GitLab url. */
    const [selfHostedGitLabUrl, setSelfHostedGitLabUrl] = useState<string | undefined>(undefined);

    /** Gets or sets the GitLab merge request scan interval in seconds. */
    const [gitLabMergeRequestScanIntervalSeconds, setGitLabMergeRequestScanIntervalSeconds] = useState<string | undefined>(undefined);

    /** Gets or sets the user's GitHub username. */
    const [gitHubUsername, setGitHubUsername] = useState<string | undefined>(undefined);

    /** Gets or sets the user's GitLab username. */
    const [gitLabUsername, setGitLabUsername] = useState<string | undefined>(undefined);

    /** Gets or sets the global workspace path. */
    const [globalWorkspacePath, setGlobalWorkspacePath] = useState<string | undefined>(undefined);

    /** Gets or sets the GitHub SSH private key path. */
    const [gitHubSshPrivateKeyPath, setGitHubSshPrivateKeyPath] = useState<string | undefined>(undefined);

    /** Gets or sets the GitHub SSH private key passphrase. */
    const [gitHubSshPassphrase, setGitHubSshPassphrase] = useState<string | undefined>(undefined);

    /** Gets or sets the GitLab SSH private key path. */
    const [gitLabSshPrivateKeyPath, setGitLabSshPrivateKeyPath] = useState<string | undefined>(undefined);

    /** Gets or sets the GitLab SSH private key passphrase. */
    const [gitLabSshPassphrase, setGitLabSshPassphrase] = useState<string | undefined>(undefined);

    /** Gets or sets a value indicating whether the request is ready to be sent. */
    const [disableSendButton, setDisableSendButton] = useState(false);

    /** Gets the safe version of the number; undefined otherwise. */
    const safeNumber = (value?: string) => value && !isNaN(Number(value)) ? Number(value) : undefined;

    /** The navigate function. */
    const navigate = useNavigate();

    /** Gets the logged in user. */
    const user = useCacheStore(state => state.user);

    /** Gets a flag indicating whether binding port is valid. **/
    function validateBindingPort(value: string | undefined): void {
        const num = Number(value);
        const isValidPortNumber = isValidInteger(value) && num >= 0 && num <= 65356
        setPortIsValid(isValidPortNumber);
    };

    /**
     * Determines if the specified number is a positive integer.
     * @param num The number to validate.
     * @returns True if valid integer; false otherwise
     */
    function isValidInteger(num: string | undefined): boolean {
        const value = Number(num);
        return Number.isInteger(value) && value > 0;
    };

    /**
     * Validates the JWT secret key.
     * @param key The JWT secret key.
     */
    function validateJwt(key: string | undefined): void {
        if (key && key.length >= 16) {
            setJwtIsValid(true);
            return;
        }
        
        setJwtIsValid(false);
    };

    /** Handles the event when the user wants to handle application configuration. */
    const handleApplicationConfiguration = async () => {
        if (!jwtIsConfigured && isBlankOrUndefined(jwtSecretKey)) {
            setNotification({
                    title: "Could not apply configurations!",
                    message: "'jwtSecretKey' must be populated!",
                    isError: true,
                });
            return;
        }

        if (!jwtIsConfigured && jwtSecretKey && jwtSecretKey.length < 16) {
            setNotification({
                    title: "Could not apply configurations!",
                    message: "'jwtSecretKey' must be greater than or equal to 16 characters!",
                    isError: true,
                });
            return;
        }

        if (isBlankOrUndefined(bindingPort)) {
            setNotification({
                    title: "Could not apply configurations!",
                    message: "'bindingPort' must be populated!",
                    isError: true,
                });
            return;
        }

        if (!isValidInteger(bindingPort)) {
            setNotification({
                    title: "Could not apply configurations!",
                    message: "'bindingPort' must be a valid integer!",
                    isError: true,
                });
            return;
        }

        if (isBlankOrUndefined(globalWorkspacePath)) {
            setNotification({
                    title: "Could not apply configurations!",
                    message: "'globalWorkspacePath' must be populated!",
                    isError: true,
                });
            return;
        }

        await sendModifyConfigurationRequest();
    };

    /**
     * Sends the modify configuration request.
     */
    const sendModifyConfigurationRequest = async () => {
        setDisableSendButton(true);
        const config = new ChasmaWebApiConfigurations();
        config.jwtSecretKey = jwtSecretKey;
        config.bindingPort = safeNumber(bindingPort);
        config.gitHubApiToken = gitHubApiToken;
        config.workflowRunReportThreshold = safeNumber(workflowRunReportThreshold);
        config.gitHubPullRequestScanIntervalSeconds = safeNumber(gitHubPullRequestScanIntervalSeconds);
        config.gitLabApiToken = gitlabApiToken;
        config.selfHostedGitLabUrl = selfHostedGitLabUrl;
        config.gitLabMergeRequestScanIntervalSeconds = safeNumber(gitLabMergeRequestScanIntervalSeconds);
        config.gitHubUsername = gitHubUsername;
        config.gitLabUsername = gitLabUsername;
        const request = new ModifyApiConfigRequest();
        request.apiConfiguration = config;
        try {
            const response = await appConfigClient.modifyConfig(request);
            if (response.isErrorResponse) {
                setNotification({
                    title: "Could not apply configurations!",
                    message: response.errorMessage,
                    isError: true,
                });
                setDisableSendButton(false);
                return;
            }

            if (response.staticConfigurationsChanged) {
                setNotification({
                    title: "Successfully applied configurations!",
                    message: "Binding Port and/or JWT Secret key has been updated. Restart the app to apply changes.",
                    isError: false,
                });
            }
            else {
                // This has been an 'on the fly change' that does not require a system restart
                setNotification({
                    title: "Successfully applied configurations!",
                    message: "No system restart required.",
                    isError: false,
                });
            }
            
            setDisableSendButton(false);
        }
        catch (e) {
            console.error(e);
            setNotification({
                title: "Error applying configurations!",
                message: "Review console logs for more information.",
                isError: true,
            });
            setDisableSendButton(false);
        }
    };

    /**
     * Generates a JWT authentication token.
     * @returns The JWT secret key.
     */
    function generateRefreshToken(): string {
        const bytes = new Uint8Array(64);
        crypto.getRandomValues(bytes);
        let binary = "";
        for (let i = 0; i < bytes.length; i++) {
          binary += String.fromCharCode(bytes[i]);
        }
        
        return btoa(binary);
    }

    useEffect(() => {
        const getIsSystemReady = async () => {
            try {
                const response = await appConfigClient.getSystemReady();
                if (!response.isReady) {
                    setNotification({
                        title: "Setup Chasma System",
                        message: "The system is not configured. Enter your configurations and select the 'Apply Configurations' button at the bottom of the screen to save changes.",
                        isError: false,
                    });
                }
            } catch (e) {
                console.error(e);
                setNotification({
                    title: "Error determining system readiness!",
                    message: "Review console logs.",
                    isError: true,
                });
            }
        };

        getIsSystemReady();
    }, [setNotification]);

    /** Handles the event when the user wants to continue to the next tab. */
    const handleNavigation = () => {
        if (user === null) {
            navigate('/login');
        }
        else {
            navigate('/home')
        }
    }

    useEffect(() => {
        /**
         * Gets the API configuration values.
         */
        const getApiConfig = async () => {
            try {
                const response = await appConfigClient.getConfig();
                setJwtIsConfigured(response.jwtSecretKeyConfigured);
                if (response.jwtSecretKeyConfigured) {
                    setJwtIsValid(true);
                }

                setBindingPort(String(response.bindingPort));
                const port = response.bindingPort;
                if (port && port > 0 && port <= 65535) {
                    setPortIsValid(true);
                }

                setGitHubApiTokenIsConfigured(response.gitHubApiTokenConfigured);
                setGitLabApiTokenIsConfigured(response.gitLabApiTokenConfigured);
                setWorkflowRunReportThreshold(String(response.workflowRunReportThreshold));
                setGitHubPullRequestScanIntervalSeconds(String(response.gitHubPullRequestScanIntervalSeconds));
                setSelfHostedGitLabUrl(response.selfHostedGitLabUrl);
                setGitLabMergeRequestScanIntervalSeconds(String(response.gitLabMergeRequestScanIntervalSeconds));
                setGitHubUsername(response.gitHubUsername);
                setGitLabUsername(response.gitLabUsername);
                setGlobalWorkspacePath(response.globalWorkspacePath);
                setGitHubSshPrivateKeyPath(response.gitHubSshKeyPrivateKeyPath);
                setGitHubSshPassphrase(response.gitHubSshPassphrase);
                setGitLabSshPrivateKeyPath(response.gitLabSshKeyPrivateKeyPath);
                setGitLabSshPassphrase(response.gitLabSshPassphrase);
            } catch (error) {
                console.log(error);
                setNotification({
                    title: "Could get API configuration!",
                    message: "Review console logs.",
                    isError: true,
                });
            }
        };
        
        getApiConfig().catch(e => console.error(e));
    }, []);

    return (
        <div className="panel-card">
            <button
                className="continue-button"
                onClick={handleNavigation}
            >
                {user === null ? "Continue to Login" : "Home"} →
            </button>
            <h1 className="page-title">Chasma Setup</h1>
            <div className="xml-attr">
                <div className="xml-attr-header">
                    <span className="xml-name">bindingPort</span>
                    <span className="xml-type">integer</span>
                    <span className="xml-required">required</span>
                </div>
                <p>Defines the port where the backend API will listen to requests on.</p>
                <input
                    type="text"
                    className="input-field"
                    placeholder="Binding Port"
                    value={bindingPort}
                    onChange={(e) => {
                        setBindingPort(e.target.value);
                        validateBindingPort(e.target.value);
                    }}
                    required />
                <button
                    className="stage-button stage"
                    onClick={() => {
                        setBindingPort("5000");
                        setPortIsValid(true);
                    }}
                >
                    Apply Default
                </button>
                {!portIsValid && (
                    <div className="password-error">
                        Port must be between 0 - 65535.
                    </div>
                )}
            </div>

            <div className="xml-attr">
                <div className="xml-attr-header">
                    <span className="xml-name">jwtSecretKey</span>
                    <span className="xml-type">string</span>
                    <span className="xml-required">required</span>
                </div>
                <p>Cryptographic string or key pair used to sign and verify JSON Web Tokens, ensuring the token's authenticity and integrity.</p>
            <input
                type={jwtIsConfigured ? "password" : "text"}
                className="input-field"
                placeholder={jwtIsConfigured ? "Already Configured" : "Enter JWT Secret Key"}
                value={jwtSecretKey}
                onChange={(e) => {
                    setJwtSecretKey(e.target.value);
                    validateJwt(e.target.value);
                }}
                required />
            <button
                className="stage-button stage"
                onClick={() => {
                    setJwtSecretKey(generateRefreshToken());
                    setJwtIsValid(true);
                }}
            >
                Apply Default
            </button>
            {!jwtIsValid && (
                <div className="password-error">
                    JWT Secret Key must be greater or equal to 16 characters.
                </div>
            )}
            </div>

            <div className="xml-attr">
                <div className="xml-attr-header">
                    <span className="xml-name">globalWorkspacePath</span>
                    <span className="xml-type">string</span>
                    <span className="xml-required">required</span>
                </div>
                <p>The user-defined workspace variable where all repositories will be stored.</p>
            <input
                type="text"
                className="input-field"
                placeholder="Workspace directory"
                value={globalWorkspacePath}
                onChange={(e) => setGlobalWorkspacePath(e.target.value)}
                required />
            {(!globalWorkspacePath || globalWorkspacePath.length === 0) && (
                <div className="password-error">
                    Global workspace directory is required.
                </div>
            )}
            </div>

            <div className="xml-attr">
                <div className="xml-attr-header">
                    <span className="xml-name">gitHubUsername</span>
                    <span className="xml-type">string</span>
                    <span className="xml-optional">optional</span>
                </div>
                <p>Your GitHub user name.</p>
            <input
                    type="text"
                    className="input-field"
                    placeholder="GitHub username"
                    value={gitHubUsername}
                    onChange={(e) => setGitHubUsername(e.target.value)} />
            </div>

            <div className="xml-attr">
                <div className="xml-attr-header">
                    <span className="xml-name">gitHubSshPrivateKeyPath</span>
                    <span className="xml-type">string</span>
                    <span className="xml-optional">optional</span>
                </div>
                <p>The path to the SSH private key for your GitHub account.</p>
            <input
                    type="text"
                    className="input-field"
                    placeholder="GitHub account SSH Private Key Path"
                    value={gitHubSshPrivateKeyPath}
                    onChange={(e) => setGitHubSshPrivateKeyPath(e.target.value)} />
            </div>

            <div className="xml-attr">
                <div className="xml-attr-header">
                    <span className="xml-name">gitHubSshPassphrase</span>
                    <span className="xml-type">string</span>
                    <span className="xml-optional">optional</span>
                </div>
                <p>The passphrase to the SSH private key for your GitHub account.</p>
            <input
                    type="text"
                    className="input-field"
                    placeholder="GitHub account SSH Private Key Passphrase"
                    value={gitHubSshPassphrase}
                    onChange={(e) => setGitHubSshPassphrase(e.target.value)} />
            </div>

            <div className="xml-attr">
                <div className="xml-attr-header">
                    <span className="xml-name">gitHubApiToken</span>
                    <span className="xml-type">string</span>
                    <span className="xml-optional">optional</span>
                </div>
                <p>Defines the GitHub API token that is used for access and performing operations with the Octokit GitHub development package.</p>
                <input
                    type={gitHubApiTokenIsConfigured ? "password" : "text"}
                    className="input-field"
                    placeholder={gitHubApiTokenIsConfigured ? "Already Configured" : "Paste GitHub API Access Token"}
                    value={gitHubApiToken}
                    onChange={(e) => setGitHubApiToken(e.target.value)} />
            </div>

            <div className="xml-attr">
                <div className="xml-attr-header">
                    <span className="xml-name">workflowRunReportThreshold</span>
                    <span className="xml-type">integer</span>
                    <span className="xml-optional">optional</span>
                </div>
                <p>Defines the maximum number of workflow runs to report to the web application.</p>
                <input
                    type="text"
                    className="input-field"
                    placeholder="Workflow Threshold"
                    value={workflowRunReportThreshold}
                    onChange={(e) => setWorkflowRunReportThreshold(e.target.value)} />
                {!isValidInteger(workflowRunReportThreshold) && (
                        <div className="password-error">
                            Must be a valid integer greater than 0. May be skipped if not wanting to use.
                        </div>
                )}
            </div>

            <div className="xml-attr">
                <div className="xml-attr-header">
                    <span className="xml-name">gitHubPullRequestScanIntervalSeconds</span>
                    <span className="xml-type">integer</span>
                    <span className="xml-optional">optional</span>
                </div>
                <p>Defines the interval in seconds at which GitHub pull requests are scanned for updates.</p>
                <input
                    type="text"
                    className="input-field"
                    placeholder="Pull Request Interval"
                    value={gitHubPullRequestScanIntervalSeconds}
                    onChange={(e) => setGitHubPullRequestScanIntervalSeconds(e.target.value)} />
                {!isValidInteger(gitHubPullRequestScanIntervalSeconds) && (
                    <div className="password-error">
                        Must be a valid integer greater than 0. May be skipped if not wanting to use.
                    </div>
                )}
            </div>

            <div className="xml-attr">
                <div className="xml-attr-header">
                    <span className="xml-name">gitLabUsername</span>
                    <span className="xml-type">string</span>
                    <span className="xml-optional">optional</span>
                </div>
                <p>Your GitLab user name.</p>
            <input
                    type="text"
                    className="input-field"
                    placeholder="GitLab username"
                    value={gitLabUsername}
                    onChange={(e) => setGitLabUsername(e.target.value)} />
            </div>

            <div className="xml-attr">
                <div className="xml-attr-header">
                    <span className="xml-name">gitLabSshPrivateKeyPath</span>
                    <span className="xml-type">string</span>
                    <span className="xml-optional">optional</span>
                </div>
                <p>The path to the SSH private key for your GitLab account.</p>
            <input
                    type="text"
                    className="input-field"
                    placeholder="GitLab account SSH Private Key Path"
                    value={gitLabSshPrivateKeyPath}
                    onChange={(e) => setGitLabSshPrivateKeyPath(e.target.value)} />
            </div>

            <div className="xml-attr">
                <div className="xml-attr-header">
                    <span className="xml-name">gitLabSshPassphrase</span>
                    <span className="xml-type">string</span>
                    <span className="xml-optional">optional</span>
                </div>
                <p>The passphrase to the SSH private key for your GitLab account.</p>
            <input
                    type="text"
                    className="input-field"
                    placeholder="GitLab account SSH Private Key Passphrase"
                    value={gitHubSshPassphrase}
                    onChange={(e) => setGitLabSshPassphrase(e.target.value)} />
            </div>

            <div className="xml-attr">
                <div className="xml-attr-header">
                    <span className="xml-name">gitlabApiToken</span>
                    <span className="xml-type">string</span>
                    <span className="xml-optional">optional</span>
                </div>
                <p>Defines the GitLab API token that is used for access and performing operations with the NGitLab development package.</p>
                <input
                    type={gitLabApiTokenIsConfigured ? "password" : "text"}
                    className="input-field"
                    placeholder={gitLabApiTokenIsConfigured ? "Already Configured" : "Paste GitLab API Access Token"}
                    value={gitlabApiToken}
                    onChange={(e) => setGitLabApiToken(e.target.value)} />
            </div>

            <div className="xml-attr">
                <div className="xml-attr-header">
                    <span className="xml-name">selfHostedGitLabUrl</span>
                    <span className="xml-type">string</span>
                    <span className="xml-optional">optional</span>
                </div>
                <p>GitLab absolute URL (with or without the /api/v* path).</p>
            <input
                    type="text"
                    className="input-field"
                    placeholder="Self Hosted GitLab Url"
                    value={selfHostedGitLabUrl}
                    onChange={(e) => setSelfHostedGitLabUrl(e.target.value)} />
            </div>

            <div className="xml-attr">
                <div className="xml-attr-header">
                    <span className="xml-name">gitLabMergeRequestScanIntervalSeconds</span>
                    <span className="xml-type">integer</span>
                    <span className="xml-optional">optional</span>
                </div>
                <p>Defines the interval in seconds at which GitLab merge requests are scanned for updates.</p>
                <input
                    type="text"
                    className="input-field"
                    placeholder="Merge Request Interval"
                    value={gitLabMergeRequestScanIntervalSeconds}
                    onChange={(e) => setGitLabMergeRequestScanIntervalSeconds(e.target.value)} />
                {!isValidInteger(gitLabMergeRequestScanIntervalSeconds) && (
                    <div className="password-error">
                        Must be a valid integer greater than 0. May be skipped if not wanting to use.
                    </div>
                )}
            </div>

            <button
                className="submit-button"
                type="submit"
                disabled={disableSendButton}
                onClick={handleApplicationConfiguration}
            >
                Apply Configuration
            </button>
        </div>
    );
};

export default AppSetupPage;
