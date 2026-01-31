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

/** The object representing the logged-in user. **/
export type User = {
    userId: number | undefined;
    username: string | undefined;
    email: string | undefined;
}

/** The toggle options for selecting commands to execute uniformly or custom. **/
export type CommandMode = "uniform" | "custom";

/** The git command row used in the help page. **/
export type GitCommand = {
    command: string;
    description: string;
}