import {
    ApplicationConfigurationClient,
    BranchClient,
    DryRunClient,
    HealthClient,
    ProxyClient,
    RemoteClient,
    RepositoryConfigurationClient,
    RepositoryStatusClient,
    ShellClient,
    StashClient,
    UserClient
} from "../API/ChasmaWebApiClient";
import { apiBaseUrl } from "../environmentConstants";
import { useCacheStore } from "./CacheManager";

// High timeout for long-running operations (e.g., 5 minutes for heavy operations)
const TIMEOUT_MS = 15 * 60 * 1000;

/** Gets the fetch operation to get data with authorization headers. */
const fetchWithAuth: typeof window.fetch = (input, init) => {
    const token = useCacheStore.getState().token;
    const headers = new Headers(init?.headers);
    if (token) {
        headers.set("Authorization", `Bearer ${token}`);
    }

    const controller = new AbortController();
    const timeout = setTimeout(() => {
        controller.abort(new Error(`Request timed out after ${TIMEOUT_MS}ms`));
        useCacheStore.getState().setNotification({
            title: "Failed to complete request.",
            message: "The request has timed out. Review server logs for more information.",
            isError: true,
        });
    }, TIMEOUT_MS);

    if (init?.signal) {
        if (init.signal.aborted) {
            controller.abort(init.signal.reason);
        } else {
            init.signal.addEventListener("abort", () => {
                controller.abort(init.signal?.reason);
            },{ once: true });
        }
    }

    return window.fetch(input, { ...init, headers, signal: controller.signal })
        .finally(() => {
            clearTimeout(timeout);
        });
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

/** The application configuration client for the web API. */
export const appConfigClient = new ApplicationConfigurationClient(apiBaseUrl, { fetch: fetchWithAuth });

/** The Proxy worker client for the web API. */
export const proxyClient = new ProxyClient(apiBaseUrl, { fetch: fetchWithAuth });