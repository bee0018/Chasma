import React, { useState } from "react";

/** Interface defining the members of the checkbox. **/
interface ICheckboxProps {
    /** The label of the checkbox. **/
    label: string;

    /** The action to execute once the box is checked. **/
    onBoxChecked: (isChecked: boolean) => void;

    /** The flag indicating whether the checked event is managed with custom logic outside the component. **/
    checked?: boolean;

    /** The tooltip for the label. **/
    tooltip?: string;
}

/**
 * Initializes a new Checkbox class.
 * @constructor
 */
const Checkbox: React.FC<ICheckboxProps> = (props: ICheckboxProps) => {
    /** Gets or sets a value indicating whether the box is checked. **/
    const [checked, setChecked] = useState(false);

    /** Handles the event when the user checks the button. **/
    const handleOnChangedEvent= (isChecked: boolean) => {
        setChecked(isChecked);
        props.onBoxChecked(isChecked);
    }
    return (
        <label className="themed-checkbox">
            <input
                type="checkbox"
                checked={props.checked !== undefined ? props.checked : checked}
                onChange={(e) => handleOnChangedEvent(e.target.checked)}
            />
            <span className="checkbox-custom" />
            <span
                className="checkbox-label"
                title={props.tooltip}
            >
                {props.label}
            </span>
        </label>
    );
}

export default Checkbox;
