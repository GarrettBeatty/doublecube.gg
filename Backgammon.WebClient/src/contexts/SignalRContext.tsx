/* eslint-disable react-refresh/only-export-components */
import React, { createContext, useContext, useState, useEffect, ReactNode } from 'react'
import { HubConnection } from '@microsoft/signalr'
import { signalRService } from '@/services/signalr.service'
import { authService } from '@/services/auth.service'
import { ConnectionState } from '@/types/signalr.types'

interface SignalRContextType {
  connection: HubConnection | null
  connectionState: ConnectionState
  isConnected: boolean
  invoke: <T = void>(methodName: string, ...args: unknown[]) => Promise<T | null>
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
  const [connection, setConnection] = useState<HubConnection | null>(null)
  const [connectionState, setConnectionState] = useState<ConnectionState>(
    ConnectionState.Disconnected
  )

  useEffect(() => {
    const initializeConnection = async () => {
      try {
        setConnectionState(ConnectionState.Connecting)

        // Ensure anonymous user registration is complete before connecting
        await authService.ensureRegistered()

        // Initialize SignalR service
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

        // Start the connection
        await signalRService.start()
        setConnectionState(ConnectionState.Connected)

        console.log('[SignalRContext] Connection established')
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
  }, [])

  const invoke = async <T = void,>(
    methodName: string,
    ...args: unknown[]
  ): Promise<T | null> => {
    try {
      return await signalRService.invoke<T>(methodName, ...args)
    } catch (error) {
      console.error(`[SignalRContext] Failed to invoke ${methodName}:`, error)
      throw error
    }
  }

  const value: SignalRContextType = {
    connection,
    connectionState,
    isConnected: connectionState === ConnectionState.Connected,
    invoke,
  }

  return <SignalRContext.Provider value={value}>{children}</SignalRContext.Provider>
}
