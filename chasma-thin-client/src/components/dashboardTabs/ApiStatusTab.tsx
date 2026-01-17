import React, {useEffect, useState} from "react";
import {HealthClient, HeartbeatStatus} from "../../API/ChasmaWebApiClient";
import "../../css/Dashboard.css"
import {apiBaseUrl} from "../../environmentConstants";

/** The health client interacting with the web API. **/
const healthClient = new HealthClient(apiBaseUrl);

/**
 * Initializes a new ApiStatusTab.
 * @constructor
 */
const ApiStatusTab: React.FC = () => {
    /** Gets or sets the heartbeat status. **/
    const [heartbeat , setHeartbeat] = useState<HeartbeatStatus | undefined>(undefined)

    /** Gets or sets the latest heartbeat time. **/
    const [latestHeartbeatTime, setLatestHeartbeatTime] = useState<Date | undefined>(undefined)

    /**
     * Gets the current heartbeat status.
     */
    function getHeartbeatStatus()
    {
        if (heartbeat === undefined) {
            return "unknown";
        }

        if (heartbeat === HeartbeatStatus.Ok) {
            return "pulsing"
        }

        return "offline"
    }

    /**
     * Gets the display text depending on the heartbeat status
     */
    function getHeartbeatDisplayText()
    {
        if (heartbeat === undefined)
        {
            return "Fetching Heartbeat...";
        }

        if (heartbeat === HeartbeatStatus.Ok)
        {
            return "Web API is currently online."
        }

        return "Web API is currently offline."
    }

    useEffect(() => {
        /**
         * Function determining if the API is running.
         */
        async function isApiRunning() {
            try {
                const response = await healthClient.getHeartbeat();
                console.log(response.message);
                setHeartbeat(response.status);
            } catch (error) {
                console.error( `Failed to receive heartbeat at ${Date.now()}: ${error}`)
                setHeartbeat(HeartbeatStatus.Error);
            } finally {
                setLatestHeartbeatTime(new Date());
            }
        }

        setInterval(async () => {
            await isApiRunning();
        }, 1000);
    }, []);

    return (
        <div>
            <h1 className="page-title">API Status Monitor &#x23FB;</h1>
            <br/>
            <div className="info-container">
                <div className="power-container">
                    <div className={`power-symbol ${getHeartbeatStatus()}`}>
                        &#x23FB;
                    </div>
                </div>
                <br/>
                {(heartbeat === undefined || heartbeat === HeartbeatStatus.Ok) && (<h3>Heartbeat Status ❤️</h3>)}
                {(heartbeat !== undefined && heartbeat === HeartbeatStatus.Error)  && (<h3>Heartbeat Status 💔</h3>)}
                <input
                    className="input-field"
                    type="text"
                    disabled={true}
                    value={getHeartbeatDisplayText()} />
                <input
                    className="input-field"
                    style={{ fontWeight: "bold" }}
                    type="text"
                    disabled={true}
                    value={latestHeartbeatTime === undefined ? '' : `Last updated at ${latestHeartbeatTime}`} />
            </div>
        </div>
    );
}
export default ApiStatusTab;