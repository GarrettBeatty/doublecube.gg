import { useEffect } from 'react'
import type { HubConnection } from '@microsoft/signalr'
import type { GameState } from '@/types/generated/Backgammon.Server.Models'
import type { DoubleOfferDto } from '@/types/generated/Backgammon.Server.Models.SignalR'
import { CheckerColor } from '@/types/generated/Backgammon.Core'
import { HubEvents } from '@/types/signalr.types'
import { useGameStore } from '@/stores/gameStore'
import { useGameAudio } from './useGameAudio'

/**
 * Hook to handle doubling cube events: DoubleOffered, DoubleAccepted
 */
export function useDoubleEvents(connection: HubConnection | null) {
  const { myColor, setPendingDoubleOffer, setGameState } = useGameStore()
  const { playDoubleOfferSound } = useGameAudio()

  useEffect(() => {
    if (!connection) return

    // DoubleOffered - Opponent offers to double stakes
    const handleDoubleOffered = (offer: DoubleOfferDto) => {
      console.log('[SignalR] DoubleOffered', offer)
      playDoubleOfferSound()

      // Determine which player offered the double (opposite of your color)
      const offerFrom = myColor === CheckerColor.White ? CheckerColor.Red : CheckerColor.White

      // Update store to show the response modal
      setPendingDoubleOffer(offerFrom, offer.newStakes)
    }

    // DoubleAccepted - Double was accepted
    const handleDoubleAccepted = (gameState: GameState) => {
      console.log('[SignalR] DoubleAccepted', gameState.doublingCubeValue)
      setGameState(gameState)
    }

    // Register handlers
    connection.on(HubEvents.DoubleOffered, handleDoubleOffered)
    connection.on(HubEvents.DoubleAccepted, handleDoubleAccepted)

    return () => {
      connection.off(HubEvents.DoubleOffered)
      connection.off(HubEvents.DoubleAccepted)
    }
  }, [connection, myColor, setPendingDoubleOffer, setGameState, playDoubleOfferSound])
}
