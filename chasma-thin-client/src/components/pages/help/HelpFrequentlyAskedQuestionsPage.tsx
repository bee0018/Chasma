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
                            <p><i>Yes</i>, however the repositories must live in different directories on your machine.</p>
                            <p>- For example, if you tried to add the repository at location <code>C:/Documents/Project</code> more than once, the system would not allow that. It would need to be at another location such as <code>C:/Documents/Sandbox</code>.</p>
                        </div>
                    </li>
                    <li>
                        <span className="help-step-index">2</span>
                        <div>
                            <strong>Is there anyway I can change the display name for my repository?</strong>
                            <p>Yes, on the home page you need to right-click a repository and select <code>Change Display Name</code>. Once you've decided on a name, select <code>Change</code> and the selected repository will update.</p>
                        </div>
                    </li>
                    <li>
                        <span className="help-step-index">3</span>
                        <div>
                            <strong>What data do we collect, and why?</strong>
                            <ul>
                                <li>
                                    <p><strong>- Account Credentials (App Username & Password):</strong> Used solely to create, authenticate, and secure your personal account on the Emryce platform software.</p>
                                </li>
                                <li>
                                    <p><strong>- Repository Metadata:</strong> We store information about your registered repositories (such as repository names, branch details, and local directory paths) to provide and update your project dashboard, as well as updating repository statuses.</p>
                                </li>
                                <li>
                                    <p><strong>- API Tokens & SSH Keys (GitHub/GitLab):</strong> Your GitHub and GitLab usernames, Personal Access Tokens (PATs), and SSH private key paths are used strictly to communicate directly with the official GitHub and GitLab APIs and execute Git operations on your behalf.</p>
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
                                    <p><strong>- Zero Third-Party Sharing:</strong> We do not sell, track, or share your personal data, credentials, or repository information with any external third parties.</p>
                                </li>
                                <li>
                                    <p><strong>- No tracking Cookies:</strong> We do not use advertising or tracking cookies in our software platform.</p>
                                </li>
                                <li>
                                    <p><strong>- Credential Encryption:</strong> Your sensitive credentials (such as access tokens and private keys) and app passwords are never stored in plain text. They are encrypted both at rest in our database and in transit.</p>
                                </li>
                                <li>
                                    <p><strong>- Local Integrity:</strong> Your private keys and local clone paths remain under your control and are only accessed locally to perform requested Git operations.</p>
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