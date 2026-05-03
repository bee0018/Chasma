using ChasmaWebApi.Data.Objects.DryRun;
using System.Text;

namespace ChasmaWebApi.Util
{
    /// <summary>
    /// Utility class for handling common operations related to dry run simulations, such as setting error messages and updating simulation results.
    /// </summary>
    public static class DryRunHelper
    {
        /// <summary>
        /// Fails the simulation result and sets the error message to be returned to the client based on the provided error message.
        /// </summary>
        /// <param name="result">The simulation result.</param>
        /// <param name="errorMessage">The simulation error message.</param>
        public static void FailSimulationResult(SimulatedResultBase result, string errorMessage)
        {
            result.IsSuccessful = false;
            result.ErrorMessage = errorMessage;
        }

        /// <summary>
        /// Generates an HTML report summarizing merge conflict details for the specified package.
        /// </summary>
        /// <param name="package">The merge conflict result package containing repository, branch, commit, and conflicting file information.</param>
        /// <returns>An HTML string representing the merge conflict report.</returns>
        public static string BuildMergeConflictResultPackageHtmlContent(MergeConflictResultPackageEntry package)
        {
            StringBuilder conflictingFiles = new();
            foreach (string file in package.ConflictingFiles)
            {
                conflictingFiles.AppendLine($"<li>{file}</li>");
            }

            return $@"
            <!DOCTYPE html>
            <html>
            <head>
            <meta charset='utf-8'>
            <style>
                body {{font-family: 'Segoe UI', sans-serif;
                background: #121212;
                color: #eee;
                margin: 40px;
            }}

            h1 {{color: #22d3ee;
                margin-bottom: 20px;
                border-bottom: 2px solid #22d3ee;
                padding-bottom: 6px;
            }}

            .panel-card {{background: linear-gradient(145deg, #1c1c1c, #242424);
                border-radius: 12px;
                padding: 16px;
                margin-bottom: 20px;
                box-shadow: 0 4px 12px rgba(0,0,0,0.6);
            }}

            .panel-card h2 {{color: #22d3ee;
                margin-bottom: 10px;
            }}

            .label {{font-weight: 600;
                color: #aaa;
            }}

            ul {{margin: 8px 0 0 20px;
            }}

            .badge {{display: inline-block;
                padding: 4px 10px;
                border-radius: 6px;
                font-size: 12px;
                font-weight: bold;
            }}

            .badge.ok {{background: #16a34a;
                color: white;
            }}

            .badge.warn {{background: #dc2626;
                color: white;
            }}

            .footer {{margin-top: 40px;
                text-align: center;
                font-size: 12px;
                color: #777;
            }}
            </style>
            </head>
            <body>
                <h1>Conflict Detection Report</h1>
                <div class='panel-card'>
                    <h2>Report Overview</h2>
                    <div><span class='label'>Repository:</span> {package.Repository?.Name ?? "N/A"}</div>
                    <div><span class='label'>Timestamp:</span> {package.TimeStamp}</div>
                <p><span class='label' /> 
                        <span class='badge warn'>Total Conflicted Files: {package.ConflictingFiles.Count}</span>
                    </p>
                </div>
                <h2>Branch Merge</h2>
                <div class='panel-card'>
                    {package.SourceBranch.FriendlyName} ➜ {package.DestinationBranch.FriendlyName}
                </div>
                <h2>Commit SHAs</h2>
                <div class='panel-card'>
                    <div><span class='label'>Base:</span> {package.BaseCommit?.Sha ?? "N/A"}</div>
                    <div><span class='label'>Source:</span> {package.SourceBranch.Tip.Sha}</div>
                    <div><span class='label'>Destination:</span> {package.DestinationBranch.Tip.Sha}</div>
                </div>
                <h2>Conflicting Files</h2>
                <div class='panel-card'>
                    <ul>
                        {conflictingFiles}
                    </ul>
                </div>
                <h2>Merge Strategies</h2>
                <div class='panel-card'>
                    <p><strong>Abort On Conflict</strong> - Stops immediately when conflicts are detected. Use when:</p>
                    <ul>
                        <li>You want strict validation</li>
                        <li>You prefer manual review for all conflicts</li>
                    </ul>
                        <p><strong>Ours Wins</strong> - Always prefers changes from the destination branch (target/current branch) when conflicts occur. Incoming changes are ignored in conflict regions. Use when:</p>
                    <ul>
                        <li>The destination branch is considered authoritative (e.g. main)</li>
                        <li>You want to preserve existing state</li>
                    </ul>
                        <p><strong>Theirs Wins</strong> - Always prefers changes from the source branch (incoming branch) when conflicts occur. Destination changes are overwritten in conflict regions. Use when:</p>
                    <ul>
                        <li>Incoming branch is trusted or authoritative</li>
                        <li>You want to fully adopt feature branch changes</li>
                    </ul>
                        <p><strong>Manual Resolution Required</strong> - Leaves all conflicts unresolved and marked for manual review. No automatic merging is performed. Use when:</p>
                    <ul>
                        <li>You want full visibility into conflicts</li>
                        <li>You plan to resolve them in an editor or UI</li>
                    </ul>
                        <p><strong>Prefer Base When Unchanged</strong> - Uses a 3-way merge heuristic in which if one side hasn't changed from the base commit, the other side wins. Also, if both sides changed the same region, a conflict is created. Use when:</p>
                    <ul>
                        <li>You want standard Git-like behavior</li>
                        <li>You want minimal unnecessary conflicts</li>
                    </ul>
                        <p><strong>Last Write Wins</strong> - Resolves conflicts based on timestamps. The most recently modified change overwrites earlier ones. Use when:</p>
                    <ul>
                        <li>Working with configuration or non-code data</li>
                        <li>You prefer `latest update wins` semantics</li>
                        <li><strong>WARNING:</strong> Can silently overwrite meaningful changes</li>
                    </ul>
                        <p><strong>Line-Level Heuristic Merge:</strong> - Performs a line-by-line merge similar to Git’s default behavior of non-overlapping changes are merged automatically and only overlapping edits become conflicts. Use when:</p>
                    <ul>
                        <li>You want standard Git-like merging</li>
                        <li>Working with code files</li>
                    </ul>
                        <p><strong>Rule-Based Merge</strong> - Applies user-defined rules to resolve conflicts. Rules can be file-path, directory, or content-based. Use when:</p>
                    <ul>
                        <li>You want deterministic enterprise workflows</li>
                        <li>You need fine-grained control over merge behavior</li>
                    </ul>
                        <p><strong>Semantic Merge:</strong> Attempts to understand code structure rather than raw text. Merges based on syntax/AST-level changes instead of line differences. Use when:</p>
                    <ul>
                        <li>Working with complex codebases</li>
                        <li>You want intelligent merging beyond text comparison</li>
                    </ul>
                </div>
                <div class='footer'>
                    Generated Report on {DateTime.Now:yyyy-MM-dd HH:mm}
                </div>
                </body>
            </html>";
        }
    }
}
