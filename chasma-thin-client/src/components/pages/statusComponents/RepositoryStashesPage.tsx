import React, {useEffect, useMemo, useState} from "react";
import {
    GetStashDetailsRequest,
    GetStashListRequest,
    PatchEntry,
    StashEntry
} from "../../../API/ChasmaWebApiClient";
import {parseUnifiedDiff} from "../../../managers/DiffViewerManager";
import ApplyStashModal from "../../modals/ApplyStashModal";
import DeleteStashModal from "../../modals/DeleteStashModal";
import {stashClient} from "../../../managers/ApiClientManager";
import { useNavigate } from "react-router-dom";
import { useCacheStore } from "../../../managers/CacheManager";
import { handleApiError } from "../../../managers/TransactionHandlerManager";
import { Virtuoso } from "react-virtuoso";

/** Defines the properties of the Repository Stashes Page. **/
interface IRepositoryStashesPageProps {
    /** The repository identifier. **/
    repositoryId: string | undefined;
}

/**
 * Initializes a new instance of the RepositoryStashesPage component.
 * @param props The properties of the repository stashes page.
 * @constructor
 */
const RepositoryStashesPage: React.FC<IRepositoryStashesPageProps> = (props: IRepositoryStashesPageProps) => {
    /** Gets or sets a value indicating whether the diff viewer is in split mode. **/
    const [isSplitView, setIsSplitView] = useState(false);

    /** Gets or sets the stash entries. **/
    const [stashEntries, setStashEntries] = useState<StashEntry[] | undefined>(undefined);

    /** Gets or sets the patch entries. **/
    const [patchEntries, setPatchEntries] = useState<PatchEntry[] | undefined>(undefined);

    /** Gets or sets the stash entry. **/
    const [selectedStashEntry, setSelectedStashEntry] = useState<StashEntry | null>(null);

    /** Gets or sets the patch entry. **/
    const [selectedPatchEntry, setSelectedPatchEntry] = useState<PatchEntry | null>(null);

    /** Gets or sets the stash entry context menu. **/
    const [stashEntryContextMenu, setStashEntryContextMenu] = useState<{
        mouseX: number;
        mouseY: number;
        stashEntry: StashEntry;
    } | null>(null);

    /** Gets or sets the raw diff of a specific file. **/
    const [rawDiff, setRawDiff] = useState<string>("");

    /** Gets or sets a value indicating whether the user is applying the stash. **/
    const [isApplyingStash, setIsApplyingStash] = useState<boolean>(false);

    /** Gets or sets a value indicating whether the user is removing the stash. **/
    const [isRemovingStash, setIsRemovingStash] = useState<boolean>(false);

    /** Gets or sets the apply stash index. **/
    const [applyStashIndex, setApplyStashIndex] = useState<number | undefined>(undefined);

    /** Gets or sets the remove stash index. **/
    const [removeStashIndex, setRemoveStashIndex] = useState<number | undefined>(undefined);

    /** The navigation function. **/
    const navigate = useNavigate();

    /** Sets the notification modal. */
    const setNotification = useCacheStore(state => state.setNotification);

    /** Gets or sets the value indicating whether the parsed diff is too big. */
    const [parsedDiffTooBig, setParsedDiffTooBig] = useState(false);

     /** Defines the maximum raw diff size. 2MB. */
    const MAX_RAW_DIFF_SIZE = 2_000_000;

    /** Gets the maximum parsed lines to render. */
    const MAX_PARSED_LINES = 5000;

    /**
     * Handles the event when the user clicks a stash entry.
     * @param stashEntry The stash entry to be selected.
     */
    const handleSelectedStash = (stashEntry: StashEntry | null) => {
        setSelectedStashEntry(stashEntry);
        handleGetStashDetailsRequest(stashEntry).catch(e => console.error(e));
    };

    /**
     * Handles the event when the user selects a patch entry.
     * @param patchEntry The entry to view the diff of.
     */
    const handleSelectedPatchEntry = (patchEntry : PatchEntry | null) => {
        setSelectedPatchEntry(patchEntry);
        if (patchEntry?.diff) {
            setRawDiff(patchEntry.diff)
        }
        else {
            setRawDiff("");
        }
    }

    /**
     * Handles the event when the user wants to get the stash entry details.
     * @param stashEntry The stash to get diffed changes.
     */
    async function handleGetStashDetailsRequest(stashEntry: StashEntry | null) {
        if (!props.repositoryId || stashEntry === null) return;

        try {
            const request = new GetStashDetailsRequest();
            request.repositoryId = props.repositoryId;
            request.stashEntry = stashEntry;
            const response = await stashClient.getStashDetails(request);
            if (response.isErrorResponse) {
                setNotification({
                    title: "Could not get stash details!",
                    message: response.errorMessage,
                    isError: true,
                });
                return;
            }

            setPatchEntries(response.patchEntries);
            if (response.patchEntries && response.patchEntries.length > 0) {
                const firstPatchEntry = response.patchEntries[0];
                handleSelectedPatchEntry(firstPatchEntry);
            }
        } catch (e) {
            const errorNotification = handleApiError(e, navigate, "Error getting stash details!", "Review internal server and console logs for more information.");
            setNotification(errorNotification);
        }
    }

    /**
     * Handles the event when the user right-clicks a stash entry to open the context menu.
     * @param event The mouse event.
     * @param stashEntry The stash entry that is clicked on.
     */
    const handleStashEntryContextMenu = (event: React.MouseEvent, stashEntry: StashEntry) => {
        event.preventDefault();
        setStashEntryContextMenu({
            mouseX: event.clientX,
            mouseY: event.clientY,
            stashEntry,
        });
    };

    /**
     * Fetches the stash list in the specified repository.
     */
    const fetchStashList = async () => {
        try {
            const request = new GetStashListRequest()
            request.repositoryId = props.repositoryId;
            const response = await stashClient.getStashList(request);
            if (response.isErrorResponse) {
                setNotification({
                    title: "Could not get stash list!",
                    message: response.errorMessage,
                    isError: true,
                });
                return;
            }

            setStashEntries(response.stashList)
        }
        catch (e) {
            const errorNotification = handleApiError(e, navigate, "Error getting stash list!", "Review internal server and console logs for more information.");
            setNotification(errorNotification);
        }
    };

    /**
     * Handles the action to apply the stash action.
     * @param index The index of the stash.
     */
    const handleApplyStashAction = (index: number | undefined) => {
        setApplyStashIndex(index);
        setIsApplyingStash(true);
    };

    /**
     * Handles the action to remove the stash.
     * @param index The index of the stash.
     */
    const handleRemoveStashAction = (index: number | undefined) => {
        setRemoveStashIndex(index);
        setIsRemovingStash(true);
    }
    
    /** The parsed unified diff. */
    const parsedDiff = useMemo(() => {
        if (!rawDiff) {
            return [];
        }

        if (rawDiff.length > MAX_RAW_DIFF_SIZE) {
            console.warn("Diff too large, truncating for performance.");
            return parseUnifiedDiff(rawDiff.slice(0, MAX_RAW_DIFF_SIZE));
        }

        return parseUnifiedDiff(rawDiff);
    }, [rawDiff]);

    /** Gets the safely parsed lines to display to prevent freezing. */
    const safeParsedDiff = useMemo(() => {
        if (parsedDiff.length > MAX_PARSED_LINES) {
            console.warn("Parsed diff too long, showing only first 5000 lines.");
            setParsedDiffTooBig(true);
            return parsedDiff.slice(0, MAX_PARSED_LINES);
        }
        setParsedDiffTooBig(false);
        return parsedDiff;
    }, [parsedDiff]);

    useEffect(() => {
        const closeMenu = () => setStashEntryContextMenu(null);
        window.addEventListener("click", closeMenu);
        return () => window.removeEventListener("click", closeMenu);
    }, []);

    useEffect(() => {
        fetchStashList().catch(e => console.error(e));
    }, [props.repositoryId]);

    return (
        <>
            <div className="content">
                <div className="main-layout">
                    {/* Left side: Stash entries/patch entries */}
                    <div className="left-panel">
                        <div className="panel-card">
                            <h2 className="page-description">Stashes Entries</h2>
                            {stashEntries ? (
                                <table className="status-table">
                                    <thead>
                                    <tr>
                                        <th>Index</th>
                                        <th>Title</th>
                                    </tr>
                                    </thead>
                                    {stashEntries?.map((element, index) => (
                                        <tbody key={index}>
                                        <tr className={selectedStashEntry?.index === element.index ? "selected" : ""}>
                                            <td>{element.index}</td>
                                            <td
                                                onClick={() => handleSelectedStash(element)}
                                                onContextMenu={e => handleStashEntryContextMenu(e, element)}
                                            >
                                                {element.stashMessage}
                                            </td>
                                        </tr>
                                        </tbody>
                                    ))}
                                </table>
                            ) : <div className="empty-table">No stashes in repository.</div>}
                        </div>

                        <div className="panel-card">
                            <h2 className="page-description">Changes in selected stash</h2>
                            {patchEntries ? (
                                <table className="status-table">
                                    <thead>
                                    <tr>
                                        <th>File</th>
                                    </tr>
                                    </thead>
                                    {patchEntries?.map((element, index) => (
                                        <tbody key={index}>
                                        <tr className={selectedPatchEntry?.filePath === element.filePath ? "selected" : ""}>
                                            <td onClick={() => handleSelectedPatchEntry(element)}>
                                                {element.filePath}
                                            </td>
                                        </tr>
                                        </tbody>
                                    ))}
                                </table>
                            ) : <div className="empty-table">No file changes</div>}
                        </div>
                    </div>

                    {stashEntryContextMenu && (
                        <div
                            className="context-menu"
                            style={{
                                top: stashEntryContextMenu.mouseY,
                                left: stashEntryContextMenu.mouseX,
                            }}
                            onClick={() => setStashEntryContextMenu(null)}
                        >
                            <ul>
                                <li onClick={() => handleApplyStashAction(stashEntryContextMenu.stashEntry.index)}>
                                    Apply Stash {stashEntryContextMenu.stashEntry.index}
                                </li>
                                <li onClick={() => handleRemoveStashAction(stashEntryContextMenu.stashEntry.index)}>
                                    Remove Stash {stashEntryContextMenu.stashEntry.index}
                                </li>
                            </ul>
                        </div>
                    )}

                    {/* Right side: Diff viewer */}
                    <div className="right-panel">
                        {parsedDiffTooBig && <p style={{textAlign: "center", color: "yellow"}}>Parsed diff too long, showing only first 5000 lines.</p>}
                        <div className="diff-toolbar">
                            <button
                                className="submit-button"
                                onClick={() => setIsSplitView(!isSplitView)}
                            >
                                {isSplitView ? "Toggle Unified View" : "Toggle Split View"}
                            </button>
                            {selectedStashEntry && (
                                <button
                                    className="submit-button"
                                    onClick={() => handleApplyStashAction(selectedStashEntry.index)}
                                >
                                    Apply Stash {selectedStashEntry.index}
                                </button>
                            )}
                        </div>

                        {selectedStashEntry ? (
                            <div className={`diff-viewer ${isSplitView ? "diff-side-by-side" : ""}`}>
                                {!isSplitView && (
                                    <div className="diff-panel"
                                        style={{overflow: "hidden"}}>
                                        <div className="diff-panel-header">Unified Diff: {selectedPatchEntry?.filePath}</div>
                                        <Virtuoso
                                            totalCount={safeParsedDiff.length}
                                            itemContent={(index: number) => {
                                                const line = safeParsedDiff[index];
                                                return (
                                                    <div
                                                        className={`diff-line ${
                                                            line.type === "add"
                                                            ? "diff-added"
                                                            : line.type === "remove"
                                                            ? "diff-removed"
                                                            : ""}`}>
                                                                <span className="diff-line-number">
                                                                    {line.oldLineNumber ?? ""}
                                                                </span>
                                                                <span className="diff-line-number">
                                                                    {line.newLineNumber ?? ""}
                                                                </span>
                                                                <span className="diff-code">{line.content}</span>
                                                    </div>
                                                );
                                            }}
                                        />
                                    </div>
                                )}
                                {isSplitView && (
                                    <>
                                        <div className="diff-panel">
                                            <div className="diff-panel-header">Original: {selectedPatchEntry?.filePath}</div>
                                            <Virtuoso
                                                style={{ height: "600px" }}
                                                totalCount={safeParsedDiff.length}
                                                itemContent={(index: number) => {
                                                    const line = safeParsedDiff[index];
                                                    return (
                                                    <div
                                                    className={`diff-line ${
                                                    line.type === "remove" ? "diff-removed" : ""
                                                    }`}
                                                    >
                                                    <span className="diff-line-number">
                                                    {line.oldLineNumber ?? ""}
                                                    </span>
                                                    <span className="diff-code">
                                                    {line.type === "add" ? "" : line.content}
                                                    </span>
                                                    </div>
                                                    );
                                                }}
                                            />
                                        </div>
                                        <div className="diff-panel">
                                            <div className="diff-panel-header">Modified: {selectedPatchEntry?.filePath}</div>
                                            <Virtuoso
                                                style={{ height: "600px" }}
                                                totalCount={safeParsedDiff.length}
                                                itemContent={(index: number) => {
                                                    const line = safeParsedDiff[index];
                                                    return (
                                                        <div
                                                            className={`diff-line ${
                                                                line.type === "add" ? "diff-added" : ""
                                                            }`}
                                                        >
                                                            <span className="diff-line-number">
                                                                {line.newLineNumber ?? ""}
                                                            </span>
                                                            <span className="diff-code">
                                                                {line.type === "remove" ? "" : line.content}
                                                            </span>
                                                        </div>
                                                    );
                                                }}
                                            />
                                        </div>
                                    </>
                                )}
                            </div>
                        ) : (
                            <div className="empty-table">Select a file to view diff</div>
                        )}
                    </div>
                </div>
            </div>
            {isApplyingStash &&
                <ApplyStashModal
                    repositoryId={props.repositoryId}
                    stashIndex={applyStashIndex}
                    onClose={() => setIsApplyingStash(false)}
                    onSuccess={fetchStashList} />
            }
            {isRemovingStash &&
                <DeleteStashModal
                    repositoryId={props.repositoryId}
                    stashIndex={removeStashIndex}
                    onClose={() => setIsRemovingStash(false)}
                    onSuccess={fetchStashList} />
            }
        </>
    )
}

export default RepositoryStashesPage;