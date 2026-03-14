import React, {useEffect, useState} from "react";
import { useCacheStore } from "../../../managers/CacheManager";
import {GitBranchRequest, LocalGitRepository} from "../../../API/ChasmaWebApiClient";
import {GitSimulationCase, SimulationEntry} from "../../types/CustomTypes";
import {branchClient} from "../../../managers/ApiClientManager";
import {isBlankOrUndefined} from "../../../stringHelperUtil";

/** Interface for the members on the simulation row. **/
interface ISimulationEntryRow {
    /** The row identifier. **/
    id: string;

    /** Selected repository ID **/
    repositoryId?: string;

    /** The action to perform when the user deletes a row. **/
    onDelete: (id: string) => void;

    /** The action to perform when the row changes **/
    onUpdate: (entry: SimulationEntry) => void;
}

/**
 * A single simulation row that allows selection of a repository.
 * @param props The simulation row properties.
 * @constructor
 */
const SimulationEntryRow: React.FC<ISimulationEntryRow> = (props) => {
    /** The cached repositories belonging to the logged-in user. **/
    const repositories = useCacheStore((state) => state.repositories);

    /** Gets or sets the currently selected repository **/
    const [selectedRepo, setSelectedRepo] = React.useState<LocalGitRepository | undefined>(repositories?.find(r => r.id === props.repositoryId));

    /** Gets or sets the simulation case. **/
    const [simulationCase, setSimulationCase] = useState<string>("Select Simulation");

    /** Gets or sets the remote branches to checkout. **/
    const [branchesList, setBranchesList] = useState<string[] | undefined>([]);

    /** Gets or sets the branch name to add. **/
    const [branchToAdd, setBranchToAdd] = useState<string | undefined>("");

    /** Gets or sets the branch name to pull. **/
    const [branchToPull, setBranchToPull] = useState<string | undefined>("");

    /** Gets or sets the destination branch. **/
    const [destinationBranch, setDestinationBranch] = useState<string | undefined>(undefined);

    /** Gets or sets the working branch name. **/
    const [baseBranchName, setBaseBranchName] = useState<string | undefined>(undefined);

    /**
     * Handles the event when the repository selection changes.
     * @param repoId The repository identifier.
     */
    const handleRepoChange = async (repoId: string) => {
        handleUpdateEntry(repoId, simulationCase);
        setSelectedRepo(repositories?.find(r => r.id === repoId))
        if (isBlankOrUndefined(repoId)) {
            setSimulationCase(GitSimulationCase.Select.toString());
            resetSimulationData();
        }

        await fetchAssociatedBranches(repoId);
    };

    /**
     * Handles the event to update a simulation entry.
     * @param repoId The repository identifier.
     * @param incomingSimCase The incoming simulation case.
     */
    const handleUpdateEntry = (repoId: string | undefined, incomingSimCase: string) => {
        const simCase = convertSimulationCase(incomingSimCase);
        const entry : SimulationEntry = {
            id: props.id,
            simCase: simCase,
            repositoryId: repoId,
            branchToPull: branchToPull,
            branchToAdd: branchToAdd,
            baseBranchToMerge: baseBranchName,
            destinationBranchToMerge: destinationBranch,
        };
        props.onUpdate(entry);
    }

    /**
     * Converts the current simulation case to its enum value.
     * @param incomingSimulationCase The current simulation case.
     */
    const convertSimulationCase = (incomingSimulationCase: string) => {
      switch (incomingSimulationCase) {
          case "Pull":
              return GitSimulationCase.Pull;
          case "Add Branch":
              return GitSimulationCase.AddBranch;
          case "Merge":
              return GitSimulationCase.Merge;
          case "Select Simulation":
          default:
              return GitSimulationCase.Select;
      }
    };

    /**
     * Handles the event when the user updates the simulation case.
     * @param simCase The git operation to simulate.
     */
    const handleSimulationCaseChange = async (simCase : string) => {
        setSimulationCase(simCase);
        resetSimulationData();
        if (simCase === "Pull" || simCase === "Merge") {
            await fetchAssociatedBranches(props.repositoryId);
        }

        handleUpdateEntry(props.repositoryId, simCase);
    }

    /** Resets the simulation data fields. **/
    const resetSimulationData = () => {
        setBranchToPull(undefined);
        setBranchToAdd(undefined);
        setBaseBranchName(undefined);
        setDestinationBranch(undefined);
    };

    /**
     * Fetches the local and remote branches associated with the specified repository.
     * @param repoId The repository identifier.
     */
    async function fetchAssociatedBranches(repoId: string | undefined) {
        if (!repoId) return;

        const request = new GitBranchRequest();
        request.repositoryId = repoId;
        try {
            const response = await branchClient.getBranches(request);
            if (response.isErrorResponse) {
                console.error(response.errorMessage);
                setBranchesList([]);
                return;
            }

            setBranchesList(response.branchNames);
            if (response.branchNames && response.branchNames.length > 0) {
                const firstBranch = response.branchNames[0];
                setBranchToAdd(prev => prev ?? firstBranch);
                setBaseBranchName(prev => prev ?? firstBranch);
                setDestinationBranch(prev => prev ?? firstBranch);
            }
        }
        catch (e) {
            console.error(e);
        }
    }

    useEffect(() => {
        handleUpdateEntry(selectedRepo?.id, simulationCase);
    }, [
        selectedRepo,
        props.repositoryId,
        simulationCase,
        branchToPull,
        branchToAdd,
        baseBranchName,
        destinationBranch
    ]);

    return (
        <div className="batch-command-row modern">
            <div className="batch-header">
                <button
                    className="remove-button modern-remove"
                    title="Remove Repository"
                    onClick={() => props.onDelete(props.id)}
                >
                    ×
                </button>
            </div>
            <div className="repo-section modern">
                <select
                    className="repo-dropdown modern-input"
                    onChange={(e) => handleRepoChange(e.target.value)}
                >
                    <option value="">Select Repository</option>
                    {repositories?.map(repo => (
                        <option key={repo.id} value={repo.id}>
                            {repo.name}
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
                    </div>
                )}
            </div>

            <section className="uniform-command-section">
                <h3>Simulation Case</h3>
                <div className="uniform-commands-list">
                    <select
                        className="repo-dropdown modern-input"
                        value={simulationCase}
                        onChange={(e) => handleSimulationCaseChange(e.target.value)}
                        style={{ marginBottom: "10px" }}
                    >
                        <option value={GitSimulationCase.Select}>{GitSimulationCase.Select}</option>
                        <option value={GitSimulationCase.Pull}>{GitSimulationCase.Pull}</option>
                        <option value={GitSimulationCase.AddBranch}>{GitSimulationCase.AddBranch}</option>
                        <option value={GitSimulationCase.Merge}>{GitSimulationCase.Merge}</option>
                    </select>
                </div>
                {simulationCase === "Pull" && branchesList && branchesList.length > 0 && (
                    <select
                        value={branchToAdd}
                        onChange={(e) => setBranchToAdd(e.target.value)}
                        className="modal-input-field"
                    >
                        {branchesList.map((branch) => (
                            <option key={branch} value={branch}>{branch}</option>
                        ))}
                    </select>
                )}
                {simulationCase === "Add Branch" &&
                    <input
                        type="text"
                        className="input-field"
                        placeholder="Simulate branch to add"
                        value={branchToAdd}
                        onChange={(e) => setBranchToAdd(e.target.value)}
                    />
                }
                {simulationCase === "Merge" &&
                    <>
                        <select
                            value={baseBranchName ?? ""}
                            onChange={(e) => setBaseBranchName(e.target.value)}
                            className="modal-input-field"
                        >
                            {branchesList && branchesList.map((branch) => (
                                <option key={branch} value={branch}>{branch}</option>
                            ))}
                        </select>
                        <select
                            value={destinationBranch ?? ""}
                            onChange={(e) => setDestinationBranch(e.target.value)}
                            className="modal-input-field"
                            style={{ marginBottom: "30px" }}
                        >
                            {branchesList && branchesList.map((branch) => (
                                <option key={branch} value={branch}>{branch}</option>
                            ))}
                        </select>
                        <span><code>{baseBranchName}</code> ➜ <code>{destinationBranch}</code></span>
                    </>
                }
            </section>
        </div>
    );
};

export default SimulationEntryRow;
