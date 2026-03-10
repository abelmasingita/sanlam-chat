import * as signalR from "@microsoft/signalr";

export const API_URL = process.env.NEXT_PUBLIC_API_URL ?? "https://localhost:7190";

// Singleton - one HubConnection instance is shared for the lifetime of the page.
let connection: signalR.HubConnection | null = null;

export function getConnection(): signalR.HubConnection {
  if (!connection) {
    connection = new signalR.HubConnectionBuilder()
      .withUrl(`${API_URL}/hubs/chat`)
      .withAutomaticReconnect()
      .configureLogging(signalR.LogLevel.Warning)
      .build();
  }
  return connection;
}

// Called on cleanup so the next mount gets a fresh connection instead of reusing a stopped one.
export function resetConnection(): void {
  connection = null;
}
