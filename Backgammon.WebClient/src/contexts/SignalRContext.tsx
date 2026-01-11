/* eslint-disable react-refresh/only-export-components */
import React, { createContext, useContext, useState, useEffect, ReactNode, useMemo } from 'react'
import { HubConnection } from '@microsoft/signalr'
import { signalRService } from '@/services/signalr.service'
import { ConnectionState } from '@/types/signalr.types'
import { useAuth } from './AuthContext'
import { getHubProxyFactory } from '@/types/generated/TypedSignalR.Client'
import type { IGameHub } from '@/types/generated/TypedSignalR.Client/Backgammon.Server.Hubs.Interfaces'

interface SignalRContextType {
  connection: HubConnection | null
  connectionState: ConnectionState
  isConnected: boolean
  hub: IGameHub | null
}

const SignalRContext = createContext<SignalRContextType | undefined>(undefined)

export const useSignalR = () => {
  const context = useContext(SignalRContext)
  if (!context) {
    throw new Error('useSignalR must be used within a SignalRProvider')
  }
  return context
}

interface SignalRProviderProps {
  children: ReactNode
}

export const SignalRProvider: React.FC<SignalRProviderProps> = ({ children }) => {
  const { isReady: authReady } = useAuth()
  const [connection, setConnection] = useState<HubConnection | null>(null)
  const [connectionState, setConnectionState] = useState<ConnectionState>(
    ConnectionState.Disconnected
  )

  // Create typed hub proxy when connection is available
  const hub = useMemo<IGameHub | null>(() => {
    if (!connection || connectionState !== ConnectionState.Connected) {
      return null
    }
    return getHubProxyFactory('IGameHub').createHubProxy(connection)
  }, [connection, connectionState])

  useEffect(() => {
    // CRITICAL: Wait for authentication to complete before connecting to SignalR
    // This ensures JWT with displayName exists and user is in database
    if (!authReady) {
      console.log('[SignalRContext] Waiting for authentication to complete...')
      return
    }

    console.log('[SignalRContext] Authentication ready, initializing SignalR connection...')

    const initializeConnection = async () => {
      try {
        setConnectionState(ConnectionState.Connecting)

        // Initialize SignalR service (JWT will be attached automatically from localStorage)
        const conn = await signalRService.initialize()
        setConnection(conn)

        // Setup connection state change handlers
        conn.onreconnecting(() => {
          console.log('[SignalRContext] Reconnecting...')
          setConnectionState(ConnectionState.Reconnecting)
        })

        conn.onreconnected(() => {
          console.log('[SignalRContext] Reconnected')
          setConnectionState(ConnectionState.Connected)
        })

        conn.onclose(() => {
          console.log('[SignalRContext] Connection closed')
          setConnectionState(ConnectionState.Disconnected)
        })

        // Start the connection with JWT
        await signalRService.start()
        setConnectionState(ConnectionState.Connected)

        console.log('[SignalRContext] SignalR connection established successfully')
      } catch (error) {
        console.error('[SignalRContext] Failed to initialize connection:', error)
        setConnectionState(ConnectionState.Failed)
      }
    }

    initializeConnection()

    // Cleanup on unmount
    return () => {
      signalRService.stop()
    }
  }, [authReady]) // Re-run when auth becomes ready

  const value: SignalRContextType = {
    connection,
    connectionState,
    isConnected: connectionState === ConnectionState.Connected,
    hub,
  }

  return <SignalRContext.Provider value={value}>{children}</SignalRContext.Provider>
}
