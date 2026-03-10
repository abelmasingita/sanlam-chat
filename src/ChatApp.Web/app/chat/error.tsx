"use client";

interface Props {
  error: Error & { digest?: string };
  reset: () => void;
}

export default function ChatError({ error, reset }: Props) {
  return (
    <div className="flex flex-col items-center justify-center h-screen bg-gray-50 gap-4 p-6 text-center">
      <h2 className="text-lg font-semibold text-gray-900">Something went wrong</h2>
      <p className="text-sm text-gray-500 max-w-sm">{error.message || "An unexpected error occurred in the chat."}</p>
      <button
        onClick={reset}
        className="bg-blue-600 text-white px-5 py-2 rounded-xl text-sm font-semibold hover:bg-blue-700 transition-colors"
      >
        Try again
      </button>
    </div>
  );
}
