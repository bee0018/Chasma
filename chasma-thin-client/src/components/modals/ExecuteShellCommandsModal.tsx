import React, {useCallback, useState} from "react";
import {Row} from "../types/CustomTypes"
import {ExecuteShellCommandRequest, ShellClient} from "../../API/ChasmaWebApiClient";
import {apiBaseUrl} from "../../environmentConstants";
import {isBlankOrUndefined} from "../../stringHelperUtil";

/**
 * The members of the execute shell commands modal.
 */
interface IExecuteShellCommandsProps {
    /** Function to call when the modal is being closed. **/
    onClose: () => void,

    /** The repository identifier to execute shell commands in. **/
    repositoryId: string | undefined
}

/** The shell client used to interact with the API. **/
const shellClient = new ShellClient(apiBaseUrl);

/**
 * Initializes a new ExecuteShellCommandsModal class.
 * @param props The properties to execute shell commands in repositories.
 * @constructor
 */
const ExecuteShellCommandsModal: React.FC<IExecuteShellCommandsProps> = (props: IExecuteShellCommandsProps) => {
    /** Gets or sets the shell command rows. **/
    const [rows, setRows] = useState<Row[]>([]);

    /** Gets or sets the error message. **/
    const [errorMessage, setErrorMessage] = React.useState<string | undefined>(undefined);

    /** Gets or sets a flag indicating whether the commands have been executed. **/
    const [commandsExecuted, setCommandsExecuted] = React.useState<boolean>(false);

    /** Gets or sets the command output after the commands have been executed. **/
    const [output, setOutput] = React.useState<string>("");

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
               setCommandsExecuted(false);
               setOutput("");
               return;
           }

           const commandOutput = response.outputMessages
               ? response.outputMessages.map(i => i).join("\n")
               : "";
           setOutput(commandOutput);
       } catch (e) {
           console.error(e);
           setOutput("")
           setErrorMessage("Error executing shell commands. Check console logs for more information.")
       }
       finally {
           setCommandsExecuted(true);
       }}, [rows]);

    /** Resets the form to a pre-filled state. **/
    function resetForm() {
        setRows([]);
        setOutput("");
        setCommandsExecuted(false);
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
                <div className="modal-backdrop" onClick={props.onClose}>
                    <div className="commit-modal" onClick={(e) => e.stopPropagation()}>

                        {!commandsExecuted && (
                            <>
                                <div className="header-row">
                                    {errorMessage && <h3 className="commit-modal-message">{errorMessage}</h3>}
                                    <h2>Enter shell commands</h2>
                                    <button
                                        type="button"
                                        id="addClaimButton"
                                        className="circle-button"
                                        onClick={addCustomShellCommandRow}
                                    >
                                        +
                                    </button>
                                </div>
                                <br/>
                                <div id="customClaimsContainer">
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
                                                className="input-field"
                                                value={row.first}
                                                onChange={e => handleShellCommandChange(row.id, "first", e.target.value)}/>
                                            <button
                                                className="delete-button"
                                                type="button"
                                                onClick={() => deleteShellCommandRow(row.id)}
                                            >
                                                Remove
                                            </button>
                                        </div>
                                    ))}
                                    <br/>
                                </div>
                                <div>
                                    <button className="commit-modal-button"
                                            style={{marginRight: "50px"}}
                                            onClick={handleExecuteShellCommandsRequest}
                                    >
                                        Execute Commands
                                    </button>
                                    <button className="commit-modal-button"
                                            onClick={props.onClose}
                                    >
                                        Close
                                    </button>
                                </div>
                            </>
                        )}
                        {commandsExecuted && (
                            <>
                                <div className="commit-modal-title">
                                    <h2>Console Output</h2>
                                </div>
                                <textarea className="input-area"
                                        value={output}
                                        readOnly={true}/>
                                <br/>
                                <div>
                                    <button className="commit-modal-button"
                                            style={{marginRight: "50px"}}
                                            onClick={resetForm}
                                    >
                                        Restart
                                    </button>
                                    <button className="commit-modal-button"
                                            onClick={props.onClose}
                                    >
                                        Close
                                    </button>
                                </div>
                            </>
                        )}
                    </div>
                </div>
            </>
        )
}

export default ExecuteShellCommandsModal;