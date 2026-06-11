import { useEffect } from 'react';

/**
 * Custom hook to dynamically update the browser tab title.
 * @param title The title to set the page to.
 * @param retainOnUnmount Flag indicating whether to retain title on unmount.
 */
export const useDocumentTitle = (title: string, retainOnUnmount: boolean = false): void => {
    const defaultTitle = "Emryce";
    useEffect(() => {
        document.title = title ? `${title} | ${defaultTitle}` : defaultTitle;
        return () => {
            if (!retainOnUnmount) {
                document.title = defaultTitle;
            }
        };
    }, [title, retainOnUnmount]);
};