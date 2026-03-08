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

/** Gets the user management client that interfaces with the web API. **/
export const userClient = new UserClient(apiBaseUrl);

/** Gets the dry run client interfacing with the web API. **/
export const dryRunClient = new DryRunClient(apiBaseUrl);

/** The health client interacting with the web API. **/
export const healthClient = new HealthClient(apiBaseUrl);

/** The remote repository management client for the web API. **/
export const remoteClient = new RemoteClient(apiBaseUrl);

/** The repository configuration client for the web API. **/
export const configClient = new RepositoryConfigurationClient(apiBaseUrl);

/** The repository status client for the web API. **/
export const statusClient = new RepositoryStatusClient(apiBaseUrl);

/** The shell client used to interact with the API. **/
export const shellClient = new ShellClient(apiBaseUrl);

/** The repository stashing client for the web API. **/
export const stashClient = new StashClient(apiBaseUrl);

/** The branch management client for the web API. **/
export const branchClient = new BranchClient(apiBaseUrl);