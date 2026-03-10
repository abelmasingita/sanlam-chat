"use client";

import { useState } from "react";
import { useRouter } from "next/navigation";

export default function Home() {
  const router = useRouter();
  const [username, setUsername] = useState("");

  function handleJoin() {
    const trimmed = username.trim();
    if (!trimmed) return;
    router.push(`/chat?username=${encodeURIComponent(trimmed)}`);
  }

  return (
    <div className="min-h-screen flex items-center justify-center bg-gray-50">
      <div className="bg-white rounded-2xl shadow-md p-10 w-full max-w-sm flex flex-col gap-6">
        <div className="text-center">
          <h1 className="text-2xl font-bold text-gray-900">ChatApp</h1>
          <p className="text-sm text-gray-500 mt-1">Enter a username to join the chat</p>
        </div>

        <div className="flex flex-col gap-3">
          <input
            type="text"
            value={username}
            onChange={(e) => setUsername(e.target.value)}
            onKeyDown={(e) => e.key === "Enter" && handleJoin()}
            placeholder="Your name"
            maxLength={100}
            className="rounded-xl border border-gray-300 px-4 py-3 text-sm outline-none focus:border-blue-500 transition-colors"
            autoFocus
          />
          <button
            onClick={handleJoin}
            disabled={!username.trim()}
            className="bg-blue-600 text-white py-3 rounded-xl text-sm font-semibold hover:bg-blue-700 disabled:opacity-50 disabled:cursor-not-allowed transition-colors"
          >
            Join Chat
          </button>
        </div>
      </div>
    </div>
  );
}
