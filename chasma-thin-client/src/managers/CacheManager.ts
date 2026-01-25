import { create } from "zustand";
import {User} from "../components/types/CustomTypes";
import {LocalGitRepository} from "../API/ChasmaWebApiClient";
import {persist} from 'zustand/middleware'

/** Interface defining the members of the cache state. **/
interface CacheState {
    /** The logged-in user. **/
    user: User | null;

    /** The user's repositories. **/
    repositories: LocalGitRepository[];

    /** Sets the logged-in user. **/
    setUser: (user: User | null) => void;

    /** Sets the user's repositories. **/
    setRepositories: (repos: LocalGitRepository[] | undefined) => void;

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
            setUser: (user) => set({ user }),
            setRepositories: (repositories) => set({ repositories }),
            deleteRepository: (repoId: string | undefined) => set((state) => ({
                repositories: [...state.repositories.filter(i => i.id !== repoId)],
            })),
            addLocalGitRepository: (repo) =>
                set((state) => ({
                    repositories: [...state.repositories, repo],
                })),
            clearCache: () => set({ user: null, repositories: [] }),
        }),
        {
            name: "cache-store",
        }
    )
);
