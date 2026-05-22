import { useState } from "react";
import { GitCloneBlueprint } from "../../../API/ChasmaWebApiClient";
import Checkbox from "../../Checkbox";

/** Interface defining the members of the RepositoryCloneEntryRow. */
interface IRepositoryCloneEntryRow {
    /** The row identifier. */
    rowId: string;

    /** The repository cloning blueprint. */
    blueprint: GitCloneBlueprint;

    /** The action to perform when the user deletes a row. **/
    onDelete: (id: string) => void;

    /** The action to invoke when the user updates a blueprint. */
    onUpdate: (updatedBlueprint: GitCloneBlueprint) => void;

    /** The user-defined global workspace path. */
    globalWorkspacePath: string | undefined;
}

/**
 * Initializes a new instance of the RepositoryCloneEntryRow component.
 * @param props The repository clone entry row properties.
 * @constructor
 */
const RepositoryCloneEntryRow: React.FC<IRepositoryCloneEntryRow> = (props: IRepositoryCloneEntryRow) => {
    /** Gets or sets a value indicating whether the user is cloning a repository to a custom path. */
    const [isUsingCustomPath, setIsUsingCustomPath] = useState<boolean>(false);

    /**
     * Helper to safely clone the class instance structure and change properties immutably
     * @param key The property key.
     * @param value The value of the property.
     */
    const updateField = (key: keyof GitCloneBlueprint, value: any) => {
        let cloned = Object.assign(
            Object.create(Object.getPrototypeOf(props.blueprint)),
            props.blueprint,
            { [key]: value }
        );
        let sourceUrl: string = "sourceUrl";
        if (key === sourceUrl) {
            const repositoryName = extractRepositoryName(value)
            const blueprint = cloned as GitCloneBlueprint;
            let updatedWorkingDirectoryPath: string;
            if (!isUsingCustomPath && props.globalWorkspacePath && (props.globalWorkspacePath.endsWith('/') || props.globalWorkspacePath.endsWith('\\'))) {
                // The working directory ends with a slash so we just need to append the data together.
                updatedWorkingDirectoryPath = props.globalWorkspacePath + repositoryName;
            }
            else {
                if (props.globalWorkspacePath?.includes('/')) {
                    // The is delimited by '/' so we need to add /repositoryName to the end of the working directory.
                    updatedWorkingDirectoryPath = props.globalWorkspacePath + '/' + repositoryName;
                }
                else {
                    // The is delimited by '\' so we need to add \repositoryName to the end of the working directory.
                    updatedWorkingDirectoryPath = props.globalWorkspacePath + '\\' + repositoryName;
                }
            }

            let workingDirectory = "workingDirectory";
            cloned = Object.assign(
                Object.create(Object.getPrototypeOf(blueprint)),
                blueprint,
                { [workingDirectory]: updatedWorkingDirectoryPath }
            );
        }

        props.onUpdate(cloned);
    };

    /**
     * Handles the event when the user toggles the Custom Path button.
     * @param usingCustomPath Flag indicating whether the user is defining a custom path for the cloned repository.
     */
    const handleCustomPathToggleButtonClicked = (usingCustomPath: boolean) => {
        setIsUsingCustomPath(usingCustomPath);
        if (usingCustomPath) {
            updateField("workingDirectory", "");
        }
        else {
            updateField("workingDirectory", props.globalWorkspacePath);
            updateField("sourceUrl", props.blueprint.sourceUrl);
        }
    };

    /**
    * Extracts the repository name from a given Git cloning URL.
    * Supports both SSH and HTTPS links.
    * 
    * @param url The Git repository cloning link.
    * @returns The extracted name of the repository, or an empty string if invalid.
    */
    function extractRepositoryName(url: string): string {
        if (!url || typeof url !== "string") {
            return "";
        }

        const cleanUrl = url.trim().replace(/\/+$/, "");
        const match = cleanUrl.match(/[:/]([^/:]+?)(?:\.git)?$/);
        return match ? match[1] : "";
    }

    return (
        <div className="batch-command-row modern">
            <div className="batch-header">
                <button
                    type="button"
                    className="remove-button modern-remove"
                    title="Remove Repository"
                    onClick={() => props.onDelete(props.rowId)}
                >
                    ×
                </button>
            </div>
            <Checkbox
                label={"Recurse Submodules"}
                onBoxChecked={(checked) => updateField("recurseSubmodules", checked)}
                checked={props.blueprint.recurseSubmodules || false}
                tooltip={"Automatically download nested repositories."}
            />
            <br />
            <div className="form-row">
                <label>Source URL:</label>
                <input
                    type="text"
                    className="input-field"
                    placeholder="HTTPS or SSH URL"
                    value={props.blueprint.sourceUrl || ""}
                    onChange={(e) => updateField("sourceUrl", e.target.value)}
                />
            </div>
            <div className="form-row">
                <label>Repository Path:</label>
                <input
                    type="text"
                    className="input-field"
                    placeholder="Absolute Directory Path"
                    value={props.blueprint.workingDirectory || ""}
                    onChange={(e) => updateField("workingDirectory", e.target.value)}
                />
            </div>
            <div className="form-row">
                <button
                    type="button"
                    className="custom-path-btn"
                    onClick={() => handleCustomPathToggleButtonClicked(!isUsingCustomPath)}
                >
                    {isUsingCustomPath ? "Use Preset Path" : "Custom Path"}
                </button>
            </div>
        </div>
    );
};

export default RepositoryCloneEntryRow;