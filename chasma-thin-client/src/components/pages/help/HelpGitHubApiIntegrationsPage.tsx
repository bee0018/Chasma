import React from "react";

/**
 * Initializes a new instance of the HelpGitHubApiIntegrationsPage component.
 * @constructor
 */
const HelpGitHubApiIntegrationsPage: React.FC = () => (
    <section id="github-api" className="panel-card">
        <h2>GitHub API Integration</h2>
        <div className="help-subsection">
            <h3 className="help-subsection-title">Classic Token Approach</h3>
            <ul className="help-steps">

                <li>
                    <span className="help-step-index">1</span>
                    <div>
                        <strong>Log in to GitHub</strong>
                        <p>
                            Go to{' '}
                            <a
                                href="https://github.com"
                                target="_blank"
                                rel="noopener noreferrer"
                                style={{color: '#00bfff', textDecoration: 'none'}}
                            >
                                GitHub
                            </a>{' '}
                            and log in to your account.
                        </p>
                    </div>
                </li>

                <li>
                    <span className="help-step-index">2</span>
                    <div>
                        <strong>Go to Developer Settings</strong>
                        <p>Click your profile picture in the top-right corner → select Settings.</p>
                        <p>In the left sidebar, scroll down to <strong>Developer settings</strong>.</p>
                    </div>
                </li>

                <li>
                    <span className="help-step-index">3</span>
                    <div>
                        <strong>Go to Personal Access Tokens</strong>
                        <p>Click <strong>Personal access tokens</strong>.</p>
                        <p>Then select <strong>Tokens (classic)</strong> (or the newer fine-grained tokens if you want more control).</p>
                    </div>
                </li>

                <li>
                    <span className="help-step-index">4</span>
                    <div>
                        <strong>Generate a New Token</strong>
                        <p>Click <strong>Generate new token</strong>.</p>
                        <p>Choose <strong>classic token</strong> or <strong>fine-grained token</strong>.</p>
                        <ul>
                            <li>- <strong>Classic token:</strong>simpler, full access based on scopes.</li>
                            <li>- <strong>Fine-grained token:</strong>more secure, lets you select exactly which repos/actions it can access.</li>
                        </ul>
                    </div>
                </li>

                <li>
                    <span className="help-step-index">5</span>
                    <div>
                        <strong>Configure Token</strong>
                        <p><strong>Name your token</strong>: (e.g., <code>MyDevToken</code>).</p>
                        <p><strong>Expiration</strong>: Set how long the token will be valid (recommended: 30 days or custom).</p>
                        <p><strong>Scopes / Permissions</strong>: Choose what the token can do:</p>
                        <ul>
                            <li><code>repo</code>Full control of private and public repositories.</li>
                            <li><code>workflow</code>Access to GitHub Actions.</li>
                            <li><code>read:user</code>Read your profile info.</li>
                            <li>Only select what you actually need for security.</li>
                        </ul>
                    </div>
                </li>

                <li>
                    <span className="help-step-index">6</span>
                    <div>
                        <strong>Generate and Copy</strong>
                        <p>Click <strong>Generate token</strong>.</p>
                        <p><strong>Important</strong>: Copy the token <strong>IMMEDIATELY!</strong> GitHub will not show it again.</p>
                    </div>
                </li>

                <li>
                    <span className="help-step-index">7</span>
                    <div>
                        <strong>Add the Token to the Web API </strong>
                        <p>Go to your Web API location and locate your <code>config.xml</code> file.</p>
                        <p>Paste your token in the <code>gitHubApiToken</code> attribute field.</p>
                        <p>Start/Restart the Web API service.</p>
                    </div>
                </li>
            </ul>
        </div>
        <div className="help-subsection">
            <h3 className="help-subsection-title">Fine-Grained Token Approach</h3>
            <ul className="help-steps">

                <li>
                    <span className="help-step-index">1</span>
                    <div>
                        <strong>Log in to GitHub</strong>
                        <p>
                            Go to{' '}
                            <a
                                href="https://github.com"
                                target="_blank"
                                rel="noopener noreferrer"
                                style={{color: '#00bfff', textDecoration: 'none'}}
                            >
                                GitHub
                            </a>{' '}
                            and log in to your account.
                        </p>
                    </div>
                </li>

                <li>
                    <span className="help-step-index">2</span>
                    <div>
                        <strong>Go to Developer Settings</strong>
                        <p>Click your profile picture in the top-right corner and select <strong>→ Settings → Developer settings → Personal access tokens → Fine-grained tokens</strong>.</p>
                    </div>
                </li>

                <li>
                    <span className="help-step-index">3</span>
                    <div>
                        <strong>Click “Generate new token”</strong>
                        <p>You’ll see options for fine-grained tokens. Click <strong>Generate new token</strong>.</p>
                    </div>
                </li>

                <li>
                    <span className="help-step-index">4</span>
                    <div>
                        <strong>Configure the Token</strong>
                        <p>You'll see a few sections:</p>
                        <li><code>Resource Owner</code>Usually, this is your account.</li>
                        <li><code>Repository access</code>
                            <ul>
                                <li>All repositories → token can access every repo you can access.</li>
                                <li>Only select repositories → choose specific repos (safer).</li>
                            </ul>
                        </li>
                        <li><code>Permissions</code>
                            <ul>
                                <li>Contents → read/write access to code.</li>
                                <li>Metadata → read-only info about repos.</li>
                                <li>Pull requests → manage PRs.</li>
                                <li>Workflows → access GitHub Actions.</li>
                            </ul>
                        </li>
                        <li><code>Expiration</code>
                            <ul>
                                <li>GitHub forces you to set a token expiration (7, 30, 60, 90 days, or custom).</li>
                                <li>Shorter expiration = safer; you can always renew later.</li>
                            </ul>
                        </li>
                    </div>
                </li>

                <li>
                    <span className="help-step-index">5</span>
                    <div>
                        <strong>Generate the Token</strong>
                        <p>Click <strong>Generate token</strong> at the bottom</p>
                        <p><strong>Important</strong>: Copy the token <strong>IMMEDIATELY!</strong> GitHub will not show it again.</p>
                    </div>
                </li>

                <li>
                    <span className="help-step-index">6</span>
                    <div>
                        <strong>Add the Token to the Web API </strong>
                        <p>Go to your Web API location and locate your <code>config.xml</code> file.</p>
                        <p>Paste your token in the <code>gitHubApiToken</code> attribute field.</p>
                        <p>Start/Restart the Web API service.</p>
                    </div>
                </li>
            </ul>
        </div>
    </section>
);

export default HelpGitHubApiIntegrationsPage;
