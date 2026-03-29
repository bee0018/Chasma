import { create } from "zustand";
import {ApplicationUser, LocalGitRepository} from "../API/ChasmaWebApiClient";
import {persist} from 'zustand/middleware'

interface Notification {
    title: string;
    message?: string;
    isError?: boolean;
    loading?: boolean;
}

/** Interface defining the members of the cache state. **/
interface CacheState {
    /** The logged-in user. **/
    user: ApplicationUser | null;

    /** The user's repositories. **/
    repositories: LocalGitRepository[];

    token: string | undefined;

    notification: Notification | null;

    /** Sets the logged-in user. **/
    setUser: (user: ApplicationUser | undefined) => void;

    /** Sets the user's repositories. **/
    setRepositories: (repos: LocalGitRepository[] | undefined) => void;

    /** Sets the authenticated token. */
    setToken: (token: string | undefined) => void;

    setNotification: (notification: Notification | null) => void;
    clearNotification: () => void;

    /** Deletes the repository with the specified repository identifier. **/
    deleteRepository: (repoId: string | undefined) => void;

    /** Adds a local git repository to the cache. **/
    addLocalGitRepository: (repo: LocalGitRepository) => void;

    /** Clears the cache. **/
    clearCache: () => void;
}

/** The cache store.
 * Note: This implementation will persist page refreshes.
 **/
export const useCacheStore = create<CacheState>()(
    persist(
        (set) => ({
            user: null,
            repositories: [],
            token: undefined,
            notification: null,
            setUser: (user) => set({ user }),
            setRepositories: (repositories) => set({ repositories }),
            setToken: (token) => set({ token }),
            setNotification: (notification) => set({ notification }),
            clearNotification: () => set({ notification: null }),
            deleteRepository: (repoId: string | undefined) => set((state) => ({
                repositories: [...state.repositories.filter(i => i.id !== repoId)],
            })),
            addLocalGitRepository: (repo) =>
                set((state) => ({
                    repositories: [...state.repositories, repo],
                })),
            clearCache: () => set({ user: null, repositories: [], token: undefined, notification: null }),
        }),
        {
            name: "cache-store",
        }
    )
);
