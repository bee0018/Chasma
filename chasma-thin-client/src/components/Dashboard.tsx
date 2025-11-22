import React, { useState } from 'react';
import '../css/Dashboard.css';
import HomeTab from "./dashboardTabs/HomeTab";
import EncodeJwtTab from "./dashboardTabs/EncodeJwtTab";
import DecodeJwtTab from "./dashboardTabs/DecodeJwtTab";

/**
 * Initializes a new instance of the Dashboard class.
 * @constructor
 */
const Dashboard: React.FC = () => {
    /** Gets or sets the active tab that the user has selected. **/
    const [activeTab, setActiveTab] = useState<string>("home");

    /** Handles the event when the user selects a tab. **/
    const handleTabClick = (tab: string) => {
        setActiveTab(tab);
    };

    return (
        <div className="dashboard-container">
            <aside className="sidebar">
                <div
                    className={`tab ${activeTab === "home" ? "active" : ""}`}
                    onClick={() => handleTabClick("home")}
                >
                    🏠 Home
                </div>

                <div
                    className={`tab ${activeTab === "jwtEncoder" ? "active" : ""}`}
                    onClick={() => handleTabClick("jwtEncoder")}
                >
                    🔒 Encode JWT
                </div>

                <div
                    className={`tab ${activeTab === "jwtDecoder" ? "active" : ""}`}
                    onClick={() => handleTabClick("jwtDecoder")}
                >
                    🔓 Decode JWT
                </div>
            </aside>
            <main className="content">
                {activeTab === "home" && <HomeTab/>}
                {activeTab === "jwtEncoder" && <EncodeJwtTab/>}
                {activeTab === "jwtDecoder" && <DecodeJwtTab/>}
            </main>
        </div>
    );
};

export default Dashboard;