import {
    IgnoreRepositoryRequest,
    RepositoryConfigurationClient,
} from "../../API/ChasmaWebApiClient";
import React, {useEffect} from "react";
import {getUserId} from "../../managers/LocalStorageManager";
import {apiBaseUrl} from "../../environmentConstants";

/** The repository configuration client for the web API. **/
const configClient = new RepositoryConfigurationClient(apiBaseUrl)

/**
 * The members of the delete branch modal.
 */
interface IIncludeRepositoryModalProps {
    /** Function to call when the modal is being closed. **/
    onClose: () => void,

    /** Function to call when the repositories are updated. **/
    onRepositoriesUpdated: () => void
}

/**
 * Initializes a new IncludeRepositoryModal class.
 * @param props The properties to include repositories.
 * @constructor
 */
const IncludeRepositoryModal: React.FC<IIncludeRepositoryModalProps> = (props: IIncludeRepositoryModalProps) => {
    /** Gets or sets the error message. **/
    const [errorMessage, setErrorMessage] = React.useState<string | undefined>(undefined);

    /** Gets or sets the repositories to include. **/
    const [ignoredRepositoryList, setIgnoredRepositoryList] = React.useState<(string[] | undefined)>([]);

    /** Gets or sets the repository name.
     * Note: Each element will be in the format: repo name: repo identifier
     **/
    const [repositoryFullId, setRepositoryFullId] = React.useState<string>();

    /** Fetches the ignored repositories associated with this repository. **/
    async function fetchIgnoredRepositories() {
        try {
            const userId = getUserId();
            const message = await configClient.getIgnoredRepositories(userId);
            setIgnoredRepositoryList(message.ignoredRepositories);
            if (message.ignoredRepositories && message.ignoredRepositories.length > 0) {
                setRepositoryFullId(message.ignoredRepositories[0]);
            }
        } catch (e) {
            console.error(e);
            setErrorMessage("Error occurred while fetching branches. Check console logs.");
        }
    }

    /**
     * Handles the event when the user requests to include a repository for managing.
     */
    const handleIncludeRepositoryAction = async () => {
        const repoParts = repositoryFullId?.split(":");
        if (!repoParts) {
            setErrorMessage("Could not include repository. Could not get repository information.");
            return;
        }

        const repoId = repoParts[1];
        try {
            const userId = getUserId();
            const request = new IgnoreRepositoryRequest();
            request.userId = userId;
            request.repositoryId = repoId;
            request.isIgnored = false;
            const response = await configClient.ignoreRepository(request);
            if (response.isErrorResponse) {
                setErrorMessage(response.errorMessage)
                return;
            }

            const repositoryList: string[] = response.includedRepositories
                ? response.includedRepositories.map(i => `${i.name}:${i.id}`)
                : []
            setIgnoredRepositoryList(repositoryList);
            props.onRepositoriesUpdated();
        } catch (e) {
            setErrorMessage("Error including repository. Check console logs.");
            console.error(`Could not include repositories on the filesystem: ${e}`);
        }
        finally {
            await fetchIgnoredRepositories()
        }
    };

    useEffect(() => {
        fetchIgnoredRepositories().catch(e => console.error(e));
    }, []);

    return (
        <>
            <div className="modal-backdrop" onClick={props.onClose}>
                <div className="commit-modal" onClick={(e) => e.stopPropagation()}>
                    <div className="commit-modal-icon">
                        {!errorMessage && (
                            <svg
                                xmlns="http://www.w3.org/2000/svg"
                                viewBox="0 0 24 24"
                                width="48"
                                height="48"
                                fill="none"
                            >
                                <circle cx="12" cy="12" r="10" fill="#00bfff"/>
                                <rect x="11" y="10" width="2" height="7" fill="#ffffff"/>
                                <rect x="11" y="7" width="2" height="2" fill="#ffffff"/>
                            </svg>
                        )}
                        {errorMessage && (
                            <svg
                                xmlns="http://www.w3.org/2000/svg"
                                viewBox="0 0 24 24"
                                width="48"
                                height="48"
                                fill="none"
                            >
                                <circle cx="12" cy="12" r="10" fill="#ff4c4c"/>
                                <rect x="11" y="6" width="2" height="8" fill="#fff"/>
                                <rect x="11" y="16" width="2" height="2" fill="#fff"/>
                            </svg>
                        )}
                    </div>
                    <h2 className="commit-modal-title"
                        style={{marginTop: "-30px"}}>Select a repository to include:</h2>
                    {errorMessage && <h3 className="commit-modal-message">{errorMessage}</h3>}
                    {ignoredRepositoryList && ignoredRepositoryList.length > 0 && (
                        <select value={repositoryFullId}
                                onChange={(e) => setRepositoryFullId(e.target.value)}
                                className="input-field"
                                style={{width: "-webkit-fill-available"}}
                        >
                            {ignoredRepositoryList.map((fullId) => (
                                <option key={fullId} value={fullId}>{fullId}</option>
                            ))}
                        </select>
                    )}
                    <br/>
                    <div>
                        <button className="commit-modal-button"
                                style={{marginRight: "50px"}}
                                onClick={handleIncludeRepositoryAction}
                        >
                            Include
                        </button>
                        <button className="commit-modal-button"
                                onClick={props.onClose}
                        >
                            Close
                        </button>
                    </div>
                </div>
            </div>
        </>
    )
}

export default IncludeRepositoryModal;