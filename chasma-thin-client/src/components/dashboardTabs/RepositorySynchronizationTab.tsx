import React, { useState } from 'react';
import { SyncStepStatus } from '../types/CustomTypes';
import { useCacheStore } from '../../managers/CacheManager';
import { useDocumentTitle } from '../../util/useDocumentTitle';
import { LocalGitRepository } from '../../API/ChasmaWebApiClient';
import ProgressBar from '../application/ProgressBar';

// Simulated API Calls to your C# Backend
const api = {
    runPreflight: async (path: string) => {
        await new Promise(r => setTimeout(r, 1200)); // Simulate network lag
        return { success: true, message: "Safe to sync. No merge conflicts." };
    },
    runPull: async (path: string) => {
        await new Promise(r => setTimeout(r, 1800));
        return { success: true, message: "Pulled 3 commits. NuGet packages restored." };
    },
    runPush: async (path: string) => {
        await new Promise(r => setTimeout(r, 1000));
        return { success: true, message: "Pushed 1 ahead commit to origin." };
    }
};

/** The repository synchronization state. */
interface RepoSyncState {
    repository: LocalGitRepository
    preflightStatus: SyncStepStatus;
    preflightDetails: string;
    pullStatus: SyncStepStatus;
    pullDetails: string;
    pushStatus: SyncStepStatus;
    pushDetails: string;
}


export interface SyncTarget {
    id: string;
    name: string;
    path: string;
}

/**
 * Initializes a new instance of the RepositorySynchronizationTab component.
 * @constructor
 */
