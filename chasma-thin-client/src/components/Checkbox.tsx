import React, { useState } from "react";
import "../css/checkbox.css"

/** Interface defining the members of the checkbox. **/
interface ICheckboxProps {
    /** The label of the checkbox. **/
    label: string;

    /** The action to execute once the box is checked. **/
    onBoxChecked: (isChecked: boolean) => void;
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
                checked={checked}
                onChange={(e) => handleOnChangedEvent(e.target.checked)}
            />
            <span className="checkbox-custom" />
            <span className="checkbox-label">{props.label}</span>
        </label>
    );
}

export default Checkbox;
