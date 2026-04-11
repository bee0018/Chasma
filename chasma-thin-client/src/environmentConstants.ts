/** The Web API base URL. **/
export const apiBaseUrl = process.env.NODE_ENV === "development"
    ? "http://localhost:5000"
    : window.location.origin;