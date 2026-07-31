import { create } from "zustand";
import { ApplicationUser, LocalGitRepository, SystemManifest, WorkContextSnapshot } from "../API/ChasmaWebApiClient";
import { persist } from 'zustand/middleware'

/**
 * Defines the system notification components to display.
 */
interface Notification {
    /** The title of the notificiation. */
    title: string;

    /** The message of the notification. */
    message?: string;

    /** Flag indicating whether the notification is an error message. */
    isError?: boolean;

    /** Flag indicating whether the notification is in a loading state. */
    loading?: boolean;
}

/** Interface defining the members of the cache state. **/
interface CacheState {
    /** The logged-in user. **/
    user: ApplicationUser | null;

    /** The user's repositories. **/
    repositories: LocalGitRepository[];

    /** The user access token. */
    token: string | undefined;

    /** The user's refresh token. */
    refreshToken: string | undefined;

    /** The notification to display on the application. */
    notification: Notification | null;

    /** The user's workspace snapshots. */
    workspaceSnapshots: WorkContextSnapshot[];

    /** The new system update if available. */
    newSystemUpdate: SystemManifest | undefined;

    /** Sets the logged-in user. **/
    setUser: (user: ApplicationUser | undefined) => void;

    /** Sets the user's repositories. **/
    setRepositories: (repos: LocalGitRepository[] | undefined) => void;

    /** Sets the authenticated token. */
    setToken: (token: string | undefined) => void;

    /** Sets the refresh token. */
    setRefreshToken: (refreshToken: string | undefined) => void;

    /** Sets the notification of the app. */
    setNotification: (notification: Notification | null) => void;

    /** Sets the user's workspace snapshots. **/
    setWorkspaceSnapshots: (repos: WorkContextSnapshot[] | undefined) => void;

    /** Sets the new system update. */
    setNewSystemUpdate: (newSystemUpdate: SystemManifest | undefined) => void;

    /** Dismisses the notificaiton from the app. */
    clearNotification: () => void;

    /** Deletes the repository with the specified repository identifier. **/
    deleteRepository: (repoId: string | undefined) => void;

    /** Deletes the workspace snapshot with the specified snapshot identifier. */
    deleteSnapshot: (snapshotId: number | undefined) => void;

    /** Adds a local git repository to the cache. **/
    addLocalGitRepository: (repo: LocalGitRepository) => void;

    /** Adds a workspace snapshot to the cache. */
    addWorkspaceSnapshot: (snapshot: WorkContextSnapshot) => void;

    /** Updates a local git repository in the cache. **/
    updateLocalGitRepository: (repo: LocalGitRepository) => void;

    /** Clears the cache. **/
    clearCache: () => void;
}

/** The cache store.
 * Note: This implementation will persist page refreshes.
 **/
export const useCacheStore = create<CacheState>()(
    persist(
        (set) => ({
            /** The logged-in user. */
            user: null,

            /** The repositories belonging to the logged-user. */
            repositories: [],

            /** The user login access token. */
            token: undefined,

            /** The user's refresh token. */
            refreshToken: undefined,

            /** The system notification. */
            notification: null,

            /** The user's workspace snapshots. */
            workspaceSnapshots: [],

            /** The newest system update. */
            newSystemUpdate: undefined,

            /**
             * Sets the logged-in user.
             * @param user The user that is logging in.
             */
            setUser: (user) => set({ user }),

            /**
             * Sets the repositories that belong to the user.
             * @param repositories The registered repositories belonging to a user.
             */
            setRepositories: (repositories) => set({ repositories }),

            /**
             * Sets the user access token.
             * @param token The user access token.
             */
            setToken: (token) => set({ token }),

            /**
             * Sets the user refresh token.
             * @param refreshToken The user refresh token.
             */
            setRefreshToken: (refreshToken) => set({ refreshToken }),

            /**
             * Sets the system notification.
             * @param notification The new user notification.
             */
            setNotification: (notification) => set({ notification }),

            /**
             * Sets the workspace snapshots belong to the logged-in user.
             * @param workspaceSnapshots The workspace snapshots.
             */
            setWorkspaceSnapshots: (workspaceSnapshots) => set({ workspaceSnapshots }),

            /**
             * Sets the new system update provided by the server.
             * @param newSystemUpdate The new system update.
             */
            setNewSystemUpdate: (newSystemUpdate) => set({ newSystemUpdate }),

            /**
             * Clears the system notification.
             */
            clearNotification: () => set({ notification: null }),

            /**
             * Deletes the repository from cache with the specified repository identifier.
             * @param repoId The repository identifier.
             */
            deleteRepository: (repoId: string | undefined) => set((state) => ({
                repositories: [...state.repositories.filter(i => i.id !== repoId)],
            })),

            /**
             * Deletes the workspace snapshot from cache with the specified snapshot identifier.
             * @param snapshotId The snapshot identifier.
             */
            deleteSnapshot: (snapshotId: number | undefined) => set((state) => ({
                workspaceSnapshots: [...state.workspaceSnapshots.filter(i => i.snapshotId !== snapshotId)],
            })),

            /**
             * Adds the new repository to the system cache.
             * @param repo The newly added repository.
             */
            addLocalGitRepository: (repo) =>
                set((state) => ({
                    repositories: [...state.repositories, repo],
                })),

            /**
             * Adds the new workspace snapshot to the system cache.
             * @param snapshot The newly added snapshot.
             */
            addWorkspaceSnapshot: (snapshot) =>
                set((state) => ({
                    workspaceSnapshots: [...state.workspaceSnapshots, snapshot],
                })),
            
            /**
             * Updates the specific repository new repository data.
             * @param updatedRepo The updated repository.
             */
            updateLocalGitRepository: (updatedRepo) =>
                set((state) => ({
                    repositories: state.repositories.map((repo) =>
                        repo.id === updatedRepo.id ? updatedRepo : repo
                    ),
                })),

            /**
             * Clears the system cache.
             */
            clearCache: () => set({
                user: null,
                repositories: [],
                workspaceSnapshots: [],
                token: undefined,
                refreshToken: undefined,
                notification: null,
                newSystemUpdate: undefined,
            }),
        }),
        {
            name: "cache-store",
        }
    )
);