const RepositorySynchronizationTab: React.FC = () => {
    useDocumentTitle("Smart Sync");

    /** Gets the counter for the number of repositories that have been processed by the engine. */
    const [repoSyncCounter, setRepoSyncCounter] = useState<number>(0);

    /** Gets the repositories that are cached in the system. */
    const repositories = useCacheStore(state => state.repositories);

    /** Gets or sets a value indicating whether the repository divergence has been sent. */
    const [divergenceRequestHasBeenSent, setDivergenceRequestHasBeenSent] = useState<boolean>(false);

    /** Gets or sets the repository synchronization steps. */
    const [syncStates, setSyncStates] = useState<RepoSyncState[]>(() => {
        return repositories.map(i => {
            const repoSyncState: RepoSyncState = {
                repository: i,
                preflightStatus: 'idle',
                preflightDetails: '-',
                pullStatus: 'idle',
                pullDetails: '-',
                pushStatus: 'idle',
                pushDetails: '-'
            };
            return repoSyncState;
        });
    });

    /** Gets or sets a value indicating whether the user is syncing repositories. */
    const [isSyncing, setIsSyncing] = useState(false);

    /** The number signifying the percentage of completeness of synchronization. */
    const progressPercent = Math.round((repoSyncCounter / repositories.length) * 100);

    /**
     * Executes the smart synchronization pipeline.
     */
    const executeSmartSync = async () => {
        setIsSyncing(true);

        // Mock targets containing paths required by the backend
        const targets: SyncTarget[] = [
            { id: repositories[0].id!, name: 'chasma-web-api', path: 'C:/repos/chasma-web-api' },
            { id: repositories[1].id!, name: 'chasma-frontend', path: 'C:/repos/chasma-frontend' }
        ];

        // Map each target to its own isolated, running execution promise
        const syncPromises = targets.map(async (target) => {
            // --- PHASE 1: PREFLIGHT ---
            updateRepoStep(target.id, 'preflightStatus', 'running', 'Evaluating tracking branches...');
            try {
                const preflight = await api.runPreflight(target.path);
                updateRepoStep(target.id, 'preflightStatus', 'success', preflight.message);
            } catch (err) {
                updateRepoStep(target.id, 'preflightStatus', 'failed', 'Preflight check failed.');
                return; // Abort remaining steps for this specific repo
            }

            // --- PHASE 2: PULL RESULTS ---
            updateRepoStep(target.id, 'pullStatus', 'running', 'Executing pull and manifest verification...');
            try {
                const pull = await api.runPull(target.path);
                updateRepoStep(target.id, 'pullStatus', 'success', pull.message);
            } catch (err) {
                updateRepoStep(target.id, 'pullStatus', 'failed', 'Pull execution aborted.');
                return;
            }

            // --- PHASE 3: PUSH RESULTS ---
            updateRepoStep(target.id, 'pushStatus', 'running', 'Pushing staging commits upstream...');
            try {
                const push = await api.runPush(target.path);
                updateRepoStep(target.id, 'pushStatus', 'success', push.message);
            } catch (err) {
                updateRepoStep(target.id, 'pushStatus', 'failed', 'Push execution failed.');
            }
        });

        // Run all repository operational sequences in parallel
        const count = repoSyncCounter + 1;
        setRepoSyncCounter(count);

        await Promise.all(syncPromises);
        setIsSyncing(false);
    };

    /**
     * Updates the repository synchonization step with the run details.
     * @param repoId The repository identifier.
     * @param stepKey The sync step key.
     * @param status  The status of the synchronization step.
     * @param details The details of synchronization step.
     */
    const updateRepoStep = (
        repoId: string,
        stepKey: 'preflightStatus' | 'pullStatus' | 'pushStatus',
        status: SyncStepStatus,
        details: string
    ) => {
        setSyncStates(prev => prev.map(entry => {
            if (entry.repository.id !== repoId) {
                return entry;
            }

            const detailsKey = stepKey === 'preflightStatus' ? 'preflightDetails' :
                stepKey === 'pullStatus' ? 'pullDetails' : 'pushDetails';
            return {
                ...entry,
                [stepKey]: status,
                [detailsKey]: details
            };
        }));
    };

    /**
     * Gets the status badge of the sync step.
     * @param status The status of the synchronization step.
     * @returns The status badge of the sync step.
     */
    const getStatusBadge = (status: SyncStepStatus) => {
        switch (status) {
            case 'running': return <span style={{ color: '#22d3ee', fontWeight: 'bold' }}>🔄 Running</span>;
            case 'success': return <span style={{ color: '#4ade80' }}>✅ Success</span>;
            case 'failed': return <span style={{ color: '#f87171' }}>❌ Failed</span>;
            default: return <span style={{ color: '#6b7280' }}>💤 Idle</span>;
        }
    };

    return (
        <div className='sync-workspace-background'>
            <button
                onClick={executeSmartSync}
                disabled={isSyncing}
                className={isSyncing ? 'pipeline-trigger-button-is-syncing' : 'pipeline-trigger-button-not-syncing'}
            >
                {isSyncing ? 'Synchronizing Pipeline...' : 'Trigger Smart Sync'}
            </button>
            <br />
            <ProgressBar
                progressPercent={progressPercent}
                unfinishedMessage={`${repoSyncCounter} out of ${repositories.length} have been synchronized`}
                finishedMessage='Synchronization complete!'
                displayNonErrorProgressBar={true} />
            <hr className='status-separator' />
            {syncStates.map((syncState, index) => (
                <div key={syncState.repository.id}>
                    <h3 className='repo-sync-state-header'>📦 {syncState.repository.displayName ? syncState.repository.displayName : syncState.repository.name}</h3>
                    <table className='sync-state-table'>
                        <thead>
                            <tr>
                                <th>Operation Step</th>
                                <th>Execution State</th>
                                <th>Execution Details / Step Output</th>
                            </tr>
                        </thead>
                        <tbody>
                            <tr>
                                <td className='sync-state-table-operation-step-title'>Preflight Checks</td>
                                <td className='sync-state-table-operation-step-status'>{getStatusBadge(syncState.preflightStatus)}</td>
                                <td className='sync-state-table-operation-step-details'>{syncState.preflightDetails}</td>
                            </tr>
                            <tr>
                                <td className='sync-state-table-operation-step-title'>Pull Changes</td>
                                <td className='sync-state-table-operation-step-status'>{getStatusBadge(syncState.pullStatus)}</td>
                                <td className='sync-state-table-operation-step-details'>{syncState.pullDetails}</td>
                            </tr>
                            <tr>
                                <td className='sync-state-table-operation-step-title'>Push Changes</td>
                                <td className='sync-state-table-operation-step-status'>{getStatusBadge(syncState.pushStatus)}</td>
                                <td className='sync-state-table-operation-step-details'>{syncState.pushDetails}</td>
                            </tr>
                        </tbody>
                    </table>

                    {index < syncStates.length - 1 && (
                        <hr className='status-separator' />
                    )}
                </div>
            ))}
        </div>
    );
};

export default RepositorySynchronizationTab;