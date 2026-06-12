import { useEffect, useState } from "react";
import { useCacheStore } from "../../managers/CacheManager";
import { isBlankOrUndefined } from "../../stringHelperUtil";
import { ChasmaWebApiConfigurations, ModifyApiConfigRequest } from "../../API/ChasmaWebApiClient";
import { appConfigClient } from "../../managers/ApiClientManager";
import { useNavigate } from "react-router-dom";
import { useDocumentTitle } from "../../util/useDocumentTitle";

/**
 * Initializes a new instance of the parent AppSetupPage class.
 * @constructor
 */
export const AppSetupPage: React.FC = () => {
    useDocumentTitle("System Settings");
    
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

    /** Gets or sets the secure API binding port. */
    const [secureBindingPort, setSecureBindingPort] = useState<string | undefined>(undefined);

    /** Gets or sets a value indicating whether the binding port is valid. */
    const [bindingPortIsValid, setBindingPortIsValid] = useState<boolean | undefined>(undefined);

    /** Gets or sets a value indicating whether the binding secured port is valid. */
    const [secureBindingPortIsValid, setSecureBindingPortIsValid] = useState<boolean | undefined>(undefined);

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
        setBindingPortIsValid(isValidPortNumber);
    };

    /** Gets a flag indicating whether secure binding port is valid. **/
    function validateSecureBindingPort(value: string | undefined): void {
        const num = Number(value);
        const isValidPortNumber = isValidInteger(value) && num >= 0 && num <= 65356
        setSecureBindingPortIsValid(isValidPortNumber);
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

        if (isBlankOrUndefined(secureBindingPort)) {
            setNotification({
                title: "Could not apply configurations!",
                message: "'secureBindingPort' must be populated!",
                isError: true,
            });
            return;
        }

        if (!isValidInteger(secureBindingPort)) {
            setNotification({
                title: "Could not apply configurations!",
                message: "'secureBindingPort' must be a valid integer!",
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
        config.secureBindingPort = safeNumber(secureBindingPort);
        config.bindingPort = safeNumber(bindingPort);
        config.gitHubApiToken = gitHubApiToken;
        config.workflowRunReportThreshold = safeNumber(workflowRunReportThreshold);
        config.gitHubPullRequestScanIntervalSeconds = safeNumber(gitHubPullRequestScanIntervalSeconds);
        config.gitLabApiToken = gitlabApiToken;
        config.selfHostedGitLabUrl = selfHostedGitLabUrl;
        config.gitLabMergeRequestScanIntervalSeconds = safeNumber(gitLabMergeRequestScanIntervalSeconds);
        config.gitHubUsername = gitHubUsername;
        config.gitLabUsername = gitLabUsername;
        config.globalWorkspacePath = globalWorkspacePath;
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
                    message: "Binding Port, JWT Secret Key, and/or Global Workspace Path key has been updated. Restart the app to apply changes.",
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
                        title: "Setup Emryce System",
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
                    setBindingPortIsValid(true);
                }

                setSecureBindingPort(String(response.secureBindingPort));
                const securePort = response.secureBindingPort;
                if (securePort && securePort > 0 && securePort <= 65535) {
                    setSecureBindingPortIsValid(true);
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
            <h1 className="page-title">System Settings</h1>

            {/* Secure Binding Port */}
            <div className="xml-attr">
                <div className="xml-attr-header">
                    <span className="xml-name">App Port Number</span>
                    <span className="xml-type">Numbers only</span>
                    <span className="xml-required">Required</span>
                </div>
                <p>The HTTPS network port where this application will run. If you aren't sure, the default port 7200 usually works perfectly.</p>
                <input
                    type="text"
                    className="input-field"
                    placeholder="e.g., 7200"
                    value={secureBindingPort}
                    onChange={(e) => {
                        setSecureBindingPort(e.target.value);
                        validateSecureBindingPort(e.target.value);
                    }}
                    required />
                <button
                    className="stage-button stage"
                    onClick={() => {
                        setSecureBindingPort("7200");
                        setSecureBindingPortIsValid(true);
                    }}
                >
                    Use Default (7200)
                </button>
                {!secureBindingPortIsValid && (
                    <div className="password-error">
                        Please enter a valid port number between 0 and 65535.
                    </div>
                )}
            </div>

            {/* Binding Port */}
            <div className="xml-attr">
                <div className="xml-attr-header">
                    <span className="xml-name">Fallback App Port Number</span>
                    <span className="xml-type">Numbers only</span>
                    <span className="xml-required">Required</span>
                </div>
                <p>The fallback network port where this application will run. If you aren't sure, the default port 5000 usually works perfectly.</p>
                <input
                    type="text"
                    className="input-field"
                    placeholder="e.g., 5000"
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
                        setBindingPortIsValid(true);
                    }}
                >
                    Use Default (5000)
                </button>
                {!bindingPortIsValid && (
                    <div className="password-error">
                        Please enter a valid port number between 0 and 65535.
                    </div>
                )}
            </div>

            {/* JWT Secret Key */}
            <div className="xml-attr">
                <div className="xml-attr-header">
                    <span className="xml-name">Security Security Key (JWT)</span>
                    <span className="xml-type">Text</span>
                    <span className="xml-required">Required</span>
                </div>
                <p>A secret password used behind the scenes to keep your login sessions safe and secure. You can type your own or click below to generate a secure one.</p>
                <input
                    type={jwtIsConfigured ? "password" : "text"}
                    className="input-field"
                    placeholder={jwtIsConfigured ? "Saved & Encrypted" : "Enter a secure secret key"}
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
                    Generate Secure Key
                </button>
                {!jwtIsValid && (
                    <div className="password-error">
                        Your secret key must be at least 16 characters long to keep your app safe.
                    </div>
                )}
            </div>

            {/* Global Workspace Path */}
            <div className="xml-attr">
                <div className="xml-attr-header">
                    <span className="xml-name">Storage Folder Path</span>
                    <span className="xml-type">Folder Path</span>
                    <span className="xml-required">Required</span>
                </div>
                <p>The main folder on your computer or server where all downloaded code repositories will be stored.</p>
                <input
                    type="text"
                    className="input-field"
                    placeholder="e.g., /home/user/projects or C:\Projects"
                    value={globalWorkspacePath}
                    onChange={(e) => setGlobalWorkspacePath(e.target.value)}
                    required />
                {(!globalWorkspacePath || globalWorkspacePath.length === 0) && (
                    <div className="password-error">
                        Please specify a folder path to save your work.
                    </div>
                )}
            </div>

            {/* GitHub Username */}
            <div className="xml-attr">
                <div className="xml-attr-header">
                    <span className="xml-name">GitHub Username</span>
                    <span className="xml-type">Text</span>
                    <span className="xml-optional">Optional</span>
                </div>
                <p>Your personal or organization username on GitHub.</p>
                <input
                    type="text"
                    className="input-field"
                    placeholder="e.g., octocat"
                    value={gitHubUsername}
                    onChange={(e) => setGitHubUsername(e.target.value)} />
            </div>

            {/* GitHub SSH Private Key Path */}
            <div className="xml-attr">
                <div className="xml-attr-header">
                    <span className="xml-name">GitHub SSH Key Location</span>
                    <span className="xml-type">File Path</span>
                    <span className="xml-optional">Optional</span>
                </div>
                <p>The file path to your GitHub private SSH key if you connect over SSH instead of using tokens.</p>
                <input
                    type="text"
                    className="input-field"
                    placeholder="e.g., /home/user/.ssh/id_rsa"
                    value={gitHubSshPrivateKeyPath}
                    onChange={(e) => setGitHubSshPrivateKeyPath(e.target.value)} />
            </div>

            {/* GitHub SSH Passphrase */}
            <div className="xml-attr">
                <div className="xml-attr-header">
                    <span className="xml-name">GitHub SSH Key Password</span>
                    <span className="xml-type">Text</span>
                    <span className="xml-optional">Optional</span>
                </div>
                <p>The password (passphrase) that unlocks your GitHub SSH key, if you set one up.</p>
                <input
                    type="text"
                    className="input-field"
                    placeholder="Leave blank if your key doesn't have a password"
                    value={gitHubSshPassphrase}
                    onChange={(e) => setGitHubSshPassphrase(e.target.value)} />
            </div>

            {/* GitHub API Token */}
            <div className="xml-attr">
                <div className="xml-attr-header">
                    <span className="xml-name">GitHub Personal Access Token</span>
                    <span className="xml-type">Text</span>
                    <span className="xml-optional">Optional</span>
                </div>
                <p>Your GitHub Personal Access Token (PAT). This lets the app securely talk to your GitHub account to sync repositories and track workflows.</p>
                <input
                    type={gitHubApiTokenIsConfigured ? "password" : "text"}
                    className="input-field"
                    placeholder={gitHubApiTokenIsConfigured ? "Saved & Encrypted" : "Paste ghp_ token here"}
                    value={gitHubApiToken}
                    onChange={(e) => setGitHubApiToken(e.target.value)} />
            </div>

            {/* Workflow Run Report Threshold */}
            <div className="xml-attr">
                <div className="xml-attr-header">
                    <span className="xml-name">Max Workflow History Limit</span>
                    <span className="xml-type">Numbers only</span>
                    <span className="xml-optional">Optional</span>
                </div>
                <p>The maximum number of recent workflow runs to look up and show on your dashboard at one time.</p>
                <input
                    type="text"
                    className="input-field"
                    placeholder="e.g., 20 (Leave blank for no limit)"
                    value={workflowRunReportThreshold}
                    onChange={(e) => setWorkflowRunReportThreshold(e.target.value)} />
                {!isValidInteger(workflowRunReportThreshold) && (
                    <div className="password-error">
                        Please enter a whole number greater than 0, or leave it blank to skip.
                    </div>
                )}
            </div>

            {/* GitHub Pull Request Scan Interval */}
            <div className="xml-attr">
                <div className="xml-attr-header">
                    <span className="xml-name">GitHub Pull Request Refresh Rate</span>
                    <span className="xml-type">Seconds</span>
                    <span className="xml-optional">Optional</span>
                </div>
                <p>How often (in seconds) the application checks GitHub for new pull request changes.</p>
                <input
                    type="text"
                    className="input-field"
                    placeholder="e.g., 60 (checks every minute)"
                    value={gitHubPullRequestScanIntervalSeconds}
                    onChange={(e) => setGitHubPullRequestScanIntervalSeconds(e.target.value)} />
                {!isValidInteger(gitHubPullRequestScanIntervalSeconds) && (
                    <div className="password-error">
                        Please enter a valid number of seconds greater than 0, or leave it blank to skip.
                    </div>
                )}
            </div>

            {/* GitLab Username */}
            <div className="xml-attr">
                <div className="xml-attr-header">
                    <span className="xml-name">GitLab Username</span>
                    <span className="xml-type">Text</span>
                    <span className="xml-optional">Optional</span>
                </div>
                <p>Your personal or organization username on GitLab.</p>
                <input
                    type="text"
                    className="input-field"
                    placeholder="e.g., gitlab_user"
                    value={gitLabUsername}
                    onChange={(e) => setGitLabUsername(e.target.value)} />
            </div>

            {/* GitLab SSH Private Key Path */}
            <div className="xml-attr">
                <div className="xml-attr-header">
                    <span className="xml-name">GitLab SSH Key Location</span>
                    <span className="xml-type">File Path</span>
                    <span className="xml-optional">Optional</span>
                </div>
                <p>The file path to your GitLab private SSH key if you connect over SSH instead of using tokens.</p>
                <input
                    type="text"
                    className="input-field"
                    placeholder="e.g., /home/user/.ssh/id_rsa"
                    value={gitLabSshPrivateKeyPath}
                    onChange={(e) => setGitLabSshPrivateKeyPath(e.target.value)} />
            </div>

            {/* GitLab SSH Passphrase */}
            <div className="xml-attr">
                <div className="xml-attr-header">
                    <span className="xml-name">GitLab SSH Key Password</span>
                    <span className="xml-type">Text</span>
                    <span className="xml-optional">Optional</span>
                </div>
                <p>The password (passphrase) that unlocks your GitLab SSH key, if you set one up.</p>
                <input
                    type="text"
                    className="input-field"
                    placeholder="Leave blank if your key doesn't have a password"
                    value={gitLabSshPassphrase}
                    onChange={(e) => setGitLabSshPassphrase(e.target.value)} />
            </div>

            {/* GitLab API Token */}
            <div className="xml-attr">
                <div className="xml-attr-header">
                    <span className="xml-name">GitLab Personal Access Token</span>
                    <span className="xml-type">Text</span>
                    <span className="xml-optional">Optional</span>
                </div>
                <p>Your GitLab Personal Access Token. This lets the app securely check your GitLab repositories and merge requests.</p>
                <input
                    type={gitLabApiTokenIsConfigured ? "password" : "text"}
                    className="input-field"
                    placeholder={gitLabApiTokenIsConfigured ? "Saved & Encrypted" : "Paste glpat- token here"}
                    value={gitlabApiToken}
                    onChange={(e) => setGitLabApiToken(e.target.value)} />
            </div>

            {/* Self-Hosted GitLab URL */}
            <div className="xml-attr">
                <div className="xml-attr-header">
                    <span className="xml-name">Custom GitLab Website URL</span>
                    <span className="xml-type">Web Address</span>
                    <span className="xml-optional">Optional</span>
                </div>
                <p>If you use a private, custom-hosted version of GitLab instead of the public GitLab.com, enter your server's web address here.</p>
                <input
                    type="text"
                    className="input-field"
                    placeholder="e.g., https://gitlab.mycompany.com"
                    value={selfHostedGitLabUrl}
                    onChange={(e) => setSelfHostedGitLabUrl(e.target.value)} />
            </div>

            {/* GitLab Merge Request Scan Interval */}
            <div className="xml-attr">
                <div className="xml-attr-header">
                    <span className="xml-name">GitLab Merge Request Refresh Rate</span>
                    <span className="xml-type">Seconds</span>
                    <span className="xml-optional">Optional</span>
                </div>
                <p>How often (in seconds) the application checks GitLab for new merge request changes.</p>
                <input
                    type="text"
                    className="input-field"
                    placeholder="e.g., 60 (checks every minute)"
                    value={gitLabMergeRequestScanIntervalSeconds}
                    onChange={(e) => setGitLabMergeRequestScanIntervalSeconds(e.target.value)} />
                {!isValidInteger(gitLabMergeRequestScanIntervalSeconds) && (
                    <div className="password-error">
                        Please enter a valid number of seconds greater than 0, or leave it blank to skip.
                    </div>
                )}
            </div>

            <button
                className="submit-button"
                type="submit"
                disabled={disableSendButton}
                onClick={handleApplicationConfiguration}
            >
                Save Settings
            </button>
        </div>
    );
};

export default AppSetupPage;
