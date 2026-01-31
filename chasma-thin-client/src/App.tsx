import React from 'react';
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

function App() {
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
                    <Route path="git-commands" element={<HelpCommonGitCommandsPage />} />
                </Route>
                <Route path="/status/:repoName/:repoId" element={<RepositoryStatusPage />} />
                <Route path="/workflowruns/:repoName/:repoOwner" element={<WorkflowRunsPage />} />
            </Routes>
        </BrowserRouter>
    </div>;
}

export default App;