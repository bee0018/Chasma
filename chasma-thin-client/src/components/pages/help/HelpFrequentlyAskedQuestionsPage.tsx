import React from "react";
import { useDocumentTitle } from "../../../util/useDocumentTitle";

/**
 * Initializes a new instance of the HelpFrequentlyAskedQuestionsPage component.
 * @constructor
 */
const HelpFrequentlyAskedQuestionsPage: React.FC = () => {
    useDocumentTitle("FAQs");
    return (
        <section id="faq" className="panel-card">
            <h2>Frequently Asked Questions</h2>
            <div className="help-subsection">
                <ul className="help-steps">
                    <li>
                        <span className="help-step-index">1</span>
                        <div>
                            <strong>Can I add the same repository to the system multiple times?</strong>
                            <ul>
                                <li style={{ flexFlow: "column" }}>
                                    <p><i>Yes</i>, however the repositories must live in different directories on your machine.</p>
                                    <p>- For example, if you tried to add the repository at location <code>C:/Documents/Project</code> more than once, the system would not allow that. It would need to be at another location such as <code>C:/Documents/Sandbox</code>.</p>
                                </li>
                            </ul>
                        </div>
                    </li>
                    <li>
                        <span className="help-step-index">2</span>
                        <div>
                            <strong>Is there anyway I can change the display name for my repository?</strong>
                            <ul>
                                <li>
                                    <p>Yes, on the home page you need to right-click a repository and select <code>Change Display Name</code>. Once you've decided on a name, select <code>Change</code> and the selected repository will update.</p>                                </li>
                            </ul>
                        </div>
                    </li>
                    <li>
                        <span className="help-step-index">3</span>
                        <div>
                            <strong>What data is stored on my machine, and why?</strong>
                            <ul>
                                <li>
                                    <p><strong>- Account Credentials:</strong> App usernames and passwords are stored locally to authenticate and secure your local workspace.</p>
                                </li>
                                <li>
                                    <p><strong>- Repository Metadata:</strong> Information about your registered repositories (such as names, branch details, and local file paths) is stored locally to populate your project dashboard and display repository statuses.</p>
                                </li>
                                <li>
                                    <p><strong>- API Tokens & SSH Keys (GitHub/GitLab):</strong> Your GitHub and GitLab usernames, encrypted Personal Access Tokens (PATs), and SSH key paths are used strictly to communicate directly with official GitHub and GitLab APIs and execute local Git operations on your behalf.</p>
                                </li>
                            </ul>
                        </div>
                    </li>
                    <li>
                        <span className="help-step-index">4</span>
                        <div>
                            <strong>How is your data stored and secured?</strong>
                            <ul>
                                <li>
                                    <p><strong>- 100% Local Storage:</strong> All application data, configurations, and credentials remain strictly on your local device. We do not transmit or store your data on external servers.</p>
                                </li>
                                <li>
                                    <p><strong>- Local Credential Encryption:</strong> Sensitive credentials (such as access tokens) are encrypted locally at rest on your machine and are only decrypted in memory when communicating directly over encrypted HTTPS connections to GitHub or GitLab.</p>
                                </li>
                                <li>
                                    <p><strong>- Zero Third-Party Sharing:</strong> We do not sell, track, or share your personal data, credentials, or repository information with external third parties.</p>
                                </li>
                                <li>
                                    <p><strong>- No Tracking Cookies:</strong> We do not use advertising or telemetry tracking cookies inside our platform.</p>
                                </li>
                            </ul>
                        </div>
                    </li>
                </ul>
            </div>
        </section>
    )
};

export default HelpFrequentlyAskedQuestionsPage;