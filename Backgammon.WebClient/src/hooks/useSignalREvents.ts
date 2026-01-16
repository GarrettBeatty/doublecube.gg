/**
 * useSignalREvents - Type-safe SignalR event handler registration
 *
 * Uses the typed receiver pattern with getReceiverRegister() to ensure
 * compile-time type safety for all event handlers. If the server changes
 * an event signature, TypeScript will catch the mismatch at build time.
 */

import { useEffect } from 'react'
import { useSignalR } from '@/contexts/SignalRContext'
import { useGameStore } from '@/stores/gameStore'
import { useTypedSignalRReceiver } from '@/hooks/signalr'

export const useSignalREvents = () => {
  const { connection } = useSignalR()
  const { setShowGameResultModal, setLastGameResult } = useGameStore()

  // Register all event handlers via type-safe receiver
  useTypedSignalRReceiver(connection)

  // Defensive check: If page refreshed during/after game completion in a match,
  // restore the game result modal so user can continue
  useEffect(() => {
    const gameState = useGameStore.getState()
    const matchState = gameState.matchState

    // Show modal if:
    // 1. Game has ended (has a winner)
    // 2. Part of an active match (matchState exists and match not complete)
    // 3. Modal not already showing
    if (
      gameState.currentGameState?.winner &&
      matchState &&
      !matchState.matchComplete &&
      !gameState.showGameResultModal
    ) {
      // Calculate points from game state
      const currentGame = gameState.currentGameState
      const points = currentGame.winType === 'Gammon'
        ? 2 * (currentGame.doublingCubeValue || 1)
        : currentGame.winType === 'Backgammon'
        ? 3 * (currentGame.doublingCubeValue || 1)
        : (currentGame.doublingCubeValue || 1)

      setLastGameResult(currentGame.winner ?? null, points)
      setShowGameResultModal(true)
    }
  }, [setShowGameResultModal, setLastGameResult])

  return { connection }
}
