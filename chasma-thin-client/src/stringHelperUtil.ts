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
 * @note This will format the string by removing all underscores from the input string.
 * @param str The string to capitalize.
 */
export function capitalizeFirst(str: string | undefined): string {
    if (!str) return "";
    if (str.length === 1) return str.charAt(0).toUpperCase();
    str = str.replaceAll("_", " ");
    return str.charAt(0).toUpperCase() + str.slice(1);
}

/**
 * Determines in the string input is a whole number.
 * @param value The number input.
 * @returns True if the input is a whole number; false otherwise.
 */
export function isWholeNumber(value: string): boolean {
  return /^-?\d+$/.test(value.trim());
}