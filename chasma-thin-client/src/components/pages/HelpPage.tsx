import React, { useState } from "react";

const sections = [
    { id: "config-xml", title: "Config File (XML)" },
    { id: "api-start", title: "Start / Restart Web API" },
    { id: "faq", title: "FAQ" },
    { id: "context-menu", title: "Repository Context Menu" },
    { id: "github-api", title: "GitHub API Integration" },
    { id: "git-commands", title: "Common Git Commands" },
];

/**
 * Initializes a new instance of the HelpPage class.
 * @constructor
 */
const HelpPage: React.FC = () => {
    /** Gets or sets the active section. **/
    const [activeSection, setActiveSection] = useState(sections[0].id);
    return (
        <div className="help-page">
            <aside className="help-nav">
                <h3 className="help-nav-title">Help & Docs</h3>
                {sections.map(section => (
                    <div
                        key={section.id}
                        className={`help-nav-item ${activeSection === section.id ? "active" : ""}`}
                        onClick={() => {
                            setActiveSection(section.id);
                            document
                                .getElementById(section.id)
                                ?.scrollIntoView({ behavior: "smooth" });
                        }}
                    >
                        {section.title}
                    </div>
                ))}
            </aside>

            <main className="help-content">
                <section id="config-xml" className="panel-card">
                    <h2>Config File – XML Attributes</h2>

                    <p className="help-intro">
                        The application is configured using a single XML file (config.xml).
                        Below is a complete example followed by a breakdown of each attribute.
                    </p>

                    <pre className="xml-example-block">
{`<?xml version="1.0" encoding="utf-8"?>
<configurations xmlns:xsi="http://www.w3.org/2001/XMLSchema-instance"
\txmlns:xsd="http://www.w3.org/2001/XMLSchema">
\t<webApiUrl>https://localhost:44349/</webApiUrl>
\t<showDebugControllers>false</showDebugControllers>
\t<thinClientUrl>http://localhost:3000</thinClientUrl>
\t<githubApiToken>token</githubApiToken>
\t<workflowRunReportThreshold>30</workflowRunReportThreshold>
</configurations>`}
                    </pre>
                    <h3 className="help-subtitle">Attributes</h3>
                    <div className="xml-attr">
                        <div className="xml-attr-header">
                            <span className="xml-name">webApiUrl</span>
                            <span className="xml-type">string</span>
                            <span className="xml-required">required</span>
                        </div>
                        <p>Defines the URL where the backend Web API will be running and listening for requests.</p>
                        <p className="xml-meta">Default: <code>https://localhost:5000/</code></p>
                    </div>

                    <div className="xml-attr">
                        <div className="xml-attr-header">
                            <span className="xml-name">showDebugControllers</span>
                            <span className="xml-type">boolean</span>
                            <span className="xml-required">required</span>
                        </div>
                        <p>A value indicating whether to show the debug controllers on the Web API Swagger Page.</p>
                        <p className="xml-meta">Default: <code>true</code></p>
                    </div>

                    <div className="xml-attr">
                        <div className="xml-attr-header">
                            <span className="xml-name">thinClientUrl</span>
                            <span className="xml-type">string</span>
                            <span className="xml-required">required</span>
                        </div>
                        <p>Defines the URL where this web application will be running and sending requests to the Web API.</p>
                        <p className="xml-meta">Default: <code>http://localhost:3000</code></p>
                    </div>

                    <div className="xml-attr">
                        <div className="xml-attr-header">
                            <span className="xml-name">gitHubApiToken</span>
                            <span className="xml-type">string</span>
                            <span className="xml-optional">optional</span>
                        </div>
                        <p>Defines the GitHub API token that is used for access and performing operations with the Octokit GitHub development package.</p>
                        <p className="xml-meta">Default: none</p>
                    </div>

                    <div className="xml-attr">
                        <div className="xml-attr-header">
                            <span className="xml-name">workflowRunReportThreshold</span>
                            <span className="xml-type">number</span>
                            <span className="xml-optional">optional</span>
                        </div>
                        <p>Defines the maximum number of workflow runs to report to the web application.</p>
                        <p className="xml-meta">Default: <code>30</code></p>
                    </div>
                </section>


                <section id="api-start" className="panel-card">
                    <h2>Starting & Restarting the Web API</h2>
                    <p>
                        How to start, stop, and restart the backend API in different environments.
                    </p>
                    <div className="help-placeholder">
                        Commands, service instructions, Docker notes, etc.
                    </div>
                </section>

                <section id="faq" className="panel-card">
                    <h2>Frequently Asked Questions</h2>
                    <div className="help-placeholder">
                        Q: Why isn’t my repo showing up?<br />
                        Q: How often does status refresh?<br />
                        Q: Where are logs stored?
                    </div>
                </section>

                <section id="context-menu" className="panel-card">
                    <h2>Repository Context Menu</h2>
                    <ul className="help-list">
                        <li><strong>Open Status Page</strong> – View repo health and API checks</li>
                        <li><strong>Delete</strong> – Permanently remove repo from the system</li>
                        <li><strong>Ignore</strong> – Exclude repo from monitoring</li>
                    </ul>
                </section>

                <section id="github-api" className="panel-card">
                    <h2>GitHub API Integration</h2>
                    <p>
                        Steps to generate a token and connect your GitHub account.
                    </p>
                    <div className="help-placeholder">
                        Token scopes, rate limits, environment variables, etc.
                    </div>
                </section>

                <section id="git-commands" className="panel-card">
                    <h2>Common Git Commands</h2>

                    <p className="help-intro">
                        Frequently used Git commands and what they do.
                    </p>

                    <div className="git-command-list">
                        <div className="git-command">
                            <code>git status</code>
                            <span>Shows the current state of the working directory and staging area.</span>
                        </div>

                        <div className="git-command">
                            <code>git pull</code>
                            <span>Fetches and merges changes from the remote repository.</span>
                        </div>

                        <div className="git-command">
                            <code>git fetch</code>
                            <span>Downloads changes from the remote without modifying local files.</span>
                        </div>

                        <div className="git-command">
                            <code>git checkout -b &lt;branch&gt;</code>
                            <span>Creates and switches to a new branch.</span>
                        </div>

                        <div className="git-command">
                            <code>git rebase main</code>
                            <span>Reapplies commits on top of the latest main branch.</span>
                        </div>

                        <div className="git-command">
                            <code>git log --oneline</code>
                            <span>Displays a compact history of commits.</span>
                        </div>
                    </div>
                </section>

            </main>
        </div>
    );
};

export default HelpPage;
