import {LocalGitRepository} from "../API/ChasmaWebApiClient";

/** Gets the userId from local storage. **/
export const getUserId = () => {
    const userIdJson = localStorage.getItem("userId");
    if (!userIdJson) return undefined;
    return Number(userIdJson)
};

/** Gets the username from local storage. **/
export const getUsername = () => {
    const userNameJson = localStorage.getItem("username");
    if (!userNameJson) return undefined;
    return JSON.parse(userNameJson)
};

/** Gets the userId from local storage. **/
export const getUserEmail = () => {
    const emailJson = localStorage.getItem("email");
    if (!emailJson) return undefined;
    return JSON.parse(emailJson)
};

/**
 * Gets the repository with the specified repository identifier.
 * @param repositoryId The repository identifier.
 */
export const getLocalGitRepository = (repositoryId : string | undefined) => {
    const repositoriesJson = localStorage.getItem("gitRepositories");
    if (!repositoriesJson) return undefined;
    const repositories : LocalGitRepository[] = JSON.parse(repositoriesJson) || [];
    return repositories.find((repository) => repository.id === repositoryId) ?? null;
}