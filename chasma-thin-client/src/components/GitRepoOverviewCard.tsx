import React from "react";
import {useNavigate} from "react-router-dom";
import {ApplicationUser, LocalGitRepository, RemoteHostPlatform} from "../API/ChasmaWebApiClient";

/** The properties of the Card component. */
interface IProps {
    /** The repository. **/
    repository: LocalGitRepository;

    /** The URL of the repository. **/
    url: string | undefined;

    /** The action to execute once a repo is successfully deleted. **/
    onDelete: (repoId: string | undefined) => void;

    /** The action to execute on a user right-clicks on the card. **/
    onContextMenu?: (e: React.MouseEvent) => void;

    /** The logged-in user. **/
    user : ApplicationUser | null;
}

/**
 * The card details and display components.
 * @param props The properties of the card.
 * @constructor Initializes a new instance of the GitRepoOverviewCard.
 */
const GitRepoOverviewCard: React.FC<IProps> = (props) => {
    /** The navigation function. **/
    const navigate = useNavigate();

    return (
        <div
            className="repo-row"
            onClick={() => navigate(`${props.url}`)}
            onContextMenu={props.onContextMenu}
        >
            <div className="repo-main">
                <div className="repo-name">{props.repository.name}</div>
                <div className="repo-meta">
                    <span>ID: {props.repository.id}</span>
                    <span>Owner: {props.repository.owner}</span>
                    <span>
                        Host Platform: {props.repository.hostPlatform ? RemoteHostPlatform[props.repository.hostPlatform] : "Unknown"}
                    </span>
                </div>
            </div>

            <div className="repo-actions">
                {props.user?.permissions
                    && props.user.permissions.isUsingGitHubApi
                    && props.repository.hostPlatform === RemoteHostPlatform.GitHub
                    && (
                        <button
                            className="repo-action"
                            onClick={(e) => {
                                e.stopPropagation();
                                navigate(`/builds/${props.repository.id}`);
                            }}
                        >
                            Workflow Runs
                        </button>
                )}
                {props.user?.permissions
                    && props.user.permissions.isUsingGitLabApi
                    && props.repository.hostPlatform === RemoteHostPlatform.GitLab
                    && (
                        <button
                            className="repo-action"
                            onClick={(e) => {
                                e.stopPropagation();
                                navigate(`/builds/${props.repository.id}`);
                            }}
                        >
                            Pipeline Jobs
                        </button>
                    )}
                <button
                    className="repo-delete"
                    onClick={(e) => {
                        e.stopPropagation();
                        props.onDelete(props.repository.id);
                    }}
                >
                    ×
                </button>
            </div>
        </div>
    );
};

export default GitRepoOverviewCard;
