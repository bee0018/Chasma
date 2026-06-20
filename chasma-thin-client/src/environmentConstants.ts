/** The Web API base URL. **/
export const apiBaseUrl = process.env.NODE_ENV === "development"
    ? "https://localhost:7200"
    : window.location.origin;