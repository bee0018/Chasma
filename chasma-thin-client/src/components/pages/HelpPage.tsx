import { Outlet, useLocation, useNavigate, Navigate } from "react-router-dom";
import React from "react";

/** The help documentation sections. **/
const sections = [
    { id: "config", title: "Config File (XML)" },
    { id: "api-start", title: "Start / Restart Web API" },
    { id: "github-api", title: "GitHub API Integration" },
    { id: "git-commands", title: "Common Git Commands" },
    { id: "context-menu", title: "Repository Context Menu" },
    { id: "faq", title: "FAQ" },
];

/**
 * Initializes a new instance of the parent HelpPage class.
 * @constructor
 */
export const HelpPage: React.FC = () => {
    /** The use location utility. **/
    const location = useLocation();

    /** The use navigation utility. **/
    const navigate = useNavigate();

    /** Get the last part of the URL path. **/
    const activeSection = location.pathname.split("/").pop();

    /** If user is at /help (no subsection), redirect to first section. **/
    if (!activeSection || activeSection === "help") {
        return <Navigate to={`/help/${sections[0].id}`} replace />;
    }

    return (
        <div className="help-page">
            <aside className="help-nav">
                <h3 className="help-nav-title">Help & Docs</h3>

                {sections.map(section => (
                    <button
                        key={section.id}
                        className={`help-nav-item ${activeSection === section.id ? "active" : ""}`}
                        onClick={() => navigate(`/help/${section.id}`)}
                    >
                        {section.title}
                    </button>
                ))}
            </aside>

            <main className="help-marquee">
                <Outlet />
            </main>
        </div>
    );
};

export default HelpPage;
