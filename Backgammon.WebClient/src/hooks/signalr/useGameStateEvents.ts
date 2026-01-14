import { useEffect, useRef } from 'react'
import { useNavigate } from 'react-router-dom'
import type { HubConnection } from '@microsoft/signalr'
import type { GameState } from '@/types/generated/Backgammon.Server.Models'
import { HubEvents } from '@/types/signalr.types'
import { useGameStore } from '@/stores/gameStore'
import { parseGameContextFromPath } from '@/hooks/useGameContext'
import { useGameAudio } from './useGameAudio'

/**
 * Hook to handle core game state events: GameUpdate, GameStart, GameOver, SpectatorJoined
 */
export function useGameStateEvents(connection: HubConnection | null) {
  const navigate = useNavigate()
  const navigateRef = useRef(navigate)

  const {
    setGameState,
    setIsSpectator,
    setCurrentGameId,
    setShowGameResultModal,
    setLastGameResult,
  } = useGameStore()

  const {
    playGameSounds,
    playGameOverSound,
    updatePrevGameState,
  } = useGameAudio()

  // Keep navigateRef in sync with navigate
  useEffect(() => {
    navigateRef.current = navigate
  }, [navigate])

  useEffect(() => {
    if (!connection) return

    // SpectatorJoined - User joins as observer
    const handleSpectatorJoined = (gameState: GameState) => {
      console.log('[SignalR] SpectatorJoined', gameState.gameId)
      setGameState(gameState)
      setIsSpectator(true)
      setCurrentGameId(gameState.gameId)
      updatePrevGameState(gameState)
      if (gameState.matchId) {
        navigateRef.current(`/match/${gameState.matchId}/game/${gameState.gameId}`)
      }
    }

    // GameUpdate - Board state changes
    const handleGameUpdate = (gameState: GameState) => {
      console.log('[SignalR] ========== GAME UPDATE RECEIVED ==========')
      console.log('[SignalR] Game ID:', gameState.gameId)
      console.log('[SignalR] Current Player:', gameState.currentPlayer)
      console.log('[SignalR] Is Your Turn:', gameState.isYourTurn)
      console.log('[SignalR] =================================================')

      // Check if update is for the current game we're viewing
      const context = parseGameContextFromPath(window.location.pathname)

      // If we're on a different game page, ignore this update (it's from an old game)
      if (context.gameId && context.gameId !== gameState.gameId) {
        console.log('[SignalR] Ignoring GameUpdate for old game:', gameState.gameId, 'current game:', context.gameId)
        return
      }

      // Detect and play sounds BEFORE updating state
      playGameSounds(gameState)

      setGameState(gameState)
      setCurrentGameId(gameState.gameId)
    }

    // GameStart - Both players connected, game starts
    const handleGameStart = (gameState: GameState) => {
      console.log('[SignalR] ========== GAME START RECEIVED ==========')
      console.log('[SignalR] Game ID:', gameState.gameId)
      console.log('[SignalR] Current Player:', gameState.currentPlayer)
      console.log('[SignalR] Is Your Turn:', gameState.isYourTurn)
      console.log('[SignalR] ================================================')

      const context = parseGameContextFromPath(window.location.pathname)

      // For match games, allow GameStart for new games in the same match (continuation)
      if (context.gameId && context.gameId !== gameState.gameId) {
        // Check if this is a match continuation (same match, different game)
        const isSameMatchDifferentGame = context.matchId &&
          gameState.matchId &&
          context.matchId === gameState.matchId &&
          context.gameId !== gameState.gameId

        if (!isSameMatchDifferentGame) {
          console.log('[SignalR] Ignoring GameStart for unrelated game:', gameState.gameId, 'current game:', context.gameId)
          return
        }

        console.log('[SignalR] GameStart for next game in match:', gameState.gameId, 'previous game:', context.gameId)
      }

      setGameState(gameState)
      setCurrentGameId(gameState.gameId)
      updatePrevGameState(gameState)

      // Close game result modal if open (for match continuation flow)
      setShowGameResultModal(false)

      // Navigate to the new game if it's different from current URL
      if (gameState.matchId && context.gameId !== gameState.gameId) {
        console.log('[SignalR] Navigating to new match game:', gameState.gameId)
        navigateRef.current(`/match/${gameState.matchId}/game/${gameState.gameId}`, { replace: true })
      } else if (!context.isOnGamePage && !context.isOnAnalysisPage && gameState.matchId) {
        console.log('[SignalR] Navigating to match game:', gameState.gameId)
        navigateRef.current(`/match/${gameState.matchId}/game/${gameState.gameId}`)
      }
    }

    // GameOver - Game completed
    const handleGameOver = (gameState: GameState) => {
      console.log('[SignalR] GameOver', { winner: gameState.winner, winType: gameState.winType })

      // Play win/loss sound
      playGameOverSound(gameState.yourColor, gameState.winner)

      setGameState(gameState)
      updatePrevGameState(gameState)

      // Calculate points from win type and cube value
      const points = gameState.winType === 'Gammon' ? 2 * (gameState.doublingCubeValue || 1)
        : gameState.winType === 'Backgammon' ? 3 * (gameState.doublingCubeValue || 1)
        : (gameState.doublingCubeValue || 1)

      setLastGameResult(gameState.winner ?? null, points)
      setShowGameResultModal(true)
    }

    // Register handlers
    connection.on(HubEvents.SpectatorJoined, handleSpectatorJoined)
    connection.on(HubEvents.GameUpdate, handleGameUpdate)
    connection.on(HubEvents.GameStart, handleGameStart)
    connection.on(HubEvents.GameOver, handleGameOver)

    return () => {
      connection.off(HubEvents.SpectatorJoined)
      connection.off(HubEvents.GameUpdate)
      connection.off(HubEvents.GameStart)
      connection.off(HubEvents.GameOver)
    }
  }, [
    connection,
    setGameState,
    setIsSpectator,
    setCurrentGameId,
    setShowGameResultModal,
    setLastGameResult,
    playGameSounds,
    playGameOverSound,
    updatePrevGameState,
  ])
}
