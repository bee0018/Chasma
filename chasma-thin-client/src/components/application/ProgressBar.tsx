/**
 * Defines the members of the progress bar interface.
 */
interface IProgressBar {
    /** The message to show when the percentage complete reaches 100 */
    finishedMessage: string;

    /** The message to show when the percentage is not yet 100. */
    unfinishedMessage: string;

    /** The progress percentage. */
    progressPercent: number;

    /** A value indicating whether bar will show blue to signify a non-error progress bar; red to show error otherwise. */
    displayNonErrorProgressBar?: boolean;
}

/**
 * Initializes a new instance of the ProgressBar component.
 * @param props The properties of the progress bar.
 */
const ProgressBar: React.FC<IProgressBar> = (props: IProgressBar) => {
    return (
        <>
            <h2>
                {props.progressPercent === 100 ? props.finishedMessage : props.unfinishedMessage}
            </h2>
            <div className="progress-container">
                {!props.displayNonErrorProgressBar && (
                    <div
                        className="progress-bar"
                        style={{
                            width: `${props.progressPercent}%`,
                            background: props.progressPercent === 100
                                ? "linear-gradient(90deg, #22c55e, #4ade80)"
                                : "linear-gradient(90deg, #973737, #de4a4a)"
                        }}
                    />
                )}
                {props.displayNonErrorProgressBar && (
                    <div
                        className="progress-bar"
                        style={{
                            width: `${props.progressPercent}%`,
                            background: props.progressPercent === 100
                                ? "linear-gradient(90deg, #22c55e, #4ade80)"
                                : "linear-gradient(90deg, #22d3ee, #0ea5e9)"
                        }}
                    />
                )}
            </div>
            <br />
        </>
    );
}

export default ProgressBar;