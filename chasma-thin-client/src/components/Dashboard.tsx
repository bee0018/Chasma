import React, { useState } from 'react';
import { useCacheStore } from "../managers/CacheManager";
import { useNavigate, useLocation, Outlet } from 'react-router-dom';
import IncludeRepositoryModal from "./modals/IncludeRepositoryModal";
import LogoutModal from './modals/LogoutModal';
import { shellClient } from '../managers/ApiClientManager';

/**
 * Initializes a new instance of the Dashboard class.
 * @constructor
 */
const Dashboard: React.FC = () => {
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

    /** The live route info to dynamically track highlighted states. **/
    const location = useLocation();

    /** Gets or sets a value indicating whether the repository actions tab is open. */
    const [isRepoActionsOpen, setIsRepoActionsOpen] = useState<boolean>(false);

    /** Gets or sets a value indicating whether the executions actions tab is open. */
    const [isExecutionsOptionsOpen, setIsExecutionsOptionsOpen] = useState<boolean>(false);

    /** Gets or sets a value indicating whether the system and diagnostics actions tab is open. */
    const [isSystemDiagnosticsOptionsOpen, setIsSystemDiagnosticsOptionsOpen] = useState<boolean>(false);

    /** Gets or sets a value indicating whether the user and help actions tab is open. */
    const [isUserAndHelpOptionsOpen, setIsUserAndHelpOptionsOpen] = useState<boolean>(false);

    /** Handles the trigger when the repositories are updated. **/
    const handleReposUpdated = () => {
        setReposVersion(v => v + 1);
    };

    /** Helper function to see which tab path matches our path sub-route context */
    const isActive = (path: string) => {
        if (path === "/dashboard" && location.pathname === "/dashboard") return "active";
        return location.pathname.endsWith(path) ? "active" : "";
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

    /**
     * Handles the event when the user wants to open server logs.
    */
    const handleOpenServerLogsRequest = async () => {
        try {
            const response = await shellClient.openApiLogs();
            if (response.isErrorResponse) {
                setNotification({
                    title: "Failed to open server logs.",
                    message: response.errorMessage,
                    isError: true,
                });
                return;
            }
        } catch (error) {
            setNotification({
                title: "Error opening server logs!",
                message: "Verify console logs for more information.",
                isError: true,
            });
        }
    };

    return (
        <div className="dashboard-container">
            <aside className="sidebar">
                <div
                    className="sidebar-profile">
                    <span className="profile-icon">👤</span>
                    <span className="username">{user?.userName}</span>
                </div>
                <div
                    className={`tab ${isActive("/home")}`}
                    onClick={() => navigate("")}
                >
                    Home 🏠
                </div>
                <div
                    className="sidebar-section-header"
                    onClick={() => setIsRepoActionsOpen(!isRepoActionsOpen)}
                >
                    <span>Repository Management</span>
                    <span className={`arrow ${isRepoActionsOpen ? "open" : ""}`}>▶</span>
                </div>
                {isRepoActionsOpen && (
                    <div className="sidebar-section-content">
                        <div
                            className={`tab ${isActive("cloneRepos")}`}
                            onClick={() => navigate("cloneRepos")}
                        >
                            Clone Repositories 🚚
                        </div>
                        <div
                            className={`tab ${isActive("addRepos")}`}
                            onClick={() => navigate("addRepos")}
                        >
                            Add Repositories ➕
                        </div>
                        <div
                            className="tab"
                            onClick={() => setIsIncludingRepos(true)}
                        >
                            Ignored Repos 🚫
                        </div>
                        <div
                            className={`tab ${isActive("snapshots")}`}
                            onClick={() => navigate("snapshots")}
                        >
                            Snapshots 📸
                        </div>
                        <div
                            className={`tab ${isActive("global")}`}
                            onClick={() => navigate("global")}
                        >
                            Global 🌍
                        </div>
                    </div>
                )}
                <div
                    className="sidebar-section-header"
                    onClick={() => setIsExecutionsOptionsOpen(!isExecutionsOptionsOpen)}
                >
                    <span>Execution & Automation</span>
                    <span className={`arrow ${isExecutionsOptionsOpen ? "open" : ""}`}>▶</span>
                </div>
                {isExecutionsOptionsOpen && (
                    <div className="sidebar-section-content">
                        <div
                            className={`tab ${isActive("batchOperations")}`}
                            onClick={() => navigate("batchOperations")}
                        >
                            Batch Ops ⚡
                        </div>
                        <div
                            className={`tab ${isActive("dryRun")}`}
                            onClick={() => navigate("dryRun")}
                        >
                            Simulate 🧪
                        </div>
                    </div>
                )}
                <div
                    className="sidebar-section-header"
                    onClick={() => setIsSystemDiagnosticsOptionsOpen(!isSystemDiagnosticsOptionsOpen)}
                >
                    <span>System & Diagnostics</span>
                    <span className={`arrow ${isSystemDiagnosticsOptionsOpen ? "open" : ""}`}>▶</span>
                </div>
                {isSystemDiagnosticsOptionsOpen && (
                    <div className="sidebar-section-content">
                        <div
                            className={`tab ${isActive("apiStatus")}`}
                            onClick={() => navigate("apiStatus")}
                        >
                            Server Status 🔌
                        </div>

                        <div
                            className={`tab ${isActive("setup")}`}
                            onClick={() => navigate('setup')}>
                            System Settings ⚙️
                        </div>
                        <div
                            className="tab"
                            onClick={handleOpenServerLogsRequest}>
                            Open Server Logs 🔍
                        </div>
                    </div>
                )}
                <div
                    className="sidebar-section-header"
                    onClick={() => setIsUserAndHelpOptionsOpen(!isUserAndHelpOptionsOpen)}
                >
                    <span>User & Help</span>
                    <span className={`arrow ${isUserAndHelpOptionsOpen ? "open" : ""}`}>▶</span>
                </div>
                {isUserAndHelpOptionsOpen && (
                    <div className="sidebar-section-content">
                        <div
                            className={`tab ${isActive("userConfig")}`}
                            onClick={() => navigate("userConfig")}
                        >
                            User Settings 🔧
                        </div>
                        <div
                            className="tab"
                            onClick={() => window.open("/help", "_blank")}>
                            <span className="username">Help</span>
                            <span className="profile-icon">💡</span>
                        </div>
                    </div>
                )}
                <div
                    className="sidebar-help"
                    onClick={() => setIsLoggingOut(true)}>
                    <span className="username">Logout ⏻</span>
                </div>
            </aside>

            <main className="content">
                <div className="panel-card">
                    <Outlet context={{ reposVersion }} />
                </div>
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