"use client";

import { useEffect, useRef } from "react";
import type { MessageDto } from "@/types";

interface Props {
  messages: MessageDto[];
  currentUser: string;
}

export default function MessageList({ messages, currentUser }: Props) {
  const bottomRef = useRef<HTMLDivElement>(null);

  useEffect(() => {
    bottomRef.current?.scrollIntoView({ behavior: "smooth" });
  }, [messages]);

  if (messages.length === 0) {
    return (
      <div className="flex-1 flex items-center justify-center text-gray-400 text-sm">
        No messages yet. Say hello!
      </div>
    );
  }

  return (
    <div className="flex-1 overflow-y-auto p-4 space-y-3">
      {messages.map((msg) => {
        const isOwn = msg.username === currentUser;
        return (
          <div
            key={msg.messageId}
            className={`flex flex-col ${isOwn ? "items-end" : "items-start"}`}
          >
            <span className="text-xs text-gray-400 mb-1 px-1">{msg.username}</span>
            <div
              className={`max-w-xs md:max-w-md px-4 py-2 rounded-2xl text-sm break-words ${
                isOwn
                  ? "bg-blue-600 text-white rounded-br-sm"
                  : "bg-gray-100 text-gray-900 rounded-bl-sm"
              }`}
            >
              {msg.content}
            </div>
            <span className="text-xs text-gray-300 mt-1 px-1">
              {new Date(msg.sentAt).toLocaleTimeString([], {
                hour: "2-digit",
                minute: "2-digit",
              })}
            </span>
          </div>
        );
      })}
      <div ref={bottomRef} />
    </div>
  );
}
