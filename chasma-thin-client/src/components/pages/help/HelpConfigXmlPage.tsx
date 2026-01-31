import React from "react";

/**
 * Initializes a new instance of the HelpConfigXmlPage component.
 * @constructor
 */
const HelpConfigXmlPage: React.FC = () => (
    <section id="config-xml" className="panel-card">
        <h2>Config File – XML Attributes</h2>

        <p className="help-intro">
            The application is configured using a single XML file (config.xml).
            Below is a complete example followed by a breakdown of each attribute.
        </p>

        <pre className="xml-example-block">
{`<?xml version="1.0" encoding="utf-8"?>
<configurations xmlns:xsi="http://www.w3.org/2001/XMLSchema-instance" xmlns:xsd="http://www.w3.org/2001/XMLSchema">
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
                <span className="xml-type">integer</span>
                <span className="xml-optional">optional</span>
            </div>
            <p>Defines the maximum number of workflow runs to report to the web application.</p>
            <p className="xml-meta">Default: <code>30</code></p>
        </div>
    </section>
);

export default HelpConfigXmlPage;
