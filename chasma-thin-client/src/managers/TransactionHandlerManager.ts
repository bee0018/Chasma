import { NavigateFunction } from "react-router-dom";
import { ApiException } from "../API/ChasmaWebApiClient";
import { useCacheStore } from "./CacheManager";


export function handleApiError(error: unknown, navigate: NavigateFunction, title?: string, errorMessage?: string) {
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