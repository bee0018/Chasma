import { useState } from "react";
import { useCacheStore } from "../../managers/CacheManager";
import { useNavigate } from "react-router-dom";
import { LocalGitRepository } from "../../API/ChasmaWebApiClient";

/** The properties to handle commit messages. **/
interface IRepositoryStatusSwitcher {
    /** The confirmation action of the close function. **/
    onClose: () => void;

    /** The action to invoke when switching repositories. */
    onSwitch: () => void;
}

/**
 * Initializes a new RepositoryStatusSwitcher component.
 * @param props The properties to handle switching repository status views.
 * @constructor
 */
const RepositoryStatusSwitcher: React.FC<IRepositoryStatusSwitcher> = (props: IRepositoryStatusSwitcher) => {
    /** Gets or sets the repository to switch to. */
    const [selectedRepository, setSelectedRepository] = useState<LocalGitRepository | undefined>(undefined);

    /** Gets or sets the error message. **/
    const [errorMessage, setErrorMessage] = useState<string | undefined>(undefined);

    /** The cached repositories belonging to the logged-in user. **/
    const repositories = useCacheStore((state) => state.repositories);

    /** Gets the function to navigate to different pages. */
    const navigate = useNavigate();

    /**
     * Handles the event when the user switches repositories.
     * @param repoId The repository identifier.
     */
    const handleRepoSwitch = (repoId: string) => {
        const repository = repositories.find(i => i.id === repoId);
        if (!repository && repoId !== "") {
            setErrorMessage("Cannot switch to this repository because it was not found in system cache.");
            return;
        }

        setSelectedRepository(repository);
        setErrorMessage(undefined);
    };

    /**
     * Handles the event when the user wants to switch to another repository status page.
     */
    const handleSwitchRepository = () => {
        if (!selectedRepository) {
            setErrorMessage("Cannot switch to this repository because there is no valid repository selected.");
            return;
        }

        navigate(`/status/${selectedRepository?.name}/${selectedRepository?.id}`);
        props.onSwitch();
    };

    return (
        <>
            <div className="modal-backdrop" onClick={props.onClose}>
                <div className="modal" onClick={(e) => e.stopPropagation()}>
                    <div className="modal-icon-container">
                        {!errorMessage && <svg
                            xmlns="http://www.w3.org/2000/svg"
                            viewBox="0 0 24 24"
                            width="48"
                            height="48"
                            fill="none"
                        >
                            <circle cx="12" cy="12" r="10" fill="#00bfff" />
                            <rect x="11" y="10" width="2" height="7" fill="#ffffff" />
                            <rect x="11" y="7" width="2" height="2" fill="#ffffff" />
                        </svg>
                        }
                        {errorMessage && (
                            <svg
                                xmlns="http://www.w3.org/2000/svg"
                                viewBox="0 0 24 24"
                                width="48"
                                height="48"
                                fill="none"
                            >
                                <circle cx="12" cy="12" r="10" fill="#ff4c4c" />
                                <rect x="11" y="6" width="2" height="8" fill="#fff" />
                                <rect x="11" y="16" width="2" height="2" fill="#fff" />
                            </svg>
                        )}
                    </div>
                    <h2 className="modal-title">Switch to Status Page: {selectedRepository ? (selectedRepository.displayName ? selectedRepository.displayName : selectedRepository.name) : ""}</h2>
                    {errorMessage && <h3 className="modal-message">{errorMessage}</h3>}
                    <select
                        className="repo-dropdown modern-input"
                        onChange={(e) => handleRepoSwitch(e.target.value)}
                    >
                        <option value="">Select Repository</option>
                        {repositories?.map(repo => (
                            <option key={repo.id} value={repo.id}>
                                {repo.displayName ? repo.displayName : repo.name}
                            </option>
                        ))}
                    </select>
                    <br />
                    <div className="modal-actions">
                        <button
                            className="modal-button primary"
                            onClick={handleSwitchRepository}
                        >
                            Switch
                        </button>
                        <button
                            className="modal-button secondary"
                            onClick={props.onClose}
                        >
                            Close
                        </button>
                    </div>
                </div>
            </div>
        </>
    );
}

export default RepositoryStatusSwitcher;