"use client";

import { useRouter, useSearchParams } from "next/navigation";
import { Suspense } from "react";
import MessageList from "@/components/MessageList";
import MessageInput from "@/components/MessageInput";
import { useChat } from "@/hooks/useChat";

function ChatRoom() {
  const router = useRouter();
  const params = useSearchParams();
  const username =
    params.get("username")?.trim() ||
    (typeof window !== "undefined" ? sessionStorage.getItem("username") ?? "" : "");

  // Redirect to join page if no username in query string or sessionStorage
  if (!username) {
    router.replace("/");
    return null;
  }

  const { messages, connected, connectionId, error, sendMessage } = useChat(username);

  return (
    <div className="flex flex-col h-screen bg-white">
      {/* Header */}
      <div className="flex items-center justify-between px-6 py-4 border-b border-gray-200 bg-white shadow-sm">
        <div className="flex items-center gap-3">
          <div
            className={`w-2.5 h-2.5 rounded-full ${
              connected ? "bg-green-500" : "bg-gray-300"
            }`}
          />
          <h1 className="text-lg font-semibold text-gray-900">ChatApp</h1>
        </div>
        <div className="flex items-center gap-4">
          <span className="text-sm text-gray-500">
            Signed in as <span className="font-medium text-gray-800">{username}</span>
          </span>
          <button
            onClick={() => router.push("/")}
            className="text-sm text-gray-400 hover:text-gray-600 transition-colors"
          >
            Leave
          </button>
        </div>
      </div>

      {/* Error banner */}
      {error && (
        <div className="bg-red-50 text-red-600 text-sm px-6 py-2 text-center">
          {error}
        </div>
      )}

      {/* Messages */}
      <MessageList messages={messages} connectionId={connectionId} />

      {/* Input */}
      <MessageInput onSend={sendMessage} disabled={!connected} />
    </div>
  );
}

export default function ChatPage() {
  return (
    <Suspense>
      <ChatRoom />
    </Suspense>
  );
}
