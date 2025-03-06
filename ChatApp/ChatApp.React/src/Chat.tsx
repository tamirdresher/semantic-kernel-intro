import { Button } from "@fluentui/react-components";
import {
    AIChatMessage,
    AIChatProtocolClient,
    AIChatError,
    AIChatClientOptions,
} from "@microsoft/ai-chat-protocol";
import { useEffect, useId, useRef, useState } from "react";
import ReactMarkdown from "react-markdown";
import TextareaAutosize from "react-textarea-autosize";
import styles from "./Chat.module.css";
import gfm from "remark-gfm";

type ChatEntry = AIChatMessage | AIChatError;
type FullChatEntry = ChatMessage | AIChatMessage | AIChatError;

function isChatError(entry: unknown): entry is AIChatError {
    return (entry as AIChatError).code !== undefined;
}

export interface ChatMessage {
    role: string; // The role of the message sender (e.g., "user", "assistant")
    content: string; // The content of the message
}

export default function Chat({ style }: { style: React.CSSProperties }) {
    const clientOptions: AIChatClientOptions = {        
        retryOptions: {
            maxRetries: 0           
        },
    };

    const client = new AIChatProtocolClient("/api/chat/", clientOptions);

    const [messages, setMessages] = useState<ChatEntry[]>([]);
    const [fullMessages, setFullMessages] = useState<FullChatEntry[]>([]);
    const [isFullMessagesVisible, setIsFullMessagesVisible] = useState(false);
    const [input, setInput] = useState<string>("");
    const [streaming] = useState<boolean>(false);
    const inputId = useId();
    const [sessionState, setSessionState] = useState<unknown>(undefined);
    const messagesEndRef = useRef<HTMLDivElement>(null);

    const scrollToBottom = () => {
        messagesEndRef.current?.scrollIntoView({ behavior: "smooth" });
    };
    useEffect(scrollToBottom, [messages]);
    useEffect(scrollToBottom, [fullMessages]);


    const fetchLatestMessages = async (sessionId: string) => {
        try {
            const response = await fetch("/api/Chat/getLatestMessages", {
                method: "POST",
                headers: { "Content-Type": "application/json" },
                body: JSON.stringify({ sessionId: sessionId }),
            });
            if (!response.ok) {
                throw new Error("Failed to fetch latest messages");
            }
            const latestMessages: ChatMessage[] = await response.json();
            setFullMessages((prevMessages) => [...prevMessages, ...latestMessages]);
        } catch (error) {
            const errorMessage: AIChatError = {
                code: "error",
                message: JSON.stringify(error, Object.getOwnPropertyNames(error))
            };
            setFullMessages((prevMessages) => [...prevMessages, errorMessage]);
            console.error("Error fetching latest messages:", error);
        }
    };

    const toggleFullMessagesView = () => {
        setIsFullMessagesVisible((prev) => !prev);
    };

    const sendMessage = async () => {
        const message: AIChatMessage = {
            role: "user",
            content: input,
        };
        const updatedMessages = [...messages, message];
        setMessages(updatedMessages);
        setInput("");
        try {
            if (streaming) {
                const result = await client.getStreamedCompletion([message], {
                    sessionState: sessionState,
                });
                const latestMessage: AIChatMessage = { content: "", role: "assistant" };
                for await (const response of result) {
                    if (response.sessionState) {
                        setSessionState(response.sessionState);
                    }
                    if (!response.delta) {
                        continue;
                    }
                    if (response.delta.role) {
                        latestMessage.role = response.delta.role;
                    }
                    if (response.delta.content) {
                        latestMessage.content += response.delta.content;
                        setMessages([...updatedMessages, latestMessage]);
                    }
                }
            } else {
                try {
                    const result = await client.getCompletion([message], {
                        sessionState: sessionState,
                    });
                    setSessionState(result.sessionState);
                    setMessages([...updatedMessages, result.message]);

                    await fetchLatestMessages(result.sessionState as string)
                } catch (error) {
                    const errorMessage: AIChatError = {
                        code: "error",
                        message: JSON.stringify(error, Object.getOwnPropertyNames(error))

                    }
                    setMessages([...updatedMessages, errorMessage]);
                    console.error("Error sending message:", error);
                }
            }
        } catch (e) {
            if (isChatError(e)) {
                setMessages([...updatedMessages, e]);
            }
        }
    };

    const getClassName = (message: FullChatEntry) => {
        if (isChatError(message)) {
            return styles.caution;
        }
        return message.role === "user"
            ? styles.userMessage
            : styles.assistantMessage;
    };

    const getErrorMessage = (message: AIChatError) => {
        return `${message.code}: ${message.message}`;
    };

    return (
        <div className={styles.chatWindow} style={style}>
            <div className={styles.messages}>
                {messages.map((message) => (
                    <div key={crypto.randomUUID()} className={getClassName(message)}>
                        {isChatError(message) ? (
                            <>{getErrorMessage(message)}</>
                        ) : (
                            <div className={styles.messageBubble}>
                                <ReactMarkdown remarkPlugins={[gfm]}>
                                    {message.content}
                                </ReactMarkdown>
                            </div>
                        )}
                    </div>
                ))}
                <div ref={messagesEndRef} />
            </div>
            <div className={styles.inputArea}>
                <TextareaAutosize
                    id={inputId}
                    value={input}
                    onChange={(e) => setInput(e.target.value)}
                    onKeyDown={(e) => {
                        if (e.key === "Enter" && e.shiftKey) {
                            e.preventDefault();
                            sendMessage();
                        }
                    }}
                    minRows={1}
                    maxRows={4}
                />
                <Button onClick={sendMessage}>Send</Button>                
            </div>
            <div className={styles.fullMessagesToggle}>
                <Button onClick={toggleFullMessagesView}>
                    {isFullMessagesVisible ? "Hide Full Messages" : "Show Full Messages"}
                </Button>
                {isFullMessagesVisible && (
                    <div className={styles.fullMessages}>
                        {fullMessages.map((message) => (
                            <div key={crypto.randomUUID()} className={getClassName(message)}>
                                {isChatError(message) ? (
                                    <>{getErrorMessage(message)}</>
                                ) : (
                                    <div className={styles.messageBubble}>
                                        <ReactMarkdown remarkPlugins={[gfm]}>
                                            {message.content}
                                        </ReactMarkdown>
                                    </div>
                                )}
                            </div>
                        ))}
                    </div>
                )}
            </div>
        </div>
    );
}
