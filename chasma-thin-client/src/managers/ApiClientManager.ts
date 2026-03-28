import {
    BranchClient,
    DryRunClient,
    HealthClient,
    RemoteClient,
    RepositoryConfigurationClient,
    RepositoryStatusClient,
    ShellClient,
    StashClient,
    UserClient
} from "../API/ChasmaWebApiClient";
import {apiBaseUrl} from "../environmentConstants";
import { useCacheStore } from "./CacheManager";

/** Gets the fetch operation to get data with authorization headers. */
const fetchWithAuth: typeof window.fetch = (input, init) => {
    const token = useCacheStore.getState().token;
    const headers = new Headers(init?.headers);
    if (token) {
        headers.set("Authorization", `Bearer ${token}`);
    }

    return window.fetch(input, { ...init, headers });
};

/** Gets the user management client that interfaces with the web API. **/
export const userClient = new UserClient(apiBaseUrl, { fetch: fetchWithAuth });

/** Gets the dry run client interfacing with the web API. **/
export const dryRunClient = new DryRunClient(apiBaseUrl, { fetch: fetchWithAuth });

/** The health client interacting with the web API. **/
export const healthClient = new HealthClient(apiBaseUrl, { fetch: fetchWithAuth });

/** The remote repository management client for the web API. **/
export const remoteClient = new RemoteClient(apiBaseUrl, { fetch: fetchWithAuth });

/** The repository configuration client for the web API. **/
export const configClient = new RepositoryConfigurationClient(apiBaseUrl, { fetch: fetchWithAuth });

/** The repository status client for the web API. **/
export const statusClient = new RepositoryStatusClient(apiBaseUrl, { fetch: fetchWithAuth });

/** The shell client used to interact with the API. **/
export const shellClient = new ShellClient(apiBaseUrl, { fetch: fetchWithAuth });

/** The repository stashing client for the web API. **/
export const stashClient = new StashClient(apiBaseUrl, { fetch: fetchWithAuth });

/** The branch management client for the web API. **/
export const branchClient = new BranchClient(apiBaseUrl, { fetch: fetchWithAuth });