/**
 * Legacy useSignalREvents hook - provides backward compatibility
 *
 * This hook now composes the split SignalR event hooks.
 * For new code, prefer importing individual hooks directly from '@/hooks/signalr':
 * - useGameStateEvents - GameUpdate, GameStart, GameOver, SpectatorJoined
 * - useMatchEvents - MatchCreated, MatchUpdate, OpponentJoinedMatch
 * - useDoubleEvents - DoubleOffered, DoubleAccepted
 * - useTimeEvents - TimeUpdate, PlayerTimedOut
 * - useChatEvents - ReceiveChatMessage
 * - useConnectionEvents - WaitingForOpponent, OpponentJoined, OpponentLeft, Error, Info
 */

import { useEffect } from 'react'
import { useSignalR } from '@/contexts/SignalRContext'
import { useGameStore } from '@/stores/gameStore'
import {
  useGameStateEvents,
  useMatchEvents,
  useDoubleEvents,
  useTimeEvents,
  useChatEvents,
  useConnectionEvents,
} from '@/hooks/signalr'

export const useSignalREvents = () => {
  const { connection } = useSignalR()
  const { setShowGameResultModal, setLastGameResult } = useGameStore()

  // Register all event handlers via split hooks
  useGameStateEvents(connection)
  useMatchEvents(connection)
  useDoubleEvents(connection)
  useTimeEvents(connection)
  useChatEvents(connection)
  useConnectionEvents(connection)

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
