import React, { useState } from 'react';
import '../css/Dashboard.css';
import HomeTab from "./dashboardTabs/HomeTab";
import EncodeJwtTab from "./dashboardTabs/EncodeJwtTab";
import DecodeJwtTab from "./dashboardTabs/DecodeJwtTab";
import UuidGeneratorTab from "./dashboardTabs/UuidGeneratorTab";
import ApiStatusTab from "./dashboardTabs/ApiStatusTab";
import WorkflowRunsTab from "./dashboardTabs/WorkflowRunsTab";

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

                <div
                    className={`tab ${activeTab === "uuidGenerator" ? "active" : ""}`}
                    onClick={() => handleTabClick("uuidGenerator")}
                >
                    🔄 Generate UUID
                </div>

                <div
                    className={`tab ${activeTab === "github" ? "active" : ""}`}
                    onClick={() => handleTabClick("github")}
                >
                    📊 GitHub Builds
                </div>

                <div
                    className={`tab ${activeTab === "apiStatus" ? "active" : ""}`}
                    onClick={() => handleTabClick("apiStatus")}
                >
                    &#x23FB; API Status
                </div>
            </aside>
            <main className="content">
                {activeTab === "home" && <HomeTab/>}
                {activeTab === "jwtEncoder" && <EncodeJwtTab/>}
                {activeTab === "jwtDecoder" && <DecodeJwtTab/>}
                {activeTab === "uuidGenerator" && <UuidGeneratorTab/>}
                {activeTab === "github" && <WorkflowRunsTab/>}
                {activeTab === "apiStatus" && <ApiStatusTab/>}
            </main>
        </div>
    );
};

export default Dashboard;