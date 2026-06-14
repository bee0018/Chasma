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
                </ul>
            </div>
        </section>
    )
};

export default HelpFrequentlyAskedQuestionsPage;