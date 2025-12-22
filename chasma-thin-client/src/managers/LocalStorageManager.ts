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
