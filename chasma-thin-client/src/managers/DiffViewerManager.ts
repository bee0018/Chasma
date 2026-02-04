import {DiffLine} from "../components/types/CustomTypes";

/**
 * Parses the unified diff and track line numbers.
 * @param diff The line difference.
 */
export function parseUnifiedDiff(diff: string): DiffLine[] {
    let oldLineNum = 0;
    let newLineNum = 0;
    const lines: DiffLine[] = [];
    diff.split("\n").forEach((line) => {
        if (line.startsWith("@@")) {
            const match = line.match(/@@ -(\d+),?\d* \+(\d+),?\d* @@/);
            if (match) {
                oldLineNum = parseInt(match[1], 10) - 1;
                newLineNum = parseInt(match[2], 10) - 1;
            }
            lines.push({ type: "hunk", content: line });
        } else if (line.startsWith("+")) {
            newLineNum++;
            lines.push({ type: "add", content: line.slice(1), oldLineNumber: undefined, newLineNumber: newLineNum });
        } else if (line.startsWith("-")) {
            oldLineNum++;
            lines.push({ type: "remove", content: line.slice(1), oldLineNumber: oldLineNum, newLineNumber: undefined });
        } else {
            oldLineNum++;
            newLineNum++;
            lines.push({
                type: "context",
                content: line.startsWith(" ") ? line.slice(1) : line,
                oldLineNumber: oldLineNum,
                newLineNumber: newLineNum,
            });
        }
    });

    return lines;
}