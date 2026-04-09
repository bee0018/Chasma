import './styles/App.css';
import Dashboard from "./components/Dashboard";
import {BrowserRouter, Route, Routes} from "react-router-dom";
import RepositoryStatusPage from "./components/pages/RepositoryStatusPage";
import WorkflowRunsPage from "./components/pages/WorkflowRunsPage";
import LoginPage from "./components/pages/LoginPage";
import RegisterPage from "./components/pages/RegisterPage";
import HelpPage from "./components/pages/HelpPage";
import HelpConfigXmlPage from "./components/pages/help/HelpConfigXmlPage";
import HelpApiStartPage from "./components/pages/help/HelpApiStartPage";
import HelpFrequentlyAskedQuestionsPage from "./components/pages/help/HelpFrequentlyAskedQuestionsPage";
import HelpRepoContextMenuPage from "./components/pages/help/HelpRepoContextMenuPage";
import HelpGitHubApiIntegrationsPage from "./components/pages/help/HelpGitHubApiIntegrationsPage";
import HelpCommonGitCommandsPage from "./components/pages/help/HelpCommonGitCommandsPage";
import HelpGitLabApiIntegrationPage from "./components/pages/help/HelpGitLabApiIntegrationPage";
import { useCacheStore } from './managers/CacheManager';
import NotificationModal from './components/modals/NotificationModal';
import { userClient } from './managers/ApiClientManager';
import { RefreshRequest } from './API/ChasmaWebApiClient';
import { useEffect, useRef } from 'react';

function App() {
    /** The notification modal to display in the application. */
    const notification = useCacheStore(state => state.notification);

    /** Removes the notification from the view. */
    const clearNotification = useCacheStore(state => state.clearNotification);

    /** Sets the notification modal. */
    const setNotification = useCacheStore(state => state.setNotification);

    /** The last activity from the user. */
    const lastActivity = useRef(Date.now());

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

    return <div>
        <BrowserRouter>
            <Routes>
                <Route path="/" element={<LoginPage />} />
                <Route path="/register" element={<RegisterPage />} />
                <Route path="/home" element={<Dashboard />} />
                <Route path="/help" element={<HelpPage />}>
                    <Route path="config" element={<HelpConfigXmlPage />} />
                    <Route path="api-start" element={<HelpApiStartPage />} />
                    <Route path="faq" element={<HelpFrequentlyAskedQuestionsPage />} />
                    <Route path="context-menu" element={<HelpRepoContextMenuPage />} />
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