/**
 * Function that determines if a string is null, undefined, or empty.
 * @param value The string to evaluate.
 */
export const isBlankOrUndefined = (value: string | undefined | null) => {
    return value === null
        || value === undefined
        || value.length === 0
        || value === "";
};

/**
 * Copies the text to the clipboard.
 * @param text The text to copy.
 */
export async function copyToClipboard(text: string): Promise<boolean>  {
    try {
        await navigator.clipboard.writeText(text);
        console.log("Successfully copied to clipboard");
        return true;
    }
    catch (e) {
        console.error('Failed to copy text from clipboard', e);
        return false;
    }
}

/**
 * Capitalizes the first letter in a string.
 * @param str The string to capitalize.
 */
export function capitalizeFirst(str: string | undefined): string {
    if (!str) return "";
    if (str.length === 1) return str.charAt(0).toUpperCase();
    return str.charAt(0).toUpperCase() + str.slice(1);
}