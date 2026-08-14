import { useEffect } from "react";

/**
 * Executes a callback when a specific keyboard key is pressed.
 * 
 * @param key The keyboard key to listen for (e.g., "Escape", "Enter")
 * @param callback The function to execute on keypress
 */
export const useKeyPress = (key: string, callback: () => void): void => {
    useEffect(() => {
        const handler = (e: KeyboardEvent) => {
            if (e.key === key) {
                e.preventDefault();
                e.stopPropagation();
                callback();
            }
        };

        window.addEventListener("keydown", handler);
        return () => window.removeEventListener("keydown", handler);
    }, [key, callback]);
};

/**
 * Executes a callback when multiple specific keys are pressed.
 * 
 * @param predicate The boolean predicated to evaluate.
 * @param callback The function to execute on keypress
 */
export const useKeyPresses = (predicate: (e: KeyboardEvent) => boolean, callback: () => void): void => {
    useEffect(() => {
        const handler = (e: KeyboardEvent) => {
            if (predicate(e)) {
                e.preventDefault();
                e.stopPropagation();
                callback();
            }
        };

        window.addEventListener("keydown", handler);
        return () => window.removeEventListener("keydown", handler);
    }, [predicate, callback]);
};