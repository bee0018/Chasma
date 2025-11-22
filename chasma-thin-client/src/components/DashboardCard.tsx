import React from "react";

/** The properties of the Card component. */
interface IProps {
    title: string;
    description: string;
    url: string;
}

/**
 * The card details and display components.
 * @param props The properties of the card.
 * @constructor Initializes a new instance of the DashboardCard.
 */
const DashboardCard: React.FC<IProps> = (props) => {
    const handleClick = () => {
        window.open(props.url, "_blank");
    };

    return (
        <div className="card" onClick={handleClick}>
            <div className="card-title">{props.title}</div>
            <div className="card-description">{props.description}</div>
        </div>
    );
};

export default DashboardCard;
