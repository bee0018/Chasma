import React, { useState } from 'react';
import HomeTab from "./dashboardTabs/HomeTab";
import ApiStatusTab from "./dashboardTabs/ApiStatusTab";
import IncludeRepositoryModal from "./modals/IncludeRepositoryModal";
import BatchOperationsTab from "./dashboardTabs/BatchOperationsTab";
import {useCacheStore} from "../managers/CacheManager";
import MultiDryRunSimulationTab from "./dashboardTabs/MultiDryRunSimulationTab";
import GlobalRepositoryTab from './dashboardTabs/GlobalRepositoryTab';
import { useNavigate } from 'react-router-dom';
import LogoutModal from './modals/LogoutModal';
import UserConfigTab from './dashboardTabs/UserConfigTab';
import RepositoryAdditionsTab from './dashboardTabs/RepositoryAdditionsTab';

/**
 * Initializes a new instance of the Dashboard class.
 * @constructor
 */
const Dashboard: React.FC = () => {
    /** Gets or sets the active tab that the user has selected. **/
    const [activeTab, setActiveTab] = useState("home");

    /** Gets or sets a value indicating whether the user is including repositories. **/
    const [isIncludingRepos, setIsIncludingRepos] = useState(false);

    /** Gets or sets the repository version. Serves as a trigger to update child components. **/
    const [reposVersion, setReposVersion] = useState(0);

    /** Gets or sets a value indicating whether the user is logging out. **/
    const [isLoggingOut, setIsLoggingOut] = useState(false);

   /** Sets the notification modal. */
    const setNotification = useCacheStore(state => state.setNotification);

    /** The logged-in user. **/
    const user = useCacheStore((state) => state.user);

    /** The navigation function. **/
    const navigate = useNavigate();

    /** Handles the trigger when the repositories are updated. **/
    const handleReposUpdated = () => {
        setReposVersion(v => v + 1);
    };

    /** Handles the event when the user selects a tab. **/
    const handleTabClick = (tab: string) => {
        setActiveTab(tab);
    };

    /** Logs the user out of the system. */
    function logoutUser() {
        useCacheStore.getState().clearCache();
        window.location.href = "/";
         setNotification({
            title: "User logged out successfully.",
            message: "",
            isError: false,
        });
    }

    return (
        <div className="dashboard-container">
            <aside className="sidebar">
                <div
                    className={`sidebar-profile ${activeTab === "userConfig" ? "active" : ""}`}
                    onClick={() => handleTabClick("userConfig")}
                    >
                    <span className="profile-icon">👤</span>
                    <span className="username">{user?.userName}</span>
                </div>
                <div
                    className={`tab ${activeTab === "home" ? "active" : ""}`}
                    onClick={() => handleTabClick("home")}
                >
                    🏠 Home
                </div>
                <div
                    className={`tab ${activeTab === "batchOperations" ? "active" : ""}`}
                    onClick={() => handleTabClick("batchOperations")}
                >
                    ⚡ Batch Ops
                </div>
                <div
                    className={`tab ${activeTab === "dryRun" ? "active" : ""}`}
                    onClick={() => handleTabClick("dryRun")}
                >
                    🧪 Simulate
                </div>
                <div
                    className={`tab ${activeTab === "addRepos" ? "active" : ""}`}
                    onClick={() => handleTabClick("addRepos")}
                >
                    ➕ Add Repositories
                </div>
                <div
                    className="tab"
                    onClick={() => setIsIncludingRepos(true)}
                >
                    🚫 Ignored Repos
                </div>
                <div
                    className={`tab ${activeTab === "globalPrs" ? "active" : ""}`}
                    onClick={() => handleTabClick("globalPrs")}
                >
                    🌍 Global
                </div>
                <div
                    className={`tab ${activeTab === "apiStatus" ? "active" : ""}`}
                    onClick={() => handleTabClick("apiStatus")}
                >
                    🔌 API Status
                </div>
                <div
                    className="tab"
                    onClick={() => window.open("\help", "_blank")}>
                    <span className="profile-icon">💡</span>
                    <span className="username">Help</span>
                </div>
                <div
                    className="tab"
                    onClick={() => navigate('/setup')}>
                    ⚙️ Configure
                </div>
                <div
                    className="sidebar-help"
                    onClick={() => setIsLoggingOut(true)}>
                    <span className="username">⏻ Logout</span>
                </div>
            </aside>

            <main className="content">
                {activeTab === "home" && (
                    <div className="panel-card">
                        <HomeTab reposVersion={reposVersion} />
                    </div>
                )}
                {activeTab === "batchOperations" && (
                    <div className="panel-card">
                        <BatchOperationsTab />
                    </div>
                )}
                {activeTab === "dryRun" && (
                    <div className="panel-card">
                        <MultiDryRunSimulationTab />
                    </div>
                )}
                {activeTab === "globalPrs" && (
                    <div className="panel-card">
                        <GlobalRepositoryTab />
                    </div>
                )}
                {activeTab === "apiStatus" && (
                    <div className="panel-card">
                        <ApiStatusTab />
                    </div>
                )}
                {activeTab === "userConfig" && (
                    <div className="panel-card">
                        <UserConfigTab />
                    </div>
                )}
                {activeTab === "addRepos" && (
                    <div className="panel-card">
                        <RepositoryAdditionsTab />
                    </div>
                )}
            </main>

            {isIncludingRepos && (
                <IncludeRepositoryModal
                    onClose={() => setIsIncludingRepos(false)}
                    onRepositoriesUpdated={handleReposUpdated} />
            )}
            {isLoggingOut && (
                <LogoutModal
                    onClose={() => setIsLoggingOut(false)}
                    onSuccess={logoutUser} />
            )}
        </div>
    );
};

export default Dashboard;