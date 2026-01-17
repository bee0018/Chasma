import React, { useState } from 'react';
import '../css/Dashboard.css';
import HomeTab from "./dashboardTabs/HomeTab";
import ApiStatusTab from "./dashboardTabs/ApiStatusTab";
import IncludeRepositoryModal from "./modals/IncludeRepositoryModal";

/**
 * Initializes a new instance of the Dashboard class.
 * @constructor
 */
const Dashboard: React.FC = () => {
    /** Gets or sets the active tab that the user has selected. **/
    const [activeTab, setActiveTab] = useState<string>("home");

    /** Gets or sets a value indicating whether the user is including repositories. **/
    const [isIncludingRepos, setIsIncludingRepos] = useState<boolean>(false);

    /** Gets or sets the repository version. Serves as a trigger to update child components. **/
    const [reposVersion, setReposVersion] = useState(0);

    /** Handles the trigger when the repositories are updated. **/
    const handleReposUpdated = () => {
        setReposVersion(v => v + 1);
    };

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
                <div
                    className="tab"
                    onClick={() => setIsIncludingRepos(true)}
                    >
                    ∅ Ignored Repos
                </div>
            </aside>
            <main className="content">
                {activeTab === "home" && <HomeTab reposVersion={reposVersion} />}
                {activeTab === "apiStatus" && <ApiStatusTab/>}
            </main>
            {isIncludingRepos && (
                <IncludeRepositoryModal
                    onClose={() => setIsIncludingRepos(false)}
                    onRepositoriesUpdated={handleReposUpdated} />
            )}
        </div>
    );
};

export default Dashboard;