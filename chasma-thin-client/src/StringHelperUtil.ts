/**
 * Function that determines if a string is null, undefined, or empty.
 * @param value The string to evaluate.
 */
export const isBlankOrUndefined = (value: string) => {
    return value === null
        || value === undefined
        || value.length === 0
        || value === "";
};