import React, { useState } from 'react';
import '../css/Dashboard.css';
import HomeTab from "./dashboardTabs/HomeTab";
import ApiStatusTab from "./dashboardTabs/ApiStatusTab";

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
                    className={`tab ${activeTab === "apiStatus" ? "active" : ""}`}
                    onClick={() => handleTabClick("apiStatus")}
                >
                    &#x23FB; API Status
                </div>
            </aside>
            <main className="content">
                {activeTab === "home" && <HomeTab/>}
                {activeTab === "apiStatus" && <ApiStatusTab/>}
            </main>
        </div>
    );
};

export default Dashboard;