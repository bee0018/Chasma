import React from "react";
import { useDocumentTitle } from "../../../util/useDocumentTitle";

/**
 * Initializes a new instance of the HelpConfigXmlPage component.
 * @constructor
 */
const HelpConfigXmlPage: React.FC = () => {
    useDocumentTitle("Configuration Help");
    return (
        <section id="config-xml" className="panel-card">
            <h2>Config File – XML Attributes</h2>

            <p className="help-intro">
                The application is configured using a single XML file (config.xml).
                Below is a complete example followed by a breakdown of each attribute.
            </p>

            <pre className="xml-example-block">
                {`<?xml version="1.0" encoding="utf-8"?>
<configurations xmlns:xsi="http://www.w3.org/2001/XMLSchema-instance" xmlns:xsd="http://www.w3.org/2001/XMLSchema">
\t<bindingPort>5000</bindingPort>
\t<secureBindingPort>7200</secureBindingPort>
\t<jwtSecretKey>secretKey</jwtSecretKey>
\t<globalWorkspacePath>path/to/workspace</globalWorkspacePath>
\t<gitHubUsername>gitHubUser</gitHubUsername>
\t<gitHubSshPrivateKeyPath>path/to/private/key</gitHubSshPrivateKeyPath>
\t<gitHubSshPassphrase>phrase</gitHubSshPassphrase>
\t<githubApiToken>token</githubApiToken>
\t<workflowRunReportThreshold>30</workflowRunReportThreshold>
\t<gitHubPullRequestScanIntervalSeconds>20</gitHubPullRequestScanIntervalSeconds>
\t<gitLabUsername>gitLabUsername</gitLabUsername>
\t<gitlabApiToken>token</gitlabApiToken>
\t<gitLabSshPrivateKeyPath>path/to/private/key</gitLabSshPrivateKeyPath>
\t<gitLabSshPassphrase>phrase</gitLabSshPassphrase>
\t<selfHostedGitLabUrl>http://localhost:3000</selfHostedGitLabUrl>
\t<gitLabMergeRequestScanIntervalSeconds>45</gitLabMergeRequestScanIntervalSeconds>
</configurations>`}
            </pre>
            <h3 className="help-subtitle">Attributes</h3>
            <div className="xml-attr">
                <div className="xml-attr-header">
                    <span className="xml-name">bindingPort</span>
                    <span className="xml-type">integer</span>
                    <span className="xml-required">required</span>
                </div>
                <p>Defines the fallback port where the backend API will listen to requests on. This will represent a HTTP connection.</p>
                <p className="xml-meta">Default: 5000</p>
            </div>

            <div className="xml-attr">
                <div className="xml-attr-header">
                    <span className="xml-name">secureBindingPort</span>
                    <span className="xml-type">integer</span>
                    <span className="xml-required">required</span>
                </div>
                <p>Defines the port where the backend API will listen to requests on. This will represent an HTTPS connection</p>
                <p className="xml-meta">Default: 7200</p>
            </div>

            <div className="xml-attr">
                <div className="xml-attr-header">
                    <span className="xml-name">jwtSecretKey</span>
                    <span className="xml-type">string</span>
                    <span className="xml-required">required</span>
                </div>
                <p>Cryptographic string or key pair used to sign and verify JSON Web Tokens, ensuring the token's authenticity and integrity.</p>
                <p className="xml-meta">Default: none</p>
            </div>

            <div className="xml-attr">
                <div className="xml-attr-header">
                    <span className="xml-name">globalWorkspacePath</span>
                    <span className="xml-type">string</span>
                    <span className="xml-required">required</span>
                </div>
                <p>The user-defined workspace variable where all repositories will be stored.</p>
                <p className="xml-meta">Default: none</p>
            </div>

            <div className="xml-attr">
                <div className="xml-attr-header">
                    <span className="xml-name">gitHubUsername</span>
                    <span className="xml-type">string</span>
                    <span className="xml-optional">optional</span>
                </div>
                <p>Your GitHub user name.</p>
                <p className="xml-meta">Default: none</p>
            </div>

            <div className="xml-attr">
                <div className="xml-attr-header">
                    <span className="xml-name">gitHubSshPrivateKeyPath</span>
                    <span className="xml-type">string</span>
                    <span className="xml-optional">optional</span>
                </div>
                <p>The path to the SSH private key for your GitHub account.</p>
                <p className="xml-meta">Default: none</p>
            </div>

            <div className="xml-attr">
                <div className="xml-attr-header">
                    <span className="xml-name">gitHubSshPassphrase</span>
                    <span className="xml-type">string</span>
                    <span className="xml-optional">optional</span>
                </div>
                <p>The passphrase to the SSH private key for your GitHub account.</p>
                <p className="xml-meta">Default: none</p>
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

            <div className="xml-attr">
                <div className="xml-attr-header">
                    <span className="xml-name">gitHubPullRequestScanIntervalSeconds</span>
                    <span className="xml-type">integer</span>
                    <span className="xml-optional">optional</span>
                </div>
                <p>Defines the interval in seconds at which GitHub pull requests are scanned for updates.</p>
                <p className="xml-meta">Default: <code>45</code></p>
            </div>

            <div className="xml-attr">
                <div className="xml-attr-header">
                    <span className="xml-name">gitLabUsername</span>
                    <span className="xml-type">string</span>
                    <span className="xml-optional">optional</span>
                </div>
                <p>Your GitLab user name.</p>
                <p className="xml-meta">Default: none</p>
            </div>

            <div className="xml-attr">
                <div className="xml-attr-header">
                    <span className="xml-name">gitlabApiToken</span>
                    <span className="xml-type">string</span>
                    <span className="xml-optional">optional</span>
                </div>
                <p>Defines the GitLab API token that is used for access and performing operations with the NGitLab development package.</p>
                <p className="xml-meta">Default: none</p>
            </div>

            <div className="xml-attr">
                <div className="xml-attr-header">
                    <span className="xml-name">gitLabSshPrivateKeyPath</span>
                    <span className="xml-type">string</span>
                    <span className="xml-optional">optional</span>
                </div>
                <p>The path to the SSH private key for your GitLab account.</p>
                <p className="xml-meta">Default: none</p>
            </div>

            <div className="xml-attr">
                <div className="xml-attr-header">
                    <span className="xml-name">gitLabSshPassphrase</span>
                    <span className="xml-type">string</span>
                    <span className="xml-optional">optional</span>
                </div>
                <p>The passphrase to the SSH private key for your GitLab account.</p>
                <p className="xml-meta">Default: none</p>
            </div>

            <div className="xml-attr">
                <div className="xml-attr-header">
                    <span className="xml-name">selfHostedGitLabUrl</span>
                    <span className="xml-type">string</span>
                    <span className="xml-optional">optional</span>
                </div>
                <p>GitLab absolute URL (with or without the /api/v* path).</p>
                <p className="xml-meta">Default: none</p>
            </div>

            <div className="xml-attr">
                <div className="xml-attr-header">
                    <span className="xml-name">gitLabMergeRequestScanIntervalSeconds</span>
                    <span className="xml-type">integer</span>
                    <span className="xml-optional">optional</span>
                </div>
                <p>Defines the interval in seconds at which GitLab merge requests are scanned for updates.</p>
                <p className="xml-meta">Default: <code>45</code></p>
            </div>
        </section>
    );
}

export default HelpConfigXmlPage;
