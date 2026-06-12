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
            <div className="help-placeholder">
                None currently.
            </div>
        </section>
    )
};

export default HelpFrequentlyAskedQuestionsPage;