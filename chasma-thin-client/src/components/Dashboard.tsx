import React, { useState } from 'react';
import HomeTab from "./dashboardTabs/HomeTab";
import ApiStatusTab from "./dashboardTabs/ApiStatusTab";
import IncludeRepositoryModal from "./modals/IncludeRepositoryModal";
import BatchOperationsTab from "./dashboardTabs/BatchOperationsTab";
import NotificationModal from "./modals/NotificationModal";
import {useCacheStore} from "../managers/CacheManager";
import {AddGitRepositoryRequest} from "../API/ChasmaWebApiClient";
import AddRepositoryModal from "./modals/AddRepositoryModal";
import {configClient} from "../managers/ApiClientManager";
import MultiDryRunSimulationTab from "./dashboardTabs/MultiDryRunSimulationTab";

/**
 * Initializes a new instance of the Dashboard class.
 * @constructor
 */
const Dashboard: React.FC = () => {
    /** Gets or sets the active tab that the user has selected. **/
    const [activeTab, setActiveTab] = useState("home");

    /** Gets or sets a value indicating whether the user is including repositories. **/
    const [isIncludingRepos, setIsIncludingRepos] = useState(false);

    /** Gets or sets a value indicating whether the user is adding a repository. **/
    const [isAddingRepo, setIsAddingRepo] = useState(false);

    /** Gets or sets the repository version. Serves as a trigger to update child components. **/
    const [reposVersion, setReposVersion] = useState(0);

    /** Gets or sets the notification **/
    const [notification, setNotification] = useState<{
        title: string,
        message: string | undefined,
        isError: boolean | undefined,
        loading?: boolean
    } | null>(null);

    /** The logged-in user. **/
    const user = useCacheStore((state) => state.user);

    /** Handles the trigger when the repositories are updated. **/
    const handleReposUpdated = () => {
        setReposVersion(v => v + 1);
    };

    /** Handles the event when the user selects a tab. **/
    const handleTabClick = (tab: string) => {
        setActiveTab(tab);
    };

    /**
     * Handles the event when the user attempts to add a repository to the application.
     * @param repoPath The repository path.
     */
    const handleAddLocalRepository = async (repoPath: string) => {
        setNotification({
            title: "Adding local git repository from logical drive...",
            message: "Please wait while your request is being processed. May take a while depending on how large your filesystem is.",
            isError: false,
            loading: true
        });
        const request = new AddGitRepositoryRequest();
        request.userId = user?.userId;
        request.repositoryPath = repoPath;
        try {
            const response = await configClient.addGitRepository(request);
            if (response.isErrorResponse) {
                setNotification({
                    title: "Failed to add repository!",
                    message: response.errorMessage,
                    isError: true,
                });
                return;
            }

            if (!response.repository) {
                setNotification({
                    title: "Failed to add repository!",
                    message: "Received empty repository from the internal server.",
                    isError: true,
                });
                return;
            }

            setNotification({
                title: "Successfully added repository!",
                message: `Close the modal to start managing ${response.repository.name}!`,
                isError: false,
            });
            useCacheStore.getState().addLocalGitRepository(response.repository);
            handleReposUpdated();
        } catch (error) {
            setNotification({
                title: "Failed to add repository!",
                message: "Review console logs for more information.",
                isError: true,
            });
            console.error("Error when attempting to add repository", error);
        }
    };

    return (
        <div className="dashboard-container">
            <aside className="sidebar">
                <div className="sidebar-profile">
                    <span className="profile-icon">👤</span>
                    <span className="username">{user?.username}</span>
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
                    className="tab"
                    onClick={() => setIsAddingRepo(true)}
                >
                    ➕ Add Repo
                </div>
                <div
                    className="tab"
                    onClick={() => setIsIncludingRepos(true)}
                >
                    🚫 Ignored Repos
                </div>
                <div
                    className={`tab ${activeTab === "apiStatus" ? "active" : ""}`}
                    onClick={() => handleTabClick("apiStatus")}
                >
                    🔌 API Status
                </div>
                <div
                    className="sidebar-help"
                    onClick={() => window.open("\help", "_blank")}>
                    <span className="profile-icon">💡</span>
                    <span className="username">Help</span>
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
                {activeTab === "apiStatus" && (
                    <div className="panel-card">
                        <ApiStatusTab />
                    </div>
                )}
            </main>

            {isIncludingRepos && (
                <IncludeRepositoryModal
                    onClose={() => setIsIncludingRepos(false)}
                    onRepositoriesUpdated={handleReposUpdated} />
            )}
            {notification && (
                <NotificationModal
                    title={notification.title}
                    message={notification.message}
                    isError={notification.isError}
                    loading={notification.loading}
                    onClose={() => setNotification(null)} />
            )}
            {isAddingRepo && (
                <AddRepositoryModal
                    onClose={() => setIsAddingRepo(false)}
                    onRepositorySelected={handleAddLocalRepository} />
            )}
        </div>
    );
};

export default Dashboard;