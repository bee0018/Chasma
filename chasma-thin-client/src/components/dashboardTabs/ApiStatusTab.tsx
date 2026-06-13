import React, { useEffect, useState } from "react";
import { HeartbeatStatus } from "../../API/ChasmaWebApiClient";
import { healthClient } from "../../managers/ApiClientManager";
import { useDocumentTitle } from "../../util/useDocumentTitle";
import { useCacheStore } from "../../managers/CacheManager";

/**
 * Initializes a new ApiStatusTab.
 * @constructor
 */
const ApiStatusTab: React.FC = () => {
    useDocumentTitle("Server Status");

    /** Gets or sets the heartbeat status. **/
    const [heartbeat, setHeartbeat] = useState<HeartbeatStatus | undefined>(undefined);

    /** Gets or sets the latest heartbeat time. **/
    const [latestHeartbeatTime, setLatestHeartbeatTime] = useState<Date | undefined>(undefined);

    /** Gets or sets a value indicating whether the application is restarting the application. */
    const [isRestarting, setIsRestarting] = useState<boolean>(false);

    /** Gets or sets a value indicating whether the application is stopping the application. */
    const [isStopping, setIsStopping] = useState<boolean>(false);

    /** Gets or sets the action message. */
    const [actionMessage, setActionMessage] = useState<string | null | undefined>(null);

    /**
     * Gets the heartbeat status class message for styling.
     */
    function getHeartbeatStatusClass() {
        if (heartbeat === undefined) return "status-unknown";
        if (heartbeat === HeartbeatStatus.Ok) return "status-online";
        return "status-offline";
    }

    /** Gets the heartbeat display text. **/
    function getHeartbeatDisplayText() {
        if (isRestarting) return "Emryce services are recycling. Reconnecting momentarily...";
        if (isStopping) return "Services terminated. Safe to close dashboard view.";
        if (heartbeat === undefined) return "Checking server heartbeat...";
        if (heartbeat === HeartbeatStatus.Ok) return "Emryce server is online and responding.";
        return "Emryce server is currently offline.";
    }

    useEffect(() => {
        if (isStopping || isRestarting) return;

        async function isApiRunning() {
            try {
                const response = await healthClient.getHeartbeat();
                setHeartbeat(response.status);
            } catch (error) {
                setHeartbeat(HeartbeatStatus.Error);
            } finally {
                setLatestHeartbeatTime(new Date());
            }
        }

        isApiRunning();
        const interval = setInterval(isApiRunning, 1000);
        return () => clearInterval(interval);
    }, [isStopping, isRestarting]);

    /**
     * Handles the event when the user wants to restart the application.
     */
    const handleRestartApplication = async () => {
        if (isRestarting || isStopping) return;

        if (window.confirm("Are you sure you want to restart Emryce?")) {
            setIsRestarting(true);
            setActionMessage("Sending restart signal...");
            setHeartbeat(undefined);
            try {
                const response = await healthClient.restartApplication();
                setActionMessage(response.message);
                setTimeout(() => {
                    setIsRestarting(false);
                    setActionMessage(undefined);
                    setHeartbeat(response.status);
                    useCacheStore.getState().clearCache();
                }, 4000);
                setTimeout(() => {
                    window.location.href = "about:blank";
                }, 100);
            } catch (error) {
                setIsRestarting(false);
                setActionMessage("Restart failed.");
            }
        }
    };

    /**
     * Handles the event when the user wants to stop the application.
     */
    const handleStopApplication = async () => {
        if (isRestarting || isStopping) return;

        const stopPrompt = "Are you sure you want to shut down Emryce? You will need to restart the application by opening the Emryce desktop shortcut or the installed executable.";
        if (window.confirm(stopPrompt)) {
            setIsStopping(true);
            setActionMessage("Shutting down...");
            setHeartbeat(HeartbeatStatus.Error);

            try {
                await healthClient.stopApplication();
                setTimeout(() => {
                    useCacheStore.getState().clearCache();
                    window.location.href = "about:blank";
                }, 1500);
            } catch (error) {
                setActionMessage("Shutdown failed.");
                setIsStopping(false);
            }
        }
    };

    return (
        <div className="status-dashboard-grid">

            {/* Top: Server Status Module */}
            <div className="dashboard-card api-status-card">
                <div className="card-header">
                    <h2 className="card-title">Server Status</h2>
                    <span className={`status-pill ${getHeartbeatStatusClass()}`}>
                        {heartbeat === HeartbeatStatus.Ok ? "Online" : heartbeat === undefined ? "Checking" : "Offline"}
                    </span>
                </div>

                <div className="card-body">
                    <div className="status-row">
                        <div className={`status-indicator ${getHeartbeatStatusClass()}`} />
                        <span className="status-text">{getHeartbeatDisplayText()}</span>
                    </div>

                    <div className="status-inputs">
                        <input
                            className="api-status-input-field"
                            type="text"
                            disabled
                            value={getHeartbeatDisplayText()}
                        />
                        <input
                            className="api-status-input-field subtle"
                            type="text"
                            disabled
                            value={
                                latestHeartbeatTime
                                    ? `Last updated • ${latestHeartbeatTime.toLocaleTimeString()}`
                                    : ""
                            }
                        />
                    </div>
                </div>
            </div>

            {/* Bottom: Server Power Options */}
            {heartbeat !== HeartbeatStatus.Error &&
                <>
                    <hr className="card-spacer-line" />
                    <div className="dashboard-card power-config-card">
                        <div className="card-header">
                            <h2 className="card-title">Power Options</h2>
                        </div>

                        <div className="card-body">
                            <p className="power-description">
                                Control the background runtime execution engine state.
                            </p>

                            <div className="action-button-vertical-group">
                                <button
                                    type="button"
                                    onClick={handleRestartApplication}
                                    disabled={isRestarting || isStopping}
                                    className="btn-power btn-power-restart"
                                >
                                    🔄 Restart Emryce Server
                                </button>

                                <button
                                    type="button"
                                    onClick={handleStopApplication}
                                    disabled={isRestarting || isStopping}
                                    className="btn-power btn-power-stop"
                                >
                                    🛑 Stop Emryce Server
                                </button>
                            </div>

                            {actionMessage && (
                                <div className="power-inline-feedback">
                                    <span className="feedback-dot animate-pulse" />
                                    {actionMessage}
                                </div>
                            )}
                        </div>
                    </div>
                </>
            }
        </div>
    );
};

export default ApiStatusTab;