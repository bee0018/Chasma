import { BannerType } from "../types/CustomTypes";

/**
 * The interface defining the system update banner.
 */
interface ISystemUpdateBanner {
    /** The banner type. */
    bannerType: BannerType;

    /** The message text on the banner. */
    message: string;

    /** The action to invoke when the banner is closed. */
    onClose?: () => void;
}

/**
 * Initializes a new component of the SystemUpdateBanner.
 * @param props The system update banner properties.
 * @constructor
 */
const SystemUpdateBanner: React.FC<ISystemUpdateBanner> = (props: ISystemUpdateBanner) => {
    /** The set of banner icons. */
    const bannerIcons: Record<BannerType, string> = {
        info: 'ℹ️',
        success: '✅',
        warning: '⚠️',
        error: '🛑',
    };

    /** The banner icon. */
    const icon = bannerIcons[props.bannerType];
    return (
        <div className={`banner-container banner-${props.bannerType}`} role="alert">
            <div className="banner-content">
                <span>{icon}</span>
                <span>{props.message}</span>
            </div>
            {props.onClose && (
                <button
                    onClick={props.onClose}
                    className="banner-close-btn"
                    aria-label="Close banner"
                >
                    &times;
                </button>
            )}
        </div>
    );
}

export default SystemUpdateBanner;