
/** The interface defining the members of the ComboboxInput component. */
interface IComboboxInput {
    /** The list of items to select in combo box. */
    itemList: string[];

    /** The current value to display in the combo box input. */
    currentValue: string;

    /** The placeholder text to display when there is no entry in the combo box. */
    placeholder: string;

    /** The function to invoke when there is a change. */
    onChange: (newValue: string) => void;

    /** The input field styling. */
    styling: string;
}

/**
 * Initializes a new instance of the ComboboxInput component.
 * @param props The properties of the combo box.
 * @constructor
 */
const ComboboxInput = (props: IComboboxInput) => {
    return (
        <>
            <input
                type="text"
                list="custom-dropdown-options"
                className={props.styling}
                value={props.currentValue}
                onChange={(e) => props.onChange(e.target.value)}
                placeholder={props.placeholder}
            />
            <datalist id="custom-dropdown-options">
                {props.itemList.map((item) => (
                    <option key={item} value={item}>{item}</option>
                ))}
            </datalist>
        </>
    );
};

export default ComboboxInput;