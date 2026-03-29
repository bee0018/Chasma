import { useCallback, useState } from "react";
import { ExecuteShellCommandRequest, ShellCommandResult } from "../../../API/ChasmaWebApiClient";
import { shellClient } from "../../../managers/ApiClientManager";
import { isBlankOrUndefined } from "../../../stringHelperUtil";
import { Row } from "../../types/CustomTypes";
import { useNavigate } from "react-router-dom";
import { useCacheStore } from "../../../managers/CacheManager";
import { handleApiError } from "../../../managers/TransactionHandlerManager";

/** Interface defining the members of the ExecuteShellCommandsPage. */
interface IExecuteShellCommandsPageProps {
    /** The repository identifier. */
    repositoryId: string | undefined;
}

/**
 * Initializes a new instance of the ExecuteShellCommandsPage component.
 * @constructor
 */
const ExecuteShellCommandsPage: React.FC<IExecuteShellCommandsPageProps> = (props: IExecuteShellCommandsPageProps) => {
    /** Gets or sets the shell command rows. **/
        const [rows, setRows] = useState<Row[]>([]);
    
        /** Gets or sets the error message. **/
        const [errorMessage, setErrorMessage] = useState<string | undefined>(undefined);

        /** Gets or sets the flag indicating whether to disable the send button. */
        const [disabledSendButton, setDisableSendButton] = useState(false);

        /** The navigation function. **/
        const navigate = useNavigate();

        /** Sets the notification modal. */
        const setNotification = useCacheStore(state => state.setNotification);
    
        /** Gets or sets the command output after the commands have been executed. **/
        const [output, setOutput] = useState<{
            executedCommand: string | undefined;
            success: boolean | undefined;
            message?: string | undefined;
        }[] | undefined>([]);
    
        /** Adds a shell command row to the form. **/
        const addCustomShellCommandRow = () => {
            setRows(prev => [
                ...prev,
                {id: crypto.randomUUID(), first: "", second: ""}
            ])};
    
        /** Handles custom shell command row changes in the form. **/
        const handleShellCommandChange = (
                id: string,
                field: "first" | "second",
                value: string
        ) => {
                setRows(prev =>
                    prev.map(row =>
                        row.id === id ? { ...row, [field]: value } : row
                    )
                );
            };
    
       /**
       * Handles the request execute custom commands in the API shell.
       **/
       const handleExecuteShellCommandsRequest = useCallback(async (e: React.FormEvent) => {
           e.preventDefault();
           setDisableSendButton(true);
           const command = rows.length > 0 ? "commands" : "command";
           setNotification({
            title: `Executing shell ${command}...`,
            message: "Please wait while your request is being processed. May take a few moments.",
            isError: false,
            loading: true
        });
           const request = new ExecuteShellCommandRequest();
           request.repositoryId = props.repositoryId;
           request.commands = [];
           rows.forEach(row => {
           if (!isBlankOrUndefined(row.first) && request.commands) {
               request.commands.push(row.first);
            }
           });
    
           try {
               const response = await shellClient.executeShellCommands(request);
               if (response.isErrorResponse) {
                   setErrorMessage(response.errorMessage);
                   setOutput([]);
                   setDisableSendButton(false);
                   return;
               }
    
               setOutput(
                   response.results?.map((result: ShellCommandResult) => ({
                       executedCommand: result.executedCommand,
                       success: result.isSuccess,
                       message: result.outputMessage,
                   }))
               );
               setDisableSendButton(false);
               setNotification(null);
           } catch (e) {
               setOutput([])
               setErrorMessage("Error executing shell commands. Check console logs for more information.")
               setDisableSendButton(false);
               const errorNotification = handleApiError(e, navigate, "Could not execute commands!", "Error executing shell commands. Check console logs for more information.");
               setNotification(errorNotification);
           }}, [rows, props.repositoryId]);
    
        /** Resets the form to a pre-filled state. **/
        function resetForm() {
            setRows([]);
            setOutput([]);
            setErrorMessage(undefined);
        }
    
        /**
         * Deletes the row with the specified row identifier.
         * @param rowId The row identifier.
         */
        function deleteShellCommandRow(rowId: string) {
            const filteredRows = rows.filter(row => row.id !== rowId);
            setRows(filteredRows);
        }
        
    return (
    <>
    <div className="content">
        <div className="main-layout">
            {/* Left side: Stash entries/patch entries */}
            <div className="left-panel">
                <div className="panel-card">
                    <header className="batch-page-header">
                        <h1 className="page-title">Execute Shell Commands</h1>
                        {errorMessage && <p className="page-description" style={{color: "red"}}>{errorMessage}</p>}
                    </header>
                    <div className="command-row modern-input-row">
                        {errorMessage && <h3 className="modal-message">{errorMessage}</h3>}
                        <h2>Add Command:</h2>
                        <button
                            type="button"
                            className="add-button modern-add"
                            onClick={addCustomShellCommandRow}>
                                +
                        </button>
                    </div>
                    <br/>
                    <div>
                        {rows.map(row => (
                            <div
                                key={row.id}
                                style={{
                                    display: "flex",
                                    gap: "10px",
                                    marginBottom: "10px"
                                }}
                            >
                                <input
                                    type="text"
                                    placeholder="Example: git log --oneline"
                                    className="command-input modern-input"
                                    value={row.first}
                                    onChange={e => handleShellCommandChange(row.id, "first", e.target.value)}/>
                                <button
                                    className="remove-button modern-remove"
                                    type="button"
                                    onClick={() => deleteShellCommandRow(row.id)}
                                >
                                    -
                                </button>
                            </div>
                        ))}
                        <br/>
                    </div>
                    <div className="modal-actions">
                        <button className="modal-button primary"
                                disabled={disabledSendButton}
                                onClick={handleExecuteShellCommandsRequest}
                        >
                            Execute Commands
                        </button>
                        <button className="modal-button secondary"
                                onClick={resetForm}
                        >
                            Clear
                        </button>
                    </div>
                </div>
            </div>
            {/* Right side: Diff viewer */}
            <div className="right-panel">
                <section className="output-section">
                    <div className="output-header">
                        <h3>Operation Output</h3>
                        {output && output.length > 0 && (
                            <button
                                className="clear-output-button"
                                onClick={() => setOutput([])}
                            >
                                Clear Output
                            </button>
                        )}
                    </div>
                    <div className="output-window">
                        {output?.length === 0 && <p className="no-output-text">No operations executed yet.</p>}
                        {output?.map((result, index) => (
                            <div
                            key={index}
                            className={`output-entry ${result.success ? "success" : "failure"}`}
                        >
                            <span className="output-command">&gt; {result.executedCommand}</span>
                            {result.message && (
                                <div className="output-stdout">{result.message}</div>
                            )}
                        </div>
                        ))}
                    </div>
                </section>
            </div>
        </div>
    </div>
    </>
)}

export default ExecuteShellCommandsPage;