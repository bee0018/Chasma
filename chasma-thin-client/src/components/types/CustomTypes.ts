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

/** The toggle options for selecting commands to execute uniformly or custom. **/
export type CommandMode = "uniform" | "custom";

/** The git command row used in the help page. **/
export type GitCommand = {
    command: string;
    description: string;
}

/** The toggle options for git command simulations. **/
export enum GitSimulationCase {
    Select = "Select Simulation",
    Pull = "Pull",
    AddBranch = "Add Branch",
    Merge = "Merge",
}

/** The entry used for testing simulation functionality. **/
export type SimulationEntry = {
    id: string;
    simCase: GitSimulationCase;
    repositoryId?: string;
    branchToPull?: string;
    branchToAdd?: string;
    baseBranchToMerge?: string;
    destinationBranchToMerge?: string;
}

/** The toggle options for select the global view mode. */
export type GlobalViewMode = "prs" | "branchSync";