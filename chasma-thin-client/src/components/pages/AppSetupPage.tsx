import { useState } from "react";
import { useCacheStore } from "../../managers/CacheManager";
import { isBlankOrUndefined } from "../../stringHelperUtil";
import Checkbox from "../Checkbox";
import { ChasmaWebApiConfigurations, ModifyApiConfigRequest } from "../../API/ChasmaWebApiClient";
import { appConfigClient } from "../../managers/ApiClientManager";
import { handleApiError } from "../../managers/TransactionHandlerManager";

/**
 * Initializes a new instance of the parent AppSetupPage class.
 * @constructor
 */
export const AppSetupPage: React.FC = () => {
    /** Sets the notification modal. */
    const setNotification = useCacheStore(state => state.setNotification);

    /** Gets or sets the web API url. */
    const [webApiUrl, setWebApiUrl] = useState<string | undefined>(undefined);

    /** Gets or sets the thin client url. */
    const [thinClientUrl, setThinClientUrl] = useState<string | undefined>(undefined);

    /** Gets or sets the JWT secret key. */
    const [jwtSecretKey, setJwtSecretKey] = useState<string | undefined>(undefined);

    /** Gets or sets a value indicating whether the JWT secret key is valid. */
    const [jwtIsValid, setJwtIsValid] = useState<boolean | undefined>(undefined);

    /** Gets or sets the API binding port. */
    const [bindingPort, setBindingPort] = useState<string | undefined>(undefined);

    /** Gets or sets a value indicating whether the binding port is valid. */
    const [portIsValid, setPortIsValid] = useState<boolean | undefined>(undefined);

    /** Gets or sets the GitHub API access token. */
    const [gitHubApiToken, setGitHubApiToken] = useState<string | undefined>(undefined);

    /** Gets or sets the workflow run report threshold. */
    const [workflowRunReportThreshold, setWorkflowRunReportThreshold] = useState<string | undefined>(undefined);

    /** Gets or sets the GitHub pull request scan interval. */
    const [gitHubPullRequestScanIntervalSeconds, setGitHubPullRequestScanIntervalSeconds] = useState<string | undefined>(undefined);

    /** Gets or sets the GitLab API access token. */
    const [gitlabApiToken, setGitLabApiToken] = useState<string | undefined>(undefined);

    /** Gets or sets the self hosted GitLab url. */
    const [selfHostedGitLabUrl, setSelfHostedGitLabUrl] = useState<string | undefined>(undefined);

    /** Gets or sets the GitLab merge request scan interval in seconds. */
    const [gitLabMergeRequestScanIntervalSeconds, setGitLabMergeRequestScanIntervalSeconds] = useState<string | undefined>(undefined);

    /** Gets or sets the BitBucket API access token. */
    const [bitbucketApiToken, setBitbucketApiToken] = useState<string | undefined>(undefined);

    /** Gets or sets a value indicating whether the request is ready to be sent. */
    const [disableSendButton, setDisableSendButton] = useState(false);

    /** Gets or sets a value indicating whether the user is using default configuration values. */
    const [isUsingDefault, setIsUsingDefault] = useState<boolean>(false);

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
        if (isBlankOrUndefined(webApiUrl)) {
            setNotification({
                    title: "Could not apply configurations!",
                    message: "'webApiUrl' must be populated!",
                    isError: true,
                });
            return;
        }

        if (isBlankOrUndefined(thinClientUrl)) {
            setNotification({
                    title: "Could not apply configurations!",
                    message: "'thinClientUrl' must be populated!",
                    isError: true,
                });
            return;
        }

        if (isBlankOrUndefined(jwtSecretKey)) {
            setNotification({
                    title: "Could not apply configurations!",
                    message: "'jwtSecretKey' must be populated!",
                    isError: true,
                });
            return;
        }

        if (jwtSecretKey && jwtSecretKey.length < 16) {
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

        if (!isUsingDefault) {
            if (isBlankOrUndefined(workflowRunReportThreshold) || !isValidInteger(workflowRunReportThreshold)) {
                setNotification({
                        title: "Could not apply configurations!",
                        message: "'workflowRunReportThreshold' must be an integer greater than 0!",
                        isError: true,
                    });
                return;
            }

            if (isBlankOrUndefined(gitHubPullRequestScanIntervalSeconds) || !isValidInteger(gitHubPullRequestScanIntervalSeconds)) {
                setNotification({
                        title: "Could not apply configurations!",
                        message: "'gitHubPullRequestScanIntervalSeconds' must be an integer greater than 0!",
                        isError: true,
                    });
                return;
            }

            if (isBlankOrUndefined(gitLabMergeRequestScanIntervalSeconds) || !isValidInteger(gitLabMergeRequestScanIntervalSeconds)) {
                setNotification({
                        title: "Could not apply configurations!",
                        message: "'gitLabMergeRequestScanIntervalSeconds' must be an integer greater than 0!",
                        isError: true,
                    });
                return;
            }
        }

        await sendModifyConfigurationRequest();
    };

    /**
     * Sends the modify configuration request.
     */
    const sendModifyConfigurationRequest = async () => {
        const config = new ChasmaWebApiConfigurations();
        config.webApiUrl = webApiUrl;
        config.thinClientUrl = thinClientUrl;
        config.jwtSecretKey = jwtSecretKey;
        config.bindingPort = safeNumber(bindingPort);
        config.gitHubApiToken = gitHubApiToken;
        config.workflowRunReportThreshold = safeNumber(workflowRunReportThreshold);
        config.gitHubPullRequestScanIntervalSeconds = safeNumber(gitHubPullRequestScanIntervalSeconds);
        config.gitLabApiToken = gitlabApiToken;
        config.selfHostedGitLabUrl = selfHostedGitLabUrl;
        config.gitLabMergeRequestScanIntervalSeconds = safeNumber(gitLabMergeRequestScanIntervalSeconds);
        config.bitbucketApiToken = bitbucketApiToken;
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
                return;
            }

            setNotification({
                title: "Successfully applied configurations!",
                message: "Restart the app to apply changes.",
                isError: false,
            });
        }
        catch (e) {
            console.error(e);
            setNotification({
                title: "Error applying configurations!",
                message: "Review console logs for more information.",
                isError: true,
            });
        }
    };

    const safeNumber = (value?: string) =>
    value && !isNaN(Number(value)) ? Number(value) : undefined;

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

    /**
     * Handles the event when the user wants to check/uncheck default configurations.
     * @param isDefault Flag indicating whether the using is choosing default configurations.
     */
    const handleDefaultConfiguration = (isDefault: boolean) => {
        setIsUsingDefault(isDefault);
        if (isDefault) {
            setWebApiUrl("http://localhost:5000");
            setThinClientUrl("http://localhost:5000");
            setJwtSecretKey(generateRefreshToken());
            setBindingPort("5000");
            validateBindingPort("5000");
        }
        else {
            setWebApiUrl("");
            setThinClientUrl("");
            setJwtSecretKey("");
            setBindingPort("");
            validateBindingPort("");
        }

        setGitHubApiToken(undefined);
        setWorkflowRunReportThreshold(undefined);
        setGitHubPullRequestScanIntervalSeconds(undefined);
        setGitLabApiToken(undefined);
        setSelfHostedGitLabUrl(undefined);
        setGitLabMergeRequestScanIntervalSeconds(undefined);
        setBitbucketApiToken(undefined);
    }

    return (
        <div className="panel-card">
            <h1 className="page-title">Setup Chasma System</h1>
            <h2 className="page-description">The system is not configured. After saving your changes, please restart the application for them to take effect.</h2>
            <Checkbox
                label={"Use Default Configuration"}
                onBoxChecked={handleDefaultConfiguration}
                checked={isUsingDefault}
                tooltip="Apply the default configuration for basic functionality." />
            {!isUsingDefault && (
                <>
                    <div className="xml-attr">
                    <div className="xml-attr-header">
                        <span className="xml-name">webApiUrl</span>
                        <span className="xml-type">string</span>
                        <span className="xml-required">required</span>
                    </div>
                    <p>Defines the URL where this web application will be running and sending requests to the Web API.</p>
                    <input
                        type="text"
                        className="input-field"
                        placeholder="Web API Url"
                        value={webApiUrl}
                        onChange={(e) => setWebApiUrl(e.target.value)}
                        required />
                    </div>

                    <div className="xml-attr">
                        <div className="xml-attr-header">
                            <span className="xml-name">thinClientUrl</span>
                            <span className="xml-type">string</span>
                            <span className="xml-required">required</span>
                        </div>
                        <p>Defines the URL where this web application will be running and sending requests to the Web API.</p>
                        <input
                            type="text"
                            className="input-field"
                            placeholder="Thin Client Url"
                            value={thinClientUrl}
                            onChange={(e) => setThinClientUrl(e.target.value)}
                            required />
                    </div>

                    <div className="xml-attr">
                        <div className="xml-attr-header">
                            <span className="xml-name">jwtSecretKey</span>
                            <span className="xml-type">string</span>
                            <span className="xml-required">required</span>
                        </div>
                        <p>Cryptographic string or key pair used to sign and verify JSON Web Tokens, ensuring the token's authenticity and integrity.</p>
                    <input
                            type="text"
                            className="input-field"
                            placeholder="JWT Secret Key"
                            value={jwtSecretKey}
                            onChange={(e) => {
                                setJwtSecretKey(e.target.value);
                                validateJwt(e.target.value);
                            }}
                            required />
                    {!jwtIsValid && (
                        <div className="password-error">
                            JWT Secret Key must be greater or equal to 16 characters.
                        </div>
                    )}
                    </div>

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
                        {!portIsValid && (
                            <div className="password-error">
                                Port must be between 0 - 65535.
                            </div>
                        )}
                    </div>

                    <div className="xml-attr">
                        <div className="xml-attr-header">
                            <span className="xml-name">gitHubApiToken</span>
                            <span className="xml-type">string</span>
                            <span className="xml-optional">optional</span>
                        </div>
                        <p>Defines the GitHub API token that is used for access and performing operations with the Octokit GitHub development package.</p>
                        <input
                            type="text"
                            className="input-field"
                            placeholder="GitHub API Access Token"
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
                            <span className="xml-name">gitlabApiToken</span>
                            <span className="xml-type">string</span>
                            <span className="xml-optional">optional</span>
                        </div>
                        <p>Defines the GitLab API token that is used for access and performing operations with the NGitLab development package.</p>
                        <input
                            type="text"
                            className="input-field"
                            placeholder="GitLab API Access Token"
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

                    <div className="xml-attr">
                        <div className="xml-attr-header">
                            <span className="xml-name">bitbucketApiToken</span>
                            <span className="xml-type">string</span>
                            <span className="xml-optional">optional</span>
                        </div>
                        <p>Defines the GitLab API token that is used for access and performing operations with the Atlassian.Net SDK development package.</p>
                        <input
                            type="text"
                            className="input-field"
                            placeholder="Bitbucket Access Token"
                            value={bitbucketApiToken}
                            onChange={(e) => setBitbucketApiToken(e.target.value)} />
                    </div>
                </>
            )}
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
