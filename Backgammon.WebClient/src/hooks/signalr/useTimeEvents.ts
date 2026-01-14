import { useEffect } from 'react'
import type { HubConnection } from '@microsoft/signalr'
import { HubEvents } from '@/types/signalr.types'
import { useGameStore } from '@/stores/gameStore'
import { useToast } from '@/hooks/use-toast'
import { parseGameContextFromPath } from '@/hooks/useGameContext'
import { TimeUpdateDto, PlayerTimedOutDto } from '@/types/generated/Backgammon.Server.Models.SignalR'
import { useGameAudio } from './useGameAudio'

/**
 * Hook to handle time control events: TimeUpdate, PlayerTimedOut
 */
export function useTimeEvents(connection: HubConnection | null) {
  const { updateTimeState } = useGameStore()
  const { toast } = useToast()
  const { playTimeoutSound } = useGameAudio()

  useEffect(() => {
    if (!connection) return

    // TimeUpdate - Server broadcasts updated time state every second
    const handleTimeUpdate = (timeUpdate: TimeUpdateDto) => {
      const context = parseGameContextFromPath(window.location.pathname)

      console.log('[useTimeEvents] Received TimeUpdate:', {
        gameId: timeUpdate.gameId,
        whiteReserve: timeUpdate.whiteReserveSeconds,
        redReserve: timeUpdate.redReserveSeconds,
        whiteIsInDelay: timeUpdate.whiteIsInDelay,
        redIsInDelay: timeUpdate.redIsInDelay,
        whiteDelayRemaining: timeUpdate.whiteDelayRemaining,
        redDelayRemaining: timeUpdate.redDelayRemaining,
      })

      // Ignore time updates for games we're not viewing
      if (context.gameId && context.gameId !== timeUpdate.gameId) {
        console.log('[useTimeEvents] Ignoring - different game')
        return
      }

      console.log('[useTimeEvents] Updating time state in store')
      updateTimeState({
        whiteReserveSeconds: timeUpdate.whiteReserveSeconds,
        redReserveSeconds: timeUpdate.redReserveSeconds,
        whiteIsInDelay: timeUpdate.whiteIsInDelay,
        redIsInDelay: timeUpdate.redIsInDelay,
        whiteDelayRemaining: timeUpdate.whiteDelayRemaining,
        redDelayRemaining: timeUpdate.redDelayRemaining,
      })
    }

    // PlayerTimedOut - Player ran out of time and lost
    const handlePlayerTimedOut = (timeoutEvent: PlayerTimedOutDto) => {
      console.log('[SignalR] PlayerTimedOut', timeoutEvent)

      toast({
        title: 'Time Out!',
        description: `${timeoutEvent.timedOutPlayer} ran out of time. ${timeoutEvent.winner} wins!`,
        variant: 'destructive',
      })

      playTimeoutSound()
    }

    // Register handlers
    connection.on(HubEvents.TimeUpdate, handleTimeUpdate)
    connection.on(HubEvents.PlayerTimedOut, handlePlayerTimedOut)

    return () => {
      connection.off(HubEvents.TimeUpdate)
      connection.off(HubEvents.PlayerTimedOut)
    }
  }, [connection, updateTimeState, toast, playTimeoutSound])
}
