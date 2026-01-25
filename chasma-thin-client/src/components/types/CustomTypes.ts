/** Row representing an editable mapping. **/
export type Row = {
    id: string;
    first: string;
    second: string;
};

/** The object representation of a diffed line. */
export type DiffLine = {
    type: "add" | "remove" | "context" | "hunk";
    content: string;
    oldLineNumber?: number;
    newLineNumber?: number;
};
