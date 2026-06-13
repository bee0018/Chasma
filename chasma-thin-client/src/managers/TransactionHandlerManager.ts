import { NavigateFunction } from "react-router-dom";
import { ApiException, HeartbeatStatus } from "../API/ChasmaWebApiClient";
import { useCacheStore } from "./CacheManager";
import { healthClient } from "./ApiClientManager";

/**
 * Handles the exception from the backend API.
 * @param error The API exception error.
 * @param navigate The React navigate function.
 * @param title The error modal title.
 * @param errorMessage The API error message.
 * @returns 
 */
export async function handleApiError(error: unknown, navigate: NavigateFunction, title?: string, errorMessage?: string) {
    const heartbeatReceived = await getServerHeartbeat();
    if (!heartbeatReceived) {
        useCacheStore.getState().clearCache();
        return {
            title: "Server is not running!",
            message: "Please restart application.",
            isError: true
        };
    }

    console.error(error);
    if (error instanceof ApiException) {
        if (error.status === 401) {
            // Unauthorized handling
            useCacheStore.getState().clearCache();
            navigate("/");
            return {
                title: "Access is unauthorized or session is expired.",
                message: "Please log in again.",
                isError: true
            };
        }

        return {
            title: "API Error",
            message: error.message,
            isError: true
        };
    }

    if (error instanceof Error) {
        return {
            title: "Error",
            message: error.message,
            isError: true
        };
    }

    return {
        title: title !== undefined ? title : "Unknown Error",
        message: errorMessage !== undefined ? errorMessage : "Something went wrong.",
        isError: true
    };
}

/**
 * Gets the heartbeat from the server.
 * @returns True if heartbeat with status 'Ok'; false otherwise.
 */
const getServerHeartbeat = async () => {
    try {
        const response = await healthClient.getHeartbeat();
        if (response.status === HeartbeatStatus.Ok) {
            return true;
        }

        console.error("Server is in Error state. Please review server logs and/or restart application.")
        return false;
    } catch (error) {
        console.error("Server is not running and cannot process requests. Please restart application.");
        return false;
    }
};