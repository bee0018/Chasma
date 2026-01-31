import React from "react";

/**
 * Initializes a new instance of the HelpRepoContextMenuPage component.
 * @constructor
 */
const HelpRepoContextMenuPage: React.FC = () => (
    <section id="context-menu" className="panel-card">
        <h2>Repository Context Menu</h2>
        <ul className="help-list">
            <li><strong>Open Status Page</strong> – View repo health and API checks</li>
            <li><strong>Delete</strong> – Permanently remove repo from the system</li>
            <li><strong>Ignore</strong> – Exclude repo from monitoring</li>
        </ul>
    </section>
);

export default HelpRepoContextMenuPage;