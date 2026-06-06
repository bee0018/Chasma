import { LocalGitRepository, RepositorySnapshotBlueprint, WorkContextSnapshot } from "../../../API/ChasmaWebApiClient";
import { SnapshotMode } from "../../types/CustomTypes";

/** Interface for the members on the repository snapsot row. **/
interface IRepositorySnapshotRowProps {
    /** The snapshot view mode. */
    snapshotMode: SnapshotMode;

    /** True if this specific row is selected and should highlight. */
    isSelected?: boolean;

    /** The row identifier. */
    rowId?: string;

    /** The snapshot. */
    snapshot?: WorkContextSnapshot;

    /** The repository snapshot blueprint. */
    blueprint?: RepositorySnapshotBlueprint;

    /** The local git repositories in the system. */
    repositories?: LocalGitRepository[];

    /** The action to perform when the user deletes a row. **/
    onRepositoryDelete?: (id: string) => void;

    /** The action to invoke when the user wants to delete the snapshot. */
    onSnapshotDelete?: (snapshotId: number) => void;

    /** The action to invoke when the user updates a blueprint. */
    onUpdate?: (updatedBlueprint: RepositorySnapshotBlueprint) => void;

    /** The action to invoke when the row is selected. */
    onSelected?: (snapshot: number) => void;
}

/**
 * Initializes a new RepositorySnapshotRow class.
 * @param props The properties to interact with repository snapshots.
 * @constructor
 */
const RepositorySnapshotRow: React.FC<IRepositorySnapshotRowProps> = (props: IRepositorySnapshotRowProps) => {
    /** Currently selected repository **/
    const selectedRepo: LocalGitRepository | null = props.repositories?.find(r => r.id === props.blueprint?.repositoryId) || null;

    /**
     * Helper to safely clone the class instance structure and change properties immutably
     * @param key The property key.
     * @param value The value of the property.
     */
    const updateField = (key: keyof RepositorySnapshotBlueprint, value: string) => {
        let cloned = Object.assign(
            Object.create(Object.getPrototypeOf(props.blueprint)),
            props.blueprint,
            { [key]: value }
        );
        if (props.onUpdate) {
            props.onUpdate(cloned);
        }
    }

    /**
     * Handles the event when the user wants to delete the entry.
     */
    const handleDelete = () => {
        if (props.onRepositoryDelete && props.rowId && props.snapshotMode === "add") {
            props.onRepositoryDelete(props.rowId);
        }
        else if (props.snapshotMode === "apply" && props.onSnapshotDelete && props.snapshot && props.snapshot.snapshotId) {
            props.onSnapshotDelete(props.snapshot.snapshotId)
        }
        else {
            console.log("Delete operation is not able to be carried out.")
        }
    };

    /** Handles the event when the row is selected. */
    const handleRowSelected = () => {
        if (props.onSelected && props.snapshot && props.snapshot.snapshotId) {
            props.onSelected(props.snapshot.snapshotId)
        }
    };

    return (
        <>
            <div
                className={`batch-command-row modern ${props.isSelected ? 'selected' : ''}`}
                onClick={handleRowSelected}>
                <div className="batch-header">
                    <button
                        className="remove-button modern-remove"
                        title={props.snapshotMode === "add" ? "Remove Repository" : "Remove Snapshot"}
                        onClick={e => {
                            e.stopPropagation();
                            handleDelete();
                        }}
                    >
                        ×
                    </button>
                </div>
                {props.snapshotMode === "add" && (
                    <div className="repo-section modern">
                        <select
                            className="repo-dropdown modern-input"
                            value={props.blueprint?.repositoryId ?? ""}
                            onChange={(e) => updateField("repositoryId", e.target.value)}
                        >
                            <option value="">Select Repository</option>
                            {props.repositories?.map(repo => (
                                <option key={repo.id} value={repo.id}>
                                    {repo.displayName ? repo.displayName : repo.name}
                                </option>
                            ))}
                        </select>

                        {selectedRepo && (
                            <div className="repo-metadata modern-card">
                                <div className="repo-meta-line">
                                    <strong>Repo Title:</strong> {selectedRepo.name}
                                </div>
                                <div className="repo-meta-line">
                                    <strong>Repo ID:</strong> {selectedRepo.id}
                                </div>
                                <div className="repo-meta-line">
                                    <strong>Repo Owner:</strong> {selectedRepo.owner}
                                </div>
                                <div className="repo-meta-line">
                                    <strong>Repository Note:</strong>
                                    <textarea
                                        className="textarea-field"
                                        placeholder="(Optional)"
                                        style={{ marginTop: "10px" }}
                                        value={props.blueprint?.intentNote || ""}
                                        onChange={(e) => updateField("intentNote", e.target.value)}
                                    />
                                </div>
                            </div>
                        )}
                    </div>
                )}
                {props.snapshotMode === "apply" && props.snapshot && (
                    <div className="repo-metadata modern-card">
                        <div className="repo-meta-line">
                            <strong>Snapshot Id:</strong> {props.snapshot.snapshotId}
                        </div>
                        <div className="repo-meta-line">
                            <strong>Name:</strong> {props.snapshot.displayName}
                        </div>
                        <div className="repo-meta-line">
                            <strong>Note:</strong> {props.snapshot.snapshotNote}
                        </div>
                    </div>
                )}
            </div>
        </>
    )
}

export default RepositorySnapshotRow;