import React from "react";

/**
 * Initializes a new instance of the HelpGitLabApiIntegrationPage component.
 * @constructor
 */
const HelpGitLabApiIntegrationPage: React.FC = () => (
    <section id="gitlab-api" className="panel-card">
        <h2>GitLab API Integration</h2>
        <div className="help-subsection">
            <h3 className="help-subsection-title">Personal Access Token</h3>
            <ul className="help-steps">

                <li>
                    <span className="help-step-index">1</span>
                    <div>
                        <strong>Log in to GitLab</strong>
                        <p>
                            Go to{' '}
                            <a
                                href="https://gitlab.com"
                                target="_blank"
                                rel="noopener noreferrer"
                                style={{color: '#00bfff', textDecoration: 'none'}}
                            >
                                GitLab
                            </a>{' '}
                            and log in to your account.
                        </p>
                    </div>
                </li>

                <li>
                    <span className="help-step-index">2</span>
                    <div>
                        <strong>Go to Edit Profile option</strong>
                        <p>Click your profile avatar → select Edit profile.</p>
                    </div>
                </li>

                <li>
                    <span className="help-step-index">3</span>
                    <div>
                        <strong>Go to Access Tokens</strong>
                        <p>Go to Access → <strong>Personal access tokens</strong></p>
                        <p>Direct URL (replace with your GitLab host if self hosted): <code>https://gitlab.com/-/user_settings/personal_access_tokens</code>.</p>
                    </div>
                </li>

                <li>
                    <span className="help-step-index">4</span>
                    <div>
                        <strong>Generate a New Token</strong>
                        <p><strong>Scopes / Permissions</strong>: Choose what the token can do:</p>
                        <ul>
                            <li><code>Fine-grained token</code>(Recommended) Limit scope to specific groups and projects and fine-grained permissions to resources.</li>
                            <li><code>Legacy token</code>Scoped to all groups and projects with broad permissions to resources.</li>
                            <li>Only select what you actually need for security.</li>
                        </ul>
                    </div>
                </li>

                <li>
                    <span className="help-step-index">5</span>
                    <div>
                        <strong>Click Generate token.</strong>
                    </div>
                </li>

                <li>
                    <span className="help-step-index">6</span>
                    <div>
                        <strong>Copy Token</strong>
                        <p><strong>Important</strong>: Copy the token <strong>IMMEDIATELY!</strong> GitLab will not show it again.</p>
                    </div>
                </li>
            </ul>
        </div>
        <div className="help-subsection">
            <h3 className="help-subsection-title">Alternation Token Types (Instead of a personal access token)</h3>
            <ul className="help-steps">
                <li>
                    <span className="help-step-index">1</span>
                    <div>
                        <strong>Project Access Token</strong>
                        <p>Project → Settings → Access Tokens</p>
                        <p>Good for automation specific to one repo.</p>
                    </div>
                </li>

                <li>
                    <span className="help-step-index">2</span>
                    <div>
                        <strong>Group Access Token</strong>
                        <p>Group → Settings → Access Tokens</p>
                        <p>Good for multiple projects.</p>
                    </div>
                </li>
            </ul>
        </div>
    </section>
);

export default HelpGitLabApiIntegrationPage;
