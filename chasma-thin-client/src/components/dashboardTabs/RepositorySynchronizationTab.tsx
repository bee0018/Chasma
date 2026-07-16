import React, { useState } from 'react';
import { SyncStepStatus } from '../types/CustomTypes';
import { useCacheStore } from '../../managers/CacheManager';
import { useDocumentTitle } from '../../util/useDocumentTitle';
import { BranchCheckoutMode, LocalGitRepository, SynchronizationStep, SynchronizeRepositoryRequest, SynchronizeRepositoryResponse } from '../../API/ChasmaWebApiClient';
import ProgressBar from '../application/ProgressBar';
import { statusClient } from '../../managers/ApiClientManager';
import { handleApiError } from '../../managers/TransactionHandlerManager';
import { useNavigate } from 'react-router-dom';
import SmartSyncConfirmationModal from '../modals/SmartSyncConfirmationModal';

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

    /** Gets the logged-in user. */
    const user = useCacheStore(state => state.user);

    /** The navigation function. **/
    const navigate = useNavigate();

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

    /** Gets or sets a value indicating whether the user is configuring the checkout mode. */
    const [isConfiguringCheckoutMode, setIsConfiguringCheckoutMode] = useState<boolean>(false);

    /** The number signifying the percentage of completeness of synchronization. */
    const progressPercent = Math.round((repoSyncCounter / repositories.length) * 100);

    /** Reverts all sync states back to its initial state. */
    const resetSyncSteps = () => {
        setSyncStates(() => {
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
        })
    }

    /**
     * Executes the smart synchronization pipeline.
     * @param checkoutMode The branch checkout mode.
     */
    const executeSmartSync = async (checkoutMode: BranchCheckoutMode) => {
        setIsSyncing(true);
        setRepoSyncCounter(0);
        resetSyncSteps();
        let completedCount = 0;
        for (const repo of repositories) {
            // --- PHASE 1: PREFLIGHT ---
            updateRepoStep(repo.id, 'preflightStatus', 'running', 'Running pre-flight checks...');
            const preflightResponse = await performPreFlightChecks(repo);
            if (preflightResponse.isErrorResponse) {
                updateRepoStep(repo.id, 'preflightStatus', 'failed', preflightResponse.errorMessage);
                continue;
            }

            updateRepoStep(repo.id, 'preflightStatus', 'success', preflightResponse.syncStepDescription);

            // --- PHASE 2: PULL RESULTS ---
            updateRepoStep(repo.id, 'pullStatus', 'running', 'Executing pull and manifest verification...');
            const pullResponse = await performPullSyncStep(repo, checkoutMode);
            if (pullResponse.isErrorResponse) {
                updateRepoStep(repo.id, 'pullStatus', 'failed', pullResponse.errorMessage);
                continue;
            }

            updateRepoStep(repo.id, 'pullStatus', 'success', pullResponse.syncStepDescription);

            // --- PHASE 3: PUSH RESULTS ---
            updateRepoStep(repo.id, 'pushStatus', 'running', 'Pushing staging commits upstream...');
            const pushResponse = await performPushSyncStep(repo);
            if (pushResponse.isErrorResponse) {
                updateRepoStep(repo.id, 'pushStatus', 'failed', pushResponse.errorMessage);
                continue;
            }
            
            updateRepoStep(repo.id, 'pushStatus', 'success', pushResponse.syncStepDescription);
            completedCount++;
            setRepoSyncCounter(completedCount);
        };

        setIsSyncing(false);
    };

    /**
     * Performs the pre-flight checks in the server API.
     * @param repo The local git repository to run pre-flight checks on.
     * @returns The synchronization response to running pre-flight checks.
     */
    async function performPreFlightChecks(repo: LocalGitRepository, ): Promise<SynchronizeRepositoryResponse> {
        const request = new SynchronizeRepositoryRequest();
        request.userId = user?.userId;
        request.repositoryId = repo.id;
        request.syncStep = SynchronizationStep.PreFlightChecks;
        try {
            return await statusClient.performSynchronizationStep(request);
        } catch (error) {
            await handleApiError(error, navigate, "Error peforming pre-flight checks!", "Check server logs for more information.");
            let response = new SynchronizeRepositoryResponse();
            response.isErrorResponse = true;
            response.errorMessage = "Preflight check failed. Manually review server logs for more information.";
            return response;
        }
    }

    /**
     * Performs the pull and manifest verification changes in the server API.
     * @param repo The local git repository to pull changes in.
     * @param branchCheckoutMode The branch checkout mode.
     * @returns The synchronization response to running pull/manifest verification.
     */
    async function performPullSyncStep(repo: LocalGitRepository, branchCheckoutMode: BranchCheckoutMode): Promise<SynchronizeRepositoryResponse> {
        const request = new SynchronizeRepositoryRequest();
        request.userId = user?.userId;
        request.repositoryId = repo.id;
        request.syncStep = SynchronizationStep.PullChanges;
        request.checkoutMode = branchCheckoutMode;
        try {
            return await statusClient.performSynchronizationStep(request);
        } catch (error) {
            await handleApiError(error, navigate, "Error peforming pull sync step!", "Check server logs for more information.");
            let response = new SynchronizeRepositoryResponse();
            response.isErrorResponse = true;
            response.errorMessage = "Pulling changes failed. Manually review server logs for more information.";
            return response;
        }
    }

    /**
     * Performs the push changes sync step in the server API.
     * @param repo The local git repository to push changes in.
     * @returns The synchronization response to running push verification.
     */
    async function performPushSyncStep(repo: LocalGitRepository): Promise<SynchronizeRepositoryResponse> {
        const request = new SynchronizeRepositoryRequest();
        request.userId = user?.userId;
        request.repositoryId = repo.id;
        request.syncStep = SynchronizationStep.PushChanges;
        try {
            return await statusClient.performSynchronizationStep(request);
        } catch (error) {
            await handleApiError(error, navigate, "Error peforming push sync step!", "Check server logs for more information.");
            let response = new SynchronizeRepositoryResponse();
            response.isErrorResponse = true;
            response.errorMessage = "Pushing changes failed. Manually review server logs for more information.";
            return response;
        }
    }

    /**
     * Updates the repository synchonization step with the run details.
     * @param repoId The repository identifier.
     * @param stepKey The sync step key.
     * @param status  The status of the synchronization step.
     * @param details The details of synchronization step.
     */
    const updateRepoStep = (
        repoId: string | undefined,
        stepKey: 'preflightStatus' | 'pullStatus' | 'pushStatus',
        status: SyncStepStatus,
        details: string | undefined
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

    /**
     * Handles the event when the user selects a checkout mode.
     * @param checkoutMode The branch checkout mode.
     */
    const handleCheckoutModeSelected = async (checkoutMode: BranchCheckoutMode) => {
        setIsConfiguringCheckoutMode(false);
        if (isSyncing) {
            return;
        }

        await executeSmartSync(checkoutMode);
    };

    return (
        <div className='sync-workspace-background'>
            <button
                onClick={() => setIsConfiguringCheckoutMode(true)}
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
            {isConfiguringCheckoutMode &&
                <SmartSyncConfirmationModal
                    onClose={() => setIsConfiguringCheckoutMode(false)}
                    onSelected={(checkoutMode) => handleCheckoutModeSelected(checkoutMode)}
                />
            }
        </div>
    );
};

export default RepositorySynchronizationTab;