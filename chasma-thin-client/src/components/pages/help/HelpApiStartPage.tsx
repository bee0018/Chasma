import React from "react";

/**
 * Initializes a new HelpApiStartPage component.
 * @constructor
 */
const HelpApiStartPage: React.FC = () => (
    <section id="api-start" className="panel-card">
        <h2>Starting & Restarting the Web API</h2>
        <p>
            How to start, stop, and restart the backend API for different operating systems.
        </p>
        <div className="help-subsection">
            <h3 className="help-subsection-title">Windows Environment</h3>
            <ul className="help-steps">

                <li>
                    <span className="help-step-index">1</span>
                    <div>
                        <strong>Configure the XML file</strong>
                        <p>
                            Update <code>config.xml</code> with the correct Web API URL, ports, and optional GitHub token.
                        </p>
                    </div>
                </li>

                <li>
                    <span className="help-step-index">2</span>
                    <div>
                        <strong>Open the Windows Start menu</strong>
                        <p>
                            Open the Start menu by pressing the <code>Windows</code> key or clicking the Start icon.
                        </p>
                    </div>
                </li>

                <li>
                    <span className="help-step-index">3</span>
                    <div>
                        <strong>Navigate to Windows Services</strong>
                        <p>
                            In the Windows start menu, click <code>Services</code>. You will be prompted
                            for administrative access.
                        </p>
                    </div>
                </li>

                <li>
                    <span className="help-step-index">4</span>
                    <div>
                        <strong>Locate and Start the Web API</strong>
                        <p>
                            Use the available options in the Services interface to start, stop, or restart the Web API.
                        </p>
                    </div>
                </li>

                <li>
                    <span className="help-step-index">5</span>
                    <div>
                        <strong>Verify the service</strong>
                        <p>
                            Open the Swagger page at the <code>webApiUrl</code> configured in <code>config.xml</code> and confirm the API is running and responding to requests.
                            The Swagger page is available at the route <code>your_Web_Api_Url/swagger</code>.
                        </p>
                    </div>
                </li>
            </ul>
        </div>
        <div className="help-subsection">
            <h3 className="help-subsection-title">Unix Environment</h3>
            <div className="help-placeholder">
                Steps are not yet implemented.
            </div>
        </div>

        <div className="help-subsection">
            <h3 className="help-subsection-title">macOS Environment</h3>
            <div className="help-placeholder">
                Steps are not yet implemented.
            </div>
        </div>
    </section>
);

export default HelpApiStartPage;