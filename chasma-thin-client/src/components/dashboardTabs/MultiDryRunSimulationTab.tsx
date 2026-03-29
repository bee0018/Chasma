import React, {useEffect, useState} from "react";
import {
    AddBranchSimulationEntry,
    MergeSimulationEntry,
    PullSimulationEntry,
    SimulateAddBranchRequest,
    SimulateBranchMergeRequest,
    SimulatedAddBranchResult,
    SimulatedGitPullResult,
    SimulatedMergeResult,
    SimulateGitPullRequest,
} from "../../API/ChasmaWebApiClient";
import {useCacheStore} from "../../managers/CacheManager";
import Checkbox from "../Checkbox";
import {GitSimulationCase, SimulationEntry} from "../types/CustomTypes";
import {dryRunClient} from "../../managers/ApiClientManager";
import SimulationEntryRow from "./rows/SimulationEntryRow";
import { useNavigate } from "react-router-dom";
import { handleApiError } from "../../managers/TransactionHandlerManager";

/**
 * Initializes a new MultiDryRunSimulationTab class.
 * @constructor
 */
const MultiDryRunSimulationTab: React.FC = () => {
    /** Cached repositories. **/
    const repositories = useCacheStore((state) => state.repositories);

    /** The logged-in user. **/
    const user = useCacheStore((state) => state.user);

    /** The navigation function. **/
    const navigate = useNavigate();

   /** Sets the notification modal. */
   const setNotification = useCacheStore(state => state.setNotification);

    /** Gets or sets the simulation entries to conduct simulations on. **/
    const [simulationEntries, setSimulationEntries] = useState<SimulationEntry[]>([]);

    /** Gets or sets a value indicating whether the user is selecting all repositories to execute commands. **/
    const [isSelectingAllRepositories, setIsSelectingAllRepositories] = useState<boolean>(false);

    /** Gets or sets the simulated pull results. **/
    const [simulatedPullResults, setSimulatedPullResults] = useState<SimulatedGitPullResult[]>([]);

    /** Gets or sets the simulated add branch results. **/
    const [simulatedAddBranchResults, setSimulatedAddBranchResults] = useState<SimulatedAddBranchResult[]>([]);

    /** Gets or sets the simulated merge results. **/
    const [simulatedMergeResults, setSimulatedMergeResults] = useState<SimulatedMergeResult[]>([]);

    /**
     * Adds a new simulation entry to edit.
     */
    const addSimulationEntryRow = () => {
        setSimulationEntries(prev => [
            ...prev,
            {id: crypto.randomUUID(), simCase: GitSimulationCase.Select}
        ])
    }

    /**
     * Deletes the simulation entry with the specified row identifier.
     * @param id The row identifier.
     */
    const deleteSimulationEntry = (id: string) => {
        setSimulationEntries(prev => prev.filter(row => row.id !== id));
    };

    /**
     * Updates a simulation row entry.
     * @param entry The simulation entry.
     */
    const updateSimulateEntryRow = (entry : SimulationEntry) => {
        setSimulationEntries(prev =>
            prev.map(row =>
                row.id === entry.id ? entry : row
            )
        );
    };

    /**
     * Cleans up the simulation results from the console.
     */
    const cleanUpSimulationResults = () => {
        setSimulatedPullResults([]);
        setSimulatedAddBranchResults([]);
        setSimulatedMergeResults([]);
    }

    /**
     * Simulates the simulation dry run and sets the results.
     */
    const handleSimulationDryRun = async () => {
        const runPhrase = simulationEntries.length > 1 ? "runs" : "run";
        setNotification({
            title: `Perform simulation ${runPhrase}...`,
            message: "May take a few moments...",
            isError: false,
            loading: true,
        });

        const pullSimulationInputs: PullSimulationEntry[] = [];
        const addBranchSimulationInputs: AddBranchSimulationEntry[] = [];
        const mergeSimulationInputs: MergeSimulationEntry[] = [];
        simulationEntries.forEach(entry => {
            if (entry.simCase === GitSimulationCase.Pull) {
                const pullEntry = new PullSimulationEntry();
                pullEntry.repositoryId = entry.repositoryId;
                pullEntry.branchToPull = entry.branchToPull;
                pullSimulationInputs.push(pullEntry);
            }
            else if (entry.simCase === GitSimulationCase.AddBranch) {
                const addBranchEntry = new AddBranchSimulationEntry();
                addBranchEntry.repositoryId = entry.repositoryId;
                addBranchEntry.branchToAdd = entry.branchToAdd;
                addBranchSimulationInputs.push(addBranchEntry);
            }
            else if (entry.simCase === GitSimulationCase.Merge) {
                const mergeEntry = new MergeSimulationEntry();
                mergeEntry.repositoryId = entry.repositoryId;
                mergeEntry.userId = user?.userId;
                mergeEntry.sourceBranch = entry.baseBranchToMerge;
                mergeEntry.destinationBranch = entry.destinationBranchToMerge;
                mergeSimulationInputs.push(mergeEntry);
            }
        });

        const pullSimSuccess = await sendPullSimulationRequest(pullSimulationInputs);
        const addBranchSimSuccess = await sendAddBranchSimulationRequest(addBranchSimulationInputs);
        const mergeSimSuccess = await sendMergeSimulationRequest(mergeSimulationInputs);
        if (pullSimSuccess && addBranchSimSuccess && mergeSimSuccess) {
            setNotification({
                title: "Simulation operation completed!",
                message: "Review the output window for details.",
                isError: false,
            });
        }
    }

    /**
     * Sends the git pull simulation request to the API.
     * @param pullSimulationInputs The pull simulation entry inputs.
     */
    const sendPullSimulationRequest = async (pullSimulationInputs: PullSimulationEntry[]) => {
        try {
            const request = new SimulateGitPullRequest();
            request.entries = pullSimulationInputs;
            const response = await dryRunClient.simulateGitPull(request);
            if (response.isErrorResponse) {
                setNotification({
                    title: "Pull Simulation Failed!",
                    message: response.errorMessage,
                    isError: true,
                });
                return false;
            }

            if (response.pullResults) {
                response.pullResults.forEach(result => {
                    setSimulatedPullResults(prev => [...prev, result]);
                });
            }

            return true;
        }
        catch (e) {
            const errorNotification = handleApiError(e, navigate, "Error performing pull simulation!", "Review server logs for more information.");
            setNotification(errorNotification);
            return false;
        }
    }

    /**
     * Sends the add branch simulation request to the API.
     * @param addBranchSimulationInputs The add branch simulation entry inputs.
     */
    const sendAddBranchSimulationRequest = async (addBranchSimulationInputs: AddBranchSimulationEntry[]) => {
        try {
            const request = new SimulateAddBranchRequest();
            request.entries = addBranchSimulationInputs;
            const response = await dryRunClient.simulateAddBranch(request);
            if (response.isErrorResponse) {
                setNotification({
                    title: "Add Branch Simulation Failed!",
                    message: response.errorMessage,
                    isError: true,
                });
                return false;
            }

            if (response.simulationResults) {
                response.simulationResults.forEach(result => {
                    setSimulatedAddBranchResults(prev => [...prev, result]);
                });
            }

            return true;
        }
        catch (e) {
            const errorNotification = handleApiError(e, navigate, "Error performing add branch simulation!", "Review server logs for more information.");
            setNotification(errorNotification);
            return false;
        }
    }

    /**
     * Sends the merge simulation request to the API.
     * @param mergeSimulationInputs The merge simulation entry inputs.
     */
    const sendMergeSimulationRequest = async (mergeSimulationInputs: MergeSimulationEntry[]) => {
        try {
            const request = new SimulateBranchMergeRequest();
            request.mergeEntries = mergeSimulationInputs;
            const response = await dryRunClient.simulateMergeBranches(request);
            if (response.isErrorResponse) {
                setNotification({
                    title: "Merge Simulation Failed!",
                    message: response.errorMessage,
                    isError: true,
                });
                return false;
            }

            if (response.simulationResults) {
                response.simulationResults.forEach(result => {
                    setSimulatedMergeResults(prev => [...prev, result]);
                });
            }

            return true;
        }
        catch (e) {
            const errorNotification = handleApiError(e, navigate, "Error performing merge simulation!", "Review server logs for more information.");
            setNotification(errorNotification);
            return false;
        }
    }

    useEffect(() => {
        if (isSelectingAllRepositories) {
            if (!repositories || repositories.length === 0) return;
            setSimulationEntries(
                repositories.map(repo => ({
                    id: crypto.randomUUID(),
                    simCase: GitSimulationCase.Select,
                    repositoryId: repo.id,
                }))
            );

        } else {
            // Reset to one empty row when unchecked
            setSimulationEntries([{ id: crypto.randomUUID(), simCase: GitSimulationCase.Select }]);
        }
    }, [isSelectingAllRepositories, repositories]);

    return (
        <>
            <div className="content">
                <div className="main-layout">
                    {/* Left side: Simulation Pane */}
                    <div className="left-panel">
                        <div className="panel-card">
                            <div className="panel-header">
                                <h2 className="page-description">Simulate actions without actually affecting the repositories!</h2>
                            </div>
                        </div>
                            <section className="command-mode-section">
                                <div className="repository-actions">
                                    <Checkbox
                                        label={"Select all repositories"}
                                        onBoxChecked={setIsSelectingAllRepositories}
                                    />
                                    {!isSelectingAllRepositories && (
                                        <button className="add-repo-button" onClick={addSimulationEntryRow}>
                                            + Add Simulation Case
                                        </button>
                                    )}
                                </div>
                            </section>

                            <section className="custom-commands-section">
                                {simulationEntries.map(row => (
                                    <SimulationEntryRow
                                        key={row.id}
                                        id={row.id}
                                        repositoryId={row.repositoryId}
                                        onDelete={deleteSimulationEntry}
                                        onUpdate={updateSimulateEntryRow}
                                    />
                                ))}
                            </section>
                            <div className="run-batch-section">
                                <button
                                    className="run-batch-button"
                                    onClick={handleSimulationDryRun}
                                >
                                    Simulate
                                </button>
                            </div>
                    </div>

                    {/*The simulated dry run section*/}
                    <div className="right-panel">
                        <section className="output-section">
                            <div className="output-header">
                                <h3>Simulation Run Results</h3>
                                <button
                                    className="clear-output-button"
                                    onClick={cleanUpSimulationResults}
                                >
                                    Clear Output
                                </button>
                            </div>
                            <div className="output-window">
                                {
                                    simulatedPullResults.length === 0 &&
                                    simulatedAddBranchResults.length === 0 &&
                                    simulatedMergeResults.length === 0 &&
                                    <p className="no-output-text">No results to report.</p>
                                }
                                {simulatedPullResults.map((result, index) => (
                                    <div
                                        key={index}
                                        className={`output-entry ${result.isSuccessful ? "success" : "failure"}`}
                                    >
                                        <div className="output-header-row">
                                            <strong>{result.repositoryName}: {result.isSuccessful ? `Safe to pull ${result.branchName}!`: `'git pull' would fail for ${result.branchName}!`}</strong>
                                            <span className="status-icon" />
                                        </div>
                                        {result.commitsToPull?.map((entry, i) => (
                                            <span
                                                key={i}
                                                className="output-command"
                                            >
                                                        &gt; {entry.commitHash} - {entry.message}
                                                    </span>
                                        ))}
                                        <span className="output-stdout">{result.errorMessage}</span>
                                    </div>
                                ))}
                                {simulatedAddBranchResults.map((result, index) => (
                                    <div
                                        key={index}
                                        className={`output-entry ${result.isSuccessful ? "success" : "failure"}`}
                                    >
                                        <div className="output-header-row">
                                            <strong>{result.repositoryName}: {result.isSuccessful ? "Safe to add branch!": "Add branch operation would fail!"}</strong>
                                            <span className="status-icon" />
                                        </div>
                                        <span className="output-command">&gt; {result.infoMessage ? result.infoMessage : "Branch Naming Conflict"}</span>
                                        <span className="output-stdout">{result.errorMessage}</span>
                                    </div>
                                ))}
                                {simulatedMergeResults.map((result, index) => (
                                    <div
                                        key={index}
                                        className={`output-entry ${result.isSuccessful ? "success" : "failure"}`}
                                    >
                                        <div className="output-header-row">
                                            <strong>{result.repositoryName}: {result.mergeStatus}</strong>
                                            <span className="status-icon" />
                                        </div>
                                        <span className="output-stdout">{result.errorMessage}</span>
                                    </div>
                                ))}
                            </div>
                        </section>
                    </div>
                </div>
            </div>
        </>
    );
};

export default MultiDryRunSimulationTab;
