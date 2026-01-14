import { useEffect } from 'react'
import type { HubConnection } from '@microsoft/signalr'
import { HubEvents } from '@/types/signalr.types'
import { useGameStore } from '@/stores/gameStore'
import { useToast } from '@/hooks/use-toast'

/**
 * Hook to handle connection and misc events: WaitingForOpponent, OpponentJoined, OpponentLeft, Error, Info
 */
export function useConnectionEvents(connection: HubConnection | null) {
  const { setCurrentGameId } = useGameStore()
  const { toast } = useToast()

  useEffect(() => {
    if (!connection) return

    // WaitingForOpponent - Game created, waiting for opponent
    // Note: This is for legacy non-match games - matches use MatchCreated event
    const handleWaitingForOpponent = (gameId: string) => {
      console.log('[SignalR] WaitingForOpponent', gameId)
      setCurrentGameId(gameId)
    }

    // OpponentJoined
    const handleOpponentJoined = (opponentId: string) => {
      console.log('[SignalR] OpponentJoined', opponentId)
    }

    // OpponentLeft
    const handleOpponentLeft = () => {
      console.log('[SignalR] OpponentLeft')
    }

    // Error - Server error message
    const handleError = (errorMessage: string) => {
      console.error('[SignalR] Error:', errorMessage)
      toast({
        title: 'Error',
        description: errorMessage,
        variant: 'destructive',
      })
    }

    // Info - Server info message
    const handleInfo = (infoMessage: string) => {
      console.log('[SignalR] Info:', infoMessage)
      toast({
        title: 'Info',
        description: infoMessage,
      })
    }

    // Register handlers
    connection.on(HubEvents.WaitingForOpponent, handleWaitingForOpponent)
    connection.on(HubEvents.OpponentJoined, handleOpponentJoined)
    connection.on(HubEvents.OpponentLeft, handleOpponentLeft)
    connection.on(HubEvents.Error, handleError)
    connection.on(HubEvents.Info, handleInfo)

    return () => {
      connection.off(HubEvents.WaitingForOpponent)
      connection.off(HubEvents.OpponentJoined)
      connection.off(HubEvents.OpponentLeft)
      connection.off(HubEvents.Error)
      connection.off(HubEvents.Info)
    }
  }, [connection, setCurrentGameId, toast])
}
