import * as signalR from '@microsoft/signalr'
import { HubConnection, HubConnectionState } from '@microsoft/signalr'
import { ServerConfig } from '@/types/game.types'
import { authService } from './auth.service'

class SignalRService {
  private connection: HubConnection | null = null
  private serverUrl: string = ''
  private reconnectAttempts = 0
  private maxReconnectAttempts = 6

  async initialize(): Promise<HubConnection> {
    try {
      // Fetch server URL from /api/config (Aspire service discovery)
      const response = await fetch('/api/config')
      const config: ServerConfig = await response.json()
      this.serverUrl = config.signalrUrl

      console.log('[SignalR] Server URL from config:', this.serverUrl)
    } catch (error) {
      console.error('[SignalR] Failed to fetch config, using fallback:', error)
      this.serverUrl = 'http://localhost:5000/gamehub'
    }

    // Create connection
    const connectionBuilder = new signalR.HubConnectionBuilder()
      .withUrl(this.serverUrl, {
        accessTokenFactory: () => {
          const token = authService.getToken()
          return token || ''
        },
        withCredentials: false,
      })
      .withAutomaticReconnect({
        nextRetryDelayInMilliseconds: (retryContext) => {
          // Retry delays: 0, 1s, 2s, 5s, 10s, 30s
          const delays = [0, 1000, 2000, 5000, 10000, 30000]
          const delay = delays[Math.min(retryContext.previousRetryCount, delays.length - 1)]
          console.log(`[SignalR] Reconnect attempt ${retryContext.previousRetryCount + 1}, delay: ${delay}ms`)
          return delay
        },
      })
      .configureLogging(signalR.LogLevel.Information)

    // Set timeouts
    this.connection = connectionBuilder.build()
    this.connection.serverTimeoutInMilliseconds = 60000 // 60 seconds
    this.connection.keepAliveIntervalInMilliseconds = 15000 // 15 seconds

    // Setup connection state handlers
    this.setupConnectionHandlers()

    return this.connection
  }

  private setupConnectionHandlers(): void {
    if (!this.connection) return

    this.connection.onreconnecting((error) => {
      console.warn('[SignalR] Connection lost, reconnecting...', error)
      this.reconnectAttempts++
    })

    this.connection.onreconnected((connectionId) => {
      console.log('[SignalR] Reconnected successfully!', connectionId)
      this.reconnectAttempts = 0
    })

    this.connection.onclose((error) => {
      console.error('[SignalR] Connection closed', error)

      if (this.reconnectAttempts < this.maxReconnectAttempts) {
        console.log('[SignalR] Attempting manual reconnect...')
        setTimeout(() => this.start(), 5000)
      } else {
        console.error('[SignalR] Max reconnect attempts reached')
      }
    })
  }

  async start(): Promise<void> {
    if (!this.connection) {
      throw new Error('[SignalR] Connection not initialized. Call initialize() first.')
    }

    if (this.connection.state === HubConnectionState.Connected) {
      console.log('[SignalR] Already connected')
      return
    }

    try {
      await this.connection.start()
      console.log('[SignalR] Connected successfully!')
      this.reconnectAttempts = 0
    } catch (error) {
      console.error('[SignalR] Failed to connect:', error)
      throw error
    }
  }

  async stop(): Promise<void> {
    if (this.connection && this.connection.state === HubConnectionState.Connected) {
      await this.connection.stop()
      console.log('[SignalR] Connection stopped')
    }
  }

  getConnection(): HubConnection | null {
    return this.connection
  }

  isConnected(): boolean {
    return this.connection?.state === HubConnectionState.Connected
  }

  getConnectionState(): string {
    return this.connection?.state || 'Disconnected'
  }

  // Helper method for invoking hub methods with error handling
  async invoke<T = void>(methodName: string, ...args: unknown[]): Promise<T | null> {
    if (!this.connection || !this.isConnected()) {
      console.error('[SignalR] Cannot invoke method - not connected')
      throw new Error('SignalR connection not available')
    }

    try {
      console.log(`[SignalR] Invoking ${methodName}`, args)
      const result = await this.connection.invoke<T>(methodName, ...args)
      console.log(`[SignalR] ${methodName} completed`, result)
      return result
    } catch (error) {
      console.error(`[SignalR] Error invoking ${methodName}:`, error)
      throw error
    }
  }

  // Register event handler
  on(eventName: string, callback: (...args: unknown[]) => void): void {
    if (!this.connection) {
      console.warn('[SignalR] Cannot register event handler - connection not initialized')
      return
    }

    this.connection.on(eventName, callback)
    console.log(`[SignalR] Registered handler for event: ${eventName}`)
  }

  // Unregister event handler
  off(eventName: string, callback?: (...args: unknown[]) => void): void {
    if (!this.connection) return

    if (callback) {
      this.connection.off(eventName, callback)
    } else {
      this.connection.off(eventName)
    }

    console.log(`[SignalR] Unregistered handler for event: ${eventName}`)
  }
}

export const signalRService = new SignalRService()
