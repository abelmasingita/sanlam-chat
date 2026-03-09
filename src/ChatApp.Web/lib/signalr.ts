import * as signalR from "@microsoft/signalr";

const API_URL = process.env.NEXT_PUBLIC_API_URL ?? "http://localhost:5111";

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

export function resetConnection(): void {
  connection = null;
}
