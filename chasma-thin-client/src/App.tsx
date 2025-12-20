import React from 'react';
import './css/App.css';
import Dashboard from "./components/Dashboard";
import {BrowserRouter, Route, Routes} from "react-router-dom";
import RepositoryStatusPage from "./components/RepositoryStatusPage";
import WorkflowRunsPage from "./components/WorkflowRunsPage";
import LoginPage from "./components/LoginPage";
import RegisterPage from "./components/RegisterPage";

function App() {
    return <div>
        <BrowserRouter>
            <Routes>
                <Route path="/" element={<LoginPage />} />
                <Route path="/register" element={<RegisterPage />} />
                <Route path="/home" element={<Dashboard />} />
                <Route path="/status/:repoName/:repoId" element={<RepositoryStatusPage />} />
                <Route path="/workflowruns/:repoName/:repoOwner" element={<WorkflowRunsPage />} />
            </Routes>
        </BrowserRouter>
    </div>;
}

export default App;