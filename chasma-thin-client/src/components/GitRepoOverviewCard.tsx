import React from "react";
import {useNavigate} from "react-router-dom";
import {LocalGitRepository} from "../API/ChasmaWebApiClient";

/** The properties of the Card component. */
interface IProps {
    /** The repository. **/
    repository: LocalGitRepository

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
        <div className="card"
             onClick={() => navigate(`${props.url}`)}
             onContextMenu={props.onContextMenu}
        >
            <span
                className="card-x"
                onClick={async (e) => {
                    e.stopPropagation();
                    props.onDelete(props.repository.id);
                }}
            >
                X
            </span>
            <div className="card-title">{props.repository.name}</div>
            <div style={{textAlign: "left"}}>
                <div className="card-description"><strong>ID: </strong>{props.repository.id}</div>
                <div className="card-description"><strong>Owner: </strong>{props.repository.owner}</div>
            </div>
            <table className="repo-overview-table">
                <thead>
                <tr>
                    <th>⚒</th>
                </tr>
                </thead>
                <tbody>
                <tr>
                    <td onClick={(e) => {
                        e.stopPropagation();
                        navigate(`/workflowruns/${props.repository.name}/${props.repository.owner}`);
                    }}>Builds</td>
                </tr>
                </tbody>
            </table>
        </div>
    );
};

export default GitRepoOverviewCard;
