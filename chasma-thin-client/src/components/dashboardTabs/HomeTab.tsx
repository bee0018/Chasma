import React from "react";
import '../../css/DasboardTab.css';
import DashboardCard from "../DashboardCard";

/**
 * The Home tab contents and display components.
 * @constructor Initializes a new instance of the HomeTab.
 */
const HomeTab: React.FC = () => {
    return (
        <div>
            <h1 className="page-title">Home🏠</h1>
            <p className="page-description">
                Welcome to the development toolbox! Get started by clicking any of the tabs on the left!
            </p>
            <br/>
            <div className="card-container">
                <DashboardCard
                    title="GitHub"
                    description="Go to GitHub"
                    url="https://github.com/bee0018/Chasma" />
                <DashboardCard
                    title="YouTube"
                    description="Go to YouTube"
                    url="https://www.youtube.com" />
                <DashboardCard
                    title="Google"
                    description="Go to Google"
                    url="https://www.google.com" />
            </div>
        </div>
    );
}

export default HomeTab;