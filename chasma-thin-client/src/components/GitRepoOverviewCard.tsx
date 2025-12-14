import React from "react";

/** The properties of the Card component. */
interface IProps {
    /** The name of the card. **/
    repoName: string | undefined;

    /** The owner of the repository. **/
    repoOwner: string | undefined;

    /** The URL of the repository. **/
    url: string | undefined;
}

/**
 * The card details and display components.
 * @param props The properties of the card.
 * @constructor Initializes a new instance of the GitRepoOverviewCard.
 */
const GitRepoOverviewCard: React.FC<IProps> = (props) => {
    /** Handles the event of when the user clicks on a card. **/
    const handleClick = () => {
        window.open(props.url, "_blank");
    };

    return (
        <div className="card" onClick={handleClick}>
            <div className="card-title">{props.repoName}</div>
            <div className="card-description">{props.repoOwner}</div>
        </div>
    );
};

export default GitRepoOverviewCard;
