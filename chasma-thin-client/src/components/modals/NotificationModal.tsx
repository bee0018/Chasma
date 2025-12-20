import React from 'react';
import '../../css/NotificationModal.css';

/**
 * The properties on the notification modal.
 */
interface INotificationModalProps {
    /** The title of the notification. **/
    title: string;
    /** The message of the notification. **/
    message: string | undefined;
    /** Flag indicating whether the notification is an error type or not. **/
    isError: boolean | undefined;
    /** The confirmation action of the close function. **/
    onClose: () => void;
    /** Flag indicating whether the notification is loading. **/
    loading?: boolean;
}

/**
 * Gets the modal icon based for the specific type of notification.
 * @param props The notification modal properties.
 */
function getModalIcon(props: INotificationModalProps) {
    if (props.loading) {
        return <>
            <div className="info-icon">
                <svg
                    xmlns="http://www.w3.org/2000/svg"
                    viewBox="0 0 24 24"
                    width="48"
                    height="48"
                    fill="none"
                >
                    <circle cx="12" cy="12" r="10" fill="#00bfff" />
                    <rect x="11" y="10" width="2" height="7" fill="#ffffff" />
                    <rect x="11" y="7" width="2" height="2" fill="#ffffff" />
                </svg>
            </div>
        </>
    }

    if (props.isError) {
        return <>
            <div className="error-icon">
                <svg
                    xmlns="http://www.w3.org/2000/svg"
                    viewBox="0 0 24 24"
                    width="48"
                    height="48"
                    fill="none"
                >
                    <circle cx="12" cy="12" r="10" fill="#ff4c4c" />
                    <rect x="11" y="6" width="2" height="8" fill="#fff" />
                    <rect x="11" y="16" width="2" height="2" fill="#fff" />
                </svg>
            </div>
        </>
    }

    if (!props.isError) {
        return <>
            <div className="success-icon">
                <svg
                    xmlns="http://www.w3.org/2000/svg"
                    viewBox="0 0 24 24"
                    width="48"
                    height="48"
                    fill="none"
                >
                    <circle cx="12" cy="12" r="10" fill="#4caf50" />
                    <path
                        d="M16 9l-5.2 6L8 11.5"
                        fill="none"
                        stroke="#fff"
                        strokeWidth="2"
                        strokeLinecap="round"
                        strokeLinejoin="round"
                    />
                </svg>
            </div>
        </>
    }
}

/**
 * Instantiates a new instance of the notification modal with the specified props.
 * @param props The notification modal properties.
 * @constructor Initializes a new instance of the NotificationModal.
 */
const NotificationModal: React.FC<INotificationModalProps> = (props: INotificationModalProps) => {
    let icon = getModalIcon(props);
    return (
        <div className="modal-backdrop" onClick={props.onClose}>
            <div className="modal" onClick={(e) => e.stopPropagation()}>
                {icon}
                <h2 className="modal-title">{props.title}</h2>
                <p className="modal-message">{props.message}</p>
                {props.loading && (
                    <div>
                        <div className="modal-spinner">
                            <div className="spinner" />
                        </div>
                    </div>
                )}
                <button className="modal-button" onClick={props.onClose}>
                    Close
                </button>
            </div>
        </div>
    );
};

export default NotificationModal;