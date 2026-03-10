'use client'

import { useEffect, useRef, useState, useCallback } from 'react'
import * as signalR from '@microsoft/signalr'
import { getConnection, resetConnection, API_URL } from '@/lib/signalr'
import type { MessageDto } from '@/types'

// Manages the SignalR connection lifecycle, message history, and sending.
// Returns the current message list, connection state, error, and sendMessage function.
export function useChat(username: string) {
  const [messages, setMessages] = useState<MessageDto[]>([])
  const [connected, setConnected] = useState(false)
  const [connectionId, setConnectionId] = useState<string | null>(null)
  const [error, setError] = useState<string | null>(null)
  const connectionRef = useRef<signalR.HubConnection | null>(null)

  useEffect(() => {
    let active = true
    const conn = getConnection()
    connectionRef.current = conn

    async function start() {
      try {
        // Load recent messages first
        const res = await fetch(`${API_URL}/api/messages`)
        if (res.ok && active) {
          const recent: MessageDto[] = await res.json()
          setMessages(recent)
        }

        // Bail out if this effect was cleaned up while the fetch was in-flight.
        // Without this, the stale async continuation re-registers the handler on
        // the old connection after cleanup already removed it, causing every
        // broadcast to fire twice (duplicate key error on send).
        if (!active) return

        conn.on('ReceiveMessage', (msg: MessageDto) => {
          setMessages((prev) => [...prev, msg])
        })

        conn.onclose(() => setConnected(false))
        conn.onreconnecting(() => setConnected(false))
        conn.onreconnected(() => setConnected(true))

        if (conn.state === signalR.HubConnectionState.Disconnected) {
          await conn.start()
        }
        if (active) {
          setConnected(true)
          setConnectionId(conn.connectionId)
          setError(null)
        }
      } catch (err) {
        if (active) {
          setError('Could not connect to chat server.')
          console.error(err)
        }
      }
    }

    start()

    return () => {
      active = false
      conn.off('ReceiveMessage')
      conn.stop()
      resetConnection()
    }
  }, [])

  const sendMessage = useCallback(
    async (content: string) => {
      if (!connectionRef.current || !content.trim()) return
      try {
        await connectionRef.current.invoke('SendMessage', { username, content })
        setError(null)
      } catch (err: unknown) {
        const message = err instanceof Error ? err.message : 'Failed to send message.'
        setError(message)
      }
    },
    [username],
  )

  return { messages, connected, connectionId, error, sendMessage }
}
