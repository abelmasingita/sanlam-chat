'use client'

import { useEffect, useRef, useState, useCallback } from 'react'
import * as signalR from '@microsoft/signalr'
import { getConnection, resetConnection } from '@/lib/signalr'
import type { MessageDto } from '@/types'

const API_URL = process.env.NEXT_PUBLIC_API_URL!

export function useChat(username: string) {
  const [messages, setMessages] = useState<MessageDto[]>([])
  const [connected, setConnected] = useState(false)
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
      await connectionRef.current.invoke('SendMessage', { username, content })
    },
    [username],
  )

  return { messages, connected, error, sendMessage }
}
