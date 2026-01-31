import React from 'react';
import './styles/App.css';
import Dashboard from "./components/Dashboard";
import {BrowserRouter, Route, Routes} from "react-router-dom";
import RepositoryStatusPage from "./components/pages/RepositoryStatusPage";
import WorkflowRunsPage from "./components/pages/WorkflowRunsPage";
import LoginPage from "./components/pages/LoginPage";
import RegisterPage from "./components/pages/RegisterPage";
import HelpPage from "./components/pages/HelpPage";

function App() {
    return <div>
        <BrowserRouter>
            <Routes>
                <Route path="/" element={<LoginPage />} />
                <Route path="/register" element={<RegisterPage />} />
                <Route path="/home" element={<Dashboard />} />
                <Route path="/help" element={<HelpPage />} />
                <Route path="/status/:repoName/:repoId" element={<RepositoryStatusPage />} />
                <Route path="/workflowruns/:repoName/:repoOwner" element={<WorkflowRunsPage />} />
            </Routes>
        </BrowserRouter>
    </div>;
}

export default App;