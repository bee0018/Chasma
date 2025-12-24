import React from "react";
import {useNavigate} from "react-router-dom";
import {DeleteRepositoryRequest, RepositoryConfigurationClient} from "../API/ChasmaWebApiClient";
import {getUserId} from "../managers/LocalStorageManager";

/** The properties of the Card component. */
interface IProps {
    /** The repository identifier. **/
    repoId: string | undefined;

    /** The name of the repository. **/
    repoName: string | undefined;

    /** The owner of the repository. **/
    repoOwner: string | undefined;

    /** The URL of the repository. **/
    url: string | undefined;

    /** The action to execute once a repo is successfully deleted. **/
    onDelete: (repoId: string | undefined) => void;

    /** The action to execute once a repo is successfully deleted. **/
    onError: (errorMessage: string | undefined) => void;
}

/** The repository configuration client to interact with the web API. **/
const repoConfigClient = new RepositoryConfigurationClient();

/**
 * The card details and display components.
 * @param props The properties of the card.
 * @constructor Initializes a new instance of the GitRepoOverviewCard.
 */
const GitRepoOverviewCard: React.FC<IProps> = (props) => {
    /** The navigation function. **/
    const navigate = useNavigate();

    return (
        <div className="card" onClick={() => navigate(`${props.url}`)}>
            <span
                className="card-x"
                onClick={async (e) => {
                    e.stopPropagation();
                    try {
                        const request = new DeleteRepositoryRequest();
                        request.repositoryId = props.repoId;
                        request.userId = getUserId();
                        const response = await repoConfigClient.deleteRepository(request);
                        if (response.isErrorResponse) {
                            props.onError(response.errorMessage)
                            return;
                        }

                        props.onDelete(props.repoId);
                    } catch (e) {
                        console.error(e);
                        props.onError("Error occurred while deleting repository. Check console logs.");
                }}}
            >
                X
            </span>
            <div className="card-title">{props.repoName}</div>
            <div className="card-description">{props.repoOwner}</div>
            <table className="repo-overview-table">
                <thead>
                <tr>
                    <th>◐</th>
                    <th>⚒</th>
                </tr>
                </thead>
                <tbody>
                <tr>
                    <td onClick={(e) => {
                        e.stopPropagation();
                        navigate(`/status/${props.repoName}/${props.repoId}`);
                    }}>Status</td>
                    <td onClick={(e) => {
                        e.stopPropagation();
                        navigate(`/workflowruns/${props.repoName}/${props.repoOwner}`);
                    }}>Builds</td>
                </tr>
                </tbody>
            </table>
        </div>
    );
};

export default GitRepoOverviewCard;
