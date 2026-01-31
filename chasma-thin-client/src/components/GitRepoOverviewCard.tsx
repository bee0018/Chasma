import React from "react";
import {useNavigate} from "react-router-dom";
import {LocalGitRepository} from "../API/ChasmaWebApiClient";

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
                </div>
            </div>

            <div className="repo-actions">
                <button
                    className="repo-action"
                    onClick={(e) => {
                        e.stopPropagation();
                        navigate(`/workflowruns/${props.repository.name}/${props.repository.owner}`);
                    }}
                >
                    Builds
                </button>

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
