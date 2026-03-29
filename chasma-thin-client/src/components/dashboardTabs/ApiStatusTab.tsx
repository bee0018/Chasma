import React, { useEffect, useState } from "react";
import { HeartbeatStatus } from "../../API/ChasmaWebApiClient";
import {healthClient} from "../../managers/ApiClientManager";
import { useNavigate } from "react-router-dom";
import { useCacheStore } from "../../managers/CacheManager";
import { handleApiError } from "../../managers/TransactionHandlerManager";

/**
 * Initializes a new ApiStatusTab.
 * @constructor
 */
const ApiStatusTab: React.FC = () => {
    /** Gets or sets the heartbeat status. **/
    const [heartbeat, setHeartbeat] = useState<HeartbeatStatus | undefined>(undefined);

    /** Gets or sets the latest heartbeat time. **/
    const [latestHeartbeatTime, setLatestHeartbeatTime] = useState<Date | undefined>(undefined);

    /** The navigation function. **/
    const navigate = useNavigate();

   /** Sets the notification modal. */
   const setNotification = useCacheStore(state => state.setNotification);

    /**
     * Gets the heartbeat status class.
     */
    function getHeartbeatStatusClass() {
        if (heartbeat === undefined) return "status-unknown";
        if (heartbeat === HeartbeatStatus.Ok) return "status-online";
        return "status-offline";
    }

    /** Gets the heartbeat display text. **/
    function getHeartbeatDisplayText() {
        if (heartbeat === undefined) return "Checking API heartbeat...";
        if (heartbeat === HeartbeatStatus.Ok) return "Web API is online and responding.";
        return "Web API is currently offline.";
    }

    useEffect(() => {
        async function isApiRunning() {
            try {
                const response = await healthClient.getHeartbeat();
                setHeartbeat(response.status);
            } catch (error) {
                const errorNotification = handleApiError(error, navigate, "Failed to receive heartbeat!", "Review server logs for more information.");
                setNotification(errorNotification);
                setHeartbeat(HeartbeatStatus.Error);
            } finally {
                setLatestHeartbeatTime(new Date());
            }
        }

        isApiRunning();
        const interval = setInterval(isApiRunning, 1000);
        return () => clearInterval(interval);
    }, []);

    return (
        <div className="dashboard-card api-status-card">
            <div className="card-header">
                <h2 className="card-title">API Status</h2>
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
    );
};

export default ApiStatusTab;
