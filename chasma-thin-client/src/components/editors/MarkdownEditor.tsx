import React, { useRef, useState } from "react";
import ReactMarkdown from "react-markdown";
import remarkGfm from "remark-gfm";
import rehypeRaw from "rehype-raw";
import "../../css/MarkdownEditor.css"; // Make sure this CSS exists

interface MarkdownEditorProps {
    value: string | undefined;
    onChange: (value: string) => void;
    placeholder?: string;
    minHeight?: number;
    disabled?: boolean;
}

const MarkdownEditor: React.FC<MarkdownEditorProps> = ({
                                                           value,
                                                           onChange,
                                                           placeholder = "Enter markdown...",
                                                           minHeight = 150,
                                                           disabled = false
                                                       }) => {
    const [isPreview, setIsPreview] = useState<boolean>(false);
    const textareaRef = useRef<HTMLTextAreaElement | null>(null);

    const insertMarkdown = (before: string, after: string = "") => {
        if (!textareaRef.current) return;
        const textarea = textareaRef.current;
        const start = textarea.selectionStart;
        const end = textarea.selectionEnd;
        const selectedText = value?.substring(start, end) ?? "";
        const newValue =
            (value ?? "").substring(0, start) +
            before +
            selectedText +
            after +
            (value ?? "").substring(end);
        onChange(newValue);

        setTimeout(() => {
            textarea.focus();
            textarea.setSelectionRange(
                start + before.length,
                start + before.length + selectedText.length
            );
        }, 0);
    };

    const handleKeyDown = (e: React.KeyboardEvent<HTMLTextAreaElement>) => {
        if (!e.ctrlKey && !e.metaKey) return;
        switch (e.key.toLowerCase()) {
            case "b":
                e.preventDefault();
                insertMarkdown("**", "**");
                break;
            case "i":
                e.preventDefault();
                insertMarkdown("_", "_");
                break;
            case "`":
                e.preventDefault();
                insertMarkdown("`", "`");
                break;
        }
    };

    const handlePaste = (e: React.ClipboardEvent<HTMLTextAreaElement>) => {
        const items = e.clipboardData.items;
        if (!items) return;

        Array.from(items).forEach((item) => {
            if (item.type.indexOf("image") === 0) {
                e.preventDefault();
                const file = item.getAsFile();
                if (!file) return;

                const reader = new FileReader();
                reader.onload = () => {
                    const base64 = reader.result as string;
                    insertMarkdown(`![pasted-image](${base64})`);
                };
                reader.readAsDataURL(file);
            }
        });
    };

    return (
        <div className="markdown-editor">
            {/* Preview toggle */}
            <div className="markdown-preview-toggle">
                <button
                    type="button"
                    className="commit-modal-button"
                    disabled={disabled}
                    onClick={() => setIsPreview(!isPreview)}
                >
                    {isPreview ? "Edit Markdown" : "Preview"}
                </button>
            </div>

            {/* Toolbar */}
            {!isPreview && (
                <div className="markdown-toolbar">
                    <button
                        title="Bold (Ctrl/Cmd+B)"
                        onClick={() => insertMarkdown("**", "**")}
                        disabled={disabled}
                        dangerouslySetInnerHTML={{ __html: `<strong>B</strong>` }}
                    />
                    <button
                        title="Italic (Ctrl/Cmd+I)"
                        onClick={() => insertMarkdown("_", "_")}
                        disabled={disabled}
                        dangerouslySetInnerHTML={{ __html: `<em>I</em>` }}
                    />
                    <button
                        title="Inline Code (Ctrl/Cmd+`)"
                        onClick={() => insertMarkdown("`", "`")}
                        disabled={disabled}
                    >
                        <svg width="16" height="16" viewBox="0 0 16 16" fill="none">
                            <path
                                d="M6.5 4L2 8L6.5 12"
                                stroke="currentColor"
                                strokeWidth="2"
                                strokeLinecap="round"
                                strokeLinejoin="round"
                            />
                            <path
                                d="M9.5 4L14 8L9.5 12"
                                stroke="currentColor"
                                strokeWidth="2"
                                strokeLinecap="round"
                                strokeLinejoin="round"
                            />
                        </svg>
                    </button>
                    <button
                        title="Code Block"
                        onClick={() => insertMarkdown("\n```ts\n", "\n```\n")}
                        disabled={disabled}
                    >
                        <svg width="16" height="16" viewBox="0 0 16 16" fill="none">
                            <path
                                d="M3 4H13V12H3V4Z"
                                stroke="currentColor"
                                strokeWidth="2"
                                strokeLinecap="round"
                                strokeLinejoin="round"
                            />
                        </svg>
                    </button>
                    <button
                        title="Quote"
                        onClick={() => insertMarkdown("> ")}
                        disabled={disabled}
                    >
                        <svg width="16" height="16" viewBox="0 0 16 16" fill="none">
                            <path
                                d="M4 6H12M4 10H12"
                                stroke="currentColor"
                                strokeWidth="2"
                                strokeLinecap="round"
                                strokeLinejoin="round"
                            />
                        </svg>
                    </button>
                    <button
                        title="Checkbox"
                        onClick={() => insertMarkdown("- [ ] ")}
                        disabled={disabled}
                    >
                        <svg width="16" height="16" viewBox="0 0 16 16" fill="none">
                            <rect x="2" y="2" width="12" height="12" stroke="currentColor" strokeWidth="2"/>
                        </svg>
                    </button>
                    <button
                        title="Link"
                        onClick={() => insertMarkdown("[text](", ")")}
                        disabled={disabled}
                    >
                        <svg width="16" height="16" viewBox="0 0 16 16" fill="none">
                            <path
                                d="M4.5 8H11.5"
                                stroke="currentColor"
                                strokeWidth="2"
                                strokeLinecap="round"
                            />
                            <path
                                d="M7 5L4 8L7 11"
                                stroke="currentColor"
                                strokeWidth="2"
                                strokeLinecap="round"
                            />
                        </svg>
                    </button>
                    <button
                        title="List"
                        onClick={() => insertMarkdown("- ")}
                        disabled={disabled}
                    >
                        <svg width="16" height="16" viewBox="0 0 16 16" fill="none">
                            <path
                                d="M4 4H12M4 8H12M4 12H12"
                                stroke="currentColor"
                                strokeWidth="2"
                                strokeLinecap="round"
                            />
                        </svg>
                    </button>
                </div>
            )}

            {/* Textarea */}
            {!isPreview ? (
                <textarea
                    ref={textareaRef}
                    className="input-area"
                    placeholder={placeholder}
                    value={value ?? ""}
                    disabled={disabled}
                    onChange={(e) => onChange(e.target.value)}
                    onKeyDown={handleKeyDown}
                    onPaste={handlePaste}
                    style={{ minHeight }}
                />
            ) : (
                <div
                    className="markdown-preview"
                    style={{ minHeight }}
                >
                    <ReactMarkdown
                        remarkPlugins={[remarkGfm]}
                        rehypePlugins={[rehypeRaw]}
                        components={{
                            img: ({ node, ...props }) => {
                                // Make sure base64 images render correctly
                                return <img {...props} style={{ maxWidth: "100%", borderRadius: "4px" }} />;
                            }
                        }}
                    >
                        {value || "_Nothing to preview_"}
                    </ReactMarkdown>

                </div>
            )}
        </div>
    );
};

export default MarkdownEditor;
