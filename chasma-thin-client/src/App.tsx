import './styles/App.css';
import Dashboard from "./components/Dashboard";
import { BrowserRouter, Route, Routes } from "react-router-dom";
import RepositoryStatusPage from "./components/pages/RepositoryStatusPage";
import WorkflowRunsPage from "./components/pages/WorkflowRunsPage";
import LoginPage from "./components/pages/LoginPage";
import RegisterPage from "./components/pages/RegisterPage";
import HelpPage from "./components/pages/HelpPage";
import HelpConfigXmlPage from "./components/pages/help/HelpConfigXmlPage";
import HelpFrequentlyAskedQuestionsPage from "./components/pages/help/HelpFrequentlyAskedQuestionsPage";
import HelpGitHubApiIntegrationsPage from "./components/pages/help/HelpGitHubApiIntegrationsPage";
import HelpCommonGitCommandsPage from "./components/pages/help/HelpCommonGitCommandsPage";
import HelpGitLabApiIntegrationPage from "./components/pages/help/HelpGitLabApiIntegrationPage";
import { useCacheStore } from './managers/CacheManager';
import NotificationModal from './components/modals/NotificationModal';
import { userClient } from './managers/ApiClientManager';
import { RefreshRequest, SystemManifest } from './API/ChasmaWebApiClient';
import { useEffect, useRef } from 'react';
import AppSetupPage from './components/pages/AppSetupPage';
import StartupGate from './components/pages/gates/StartupGate';
import UserConfigTab from './components/dashboardTabs/UserConfigTab';
import BatchOperationsTab from './components/dashboardTabs/BatchOperationsTab';
import MultiDryRunSimulationTab from './components/dashboardTabs/MultiDryRunSimulationTab';
import CloneRepositoriesTab from './components/dashboardTabs/CloneRepositoriesTab';
import RepositoryAdditionsTab from './components/dashboardTabs/RepositoryAdditionsTab';
import GlobalRepositoryTab from './components/dashboardTabs/GlobalRepositoryTab';
import ApplySnapshotsTab from './components/dashboardTabs/ApplySnapshotsTab';
import ApiStatusTab from './components/dashboardTabs/ApiStatusTab';
import HomeTab from './components/dashboardTabs/HomeTab';
import ForgotPasswordPage from './components/pages/ForgotPasswordPage';
import * as signalR from "@microsoft/signalr";
import { apiBaseUrl } from './environmentConstants';
import ReportBugsPage from './components/pages/ReportBugsPage';

function App() {
    /** The notification modal to display in the application. */
    const notification = useCacheStore(state => state.notification);

    /** Removes the notification from the view. */
    const clearNotification = useCacheStore(state => state.clearNotification);

    /** Sets the notification modal. */
    const setNotification = useCacheStore(state => state.setNotification);

    /** The last activity from the user. */
    const lastActivity = useRef(Date.now());

    /** The function to set the new system update data. */
    const setNewSystemUpdate = useCacheStore(state => state.setNewSystemUpdate);

    /** Updates the last activity from the user. */
    function updateActivity() {
        lastActivity.current = Date.now();
    }

    /**
     * Gets the token expiration value. It converts the token expiration to milliseconds.
     * @param token The access token.
     * @returns The token expiration value.
     */
    function getTokenExpiration(token: string): number {
        try {
            const payload = JSON.parse(atob(token.split('.')[1]));
            return payload.exp * 1000;
        } catch {
            return 0; // force refresh/logout
        }
    }

    /** Logs the user out of the system. */
    function logoutUser() {
        useCacheStore.getState().clearCache();
        window.location.href = "/";
        setNotification({
            title: "Session expired.",
            message: "Please log in again.",
            isError: true,
        });
    }

    /**
     * The user activity threshold.
     * Note: This is every 10 minutes.
     */
    const ACTIVITY_THRESHOLD = 10 * 60 * 1000;

    /**
     * The user refresh before expiry value.
     * Note; This is every 2 minutes.
     */
    const REFRESH_BEFORE_EXPIRY = 2 * 60 * 1000;
    useEffect(() => {
        const timeout = setTimeout(() => {
            const interval = setInterval(async () => {
                const token = useCacheStore.getState().token;
                if (!token || !useCacheStore.getState().refreshToken) {
                    return;
                }

                const now = Date.now();
                const tokenExpiration = getTokenExpiration(token);
                const isUserActive = (now - lastActivity.current) < ACTIVITY_THRESHOLD;
                const isExpiringSoon = (tokenExpiration - now) < REFRESH_BEFORE_EXPIRY;

                if (isUserActive && isExpiringSoon) {
                    try {
                        const request = new RefreshRequest();
                        request.refreshToken = useCacheStore.getState().refreshToken;

                        if (!request.refreshToken) {
                            logoutUser();
                            return;
                        }

                        const response = await userClient.refresh(request);

                        if (response.isErrorResponse) {
                            logoutUser();
                            return;
                        }

                        useCacheStore.getState().setToken(response.token);
                        useCacheStore.getState().setRefreshToken(response.refreshToken);
                    } catch {
                        logoutUser();
                    }
                }
            }, 30000); // every 30 seconds

            return () => clearInterval(interval);
        }, 5000); // wait 5 seconds after mount/login

        return () => clearTimeout(timeout);
    }, []);

    useEffect(() => {
        window.addEventListener("mousemove", updateActivity);
        window.addEventListener("keydown", updateActivity);
        window.addEventListener("click", updateActivity);
        window.addEventListener("scroll", updateActivity);

        return () => {
            window.removeEventListener("mousemove", updateActivity);
            window.removeEventListener("keydown", updateActivity);
            window.removeEventListener("click", updateActivity);
            window.removeEventListener("scroll", updateActivity);
        };
    }, []);

    useEffect(() => {
        const connection = new signalR.HubConnectionBuilder()
            .withUrl(`${apiBaseUrl}/notificationHub`)
            .withAutomaticReconnect()
            .configureLogging(signalR.LogLevel.Information)
            .build();

        connection.on("OnUpdateDownloaded", (systemManifest: SystemManifest) => {
            setNewSystemUpdate(systemManifest);
        });

        connection
            .start()
            .catch((err) => console.error("SignalR Connection Error: ", err));
        return () => {
            connection.off("OnUpdateDownloaded");
            connection.stop();
        };
    }, [setNewSystemUpdate]);

    return <div>
        <BrowserRouter>
            <Routes>
                <Route path="/" element={<StartupGate />} />
                <Route path="/login" element={<LoginPage />} />
                <Route path="/setup" element={<AppSetupPage />} />
                <Route path="/register" element={<RegisterPage />} />
                <Route path="/forgot-password" element={<ForgotPasswordPage />} />
                <Route path="/home" element={<Dashboard />}>
                    <Route index element={<HomeTab />} />
                    <Route path="userConfig" element={<UserConfigTab />} />
                    <Route path="batchOperations" element={<BatchOperationsTab />} />
                    <Route path="dryRun" element={<MultiDryRunSimulationTab />} />
                    <Route path="cloneRepos" element={<CloneRepositoriesTab />} />
                    <Route path="addRepos" element={<RepositoryAdditionsTab />} />
                    <Route path="global" element={<GlobalRepositoryTab />} />
                    <Route path="snapshots" element={<ApplySnapshotsTab />} />
                    <Route path="apiStatus" element={<ApiStatusTab />} />
                    <Route path="setup" element={<AppSetupPage />} />
                    <Route path="report-bugs" element={<ReportBugsPage />} />
                </Route>
                <Route path="/help" element={<HelpPage />}>
                    <Route path="config" element={<HelpConfigXmlPage />} />
                    <Route path="faq" element={<HelpFrequentlyAskedQuestionsPage />} />
                    <Route path="github-api" element={<HelpGitHubApiIntegrationsPage />} />
                    <Route path="gitlab-api" element={<HelpGitLabApiIntegrationPage />} />
                    <Route path="git-commands" element={<HelpCommonGitCommandsPage />} />
                </Route>
                <Route path="/status/:repoName/:repoId" element={<RepositoryStatusPage />} />
                <Route path="/builds/:repoId" element={<WorkflowRunsPage />} />
            </Routes>
        </BrowserRouter>
        {notification && (
            <NotificationModal
                title={notification.title}
                message={notification.message}
                isError={notification.isError}
                loading={notification.loading}
                onClose={clearNotification}
            />
        )}
    </div>;
}

export default App;