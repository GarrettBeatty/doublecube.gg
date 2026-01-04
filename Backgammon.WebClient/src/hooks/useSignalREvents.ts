import { useEffect, useRef } from 'react'
import { useNavigate } from 'react-router-dom'
import { useSignalR } from '@/contexts/SignalRContext'
import { useGameStore } from '@/stores/gameStore'
import { audioService } from '@/services/audio.service'
import { GameState, CheckerColor } from '@/types/game.types'
import { HubEvents, MatchData, TimeUpdateEvent, TimeoutEvent } from '@/types/signalr.types'

export const useSignalREvents = () => {
  const navigate = useNavigate()
  const { connection } = useSignalR()
  const {
    setGameState,
    updateTimeState,
    setIsSpectator,
    setCurrentGameId,
    addChatMessage,
  } = useGameStore()
  const prevGameStateRef = useRef<GameState | null>(null)
  const navigateRef = useRef(navigate)

  // Keep navigateRef in sync with navigate
  useEffect(() => {
    navigateRef.current = navigate
  }, [navigate])

  useEffect(() => {
    if (!connection) return

    console.log('[useSignalREvents] Registering event handlers')

    // SpectatorJoined - User joins as observer
    connection.on(HubEvents.SpectatorJoined, (gameState: GameState) => {
      console.log('[SignalR] SpectatorJoined', gameState.gameId)
      setGameState(gameState)
      setIsSpectator(true)
      setCurrentGameId(gameState.gameId)
      prevGameStateRef.current = gameState
      navigateRef.current(`/game/${gameState.gameId}`)
    })

    // GameUpdate - Board state changes
    connection.on(HubEvents.GameUpdate, (gameState: GameState) => {
      console.log('[SignalR] GameUpdate', {
        gameId: gameState.gameId,
        currentPlayer: gameState.currentPlayer,
        isYourTurn: gameState.isYourTurn,
      })

      // Check if update is for the current game we're viewing
      const currentPath = window.location.pathname
      const isOnGamePage = currentPath.startsWith('/game/')
      const currentGameIdFromUrl = isOnGamePage ? currentPath.split('/game/')[1] : null

      // If we're on a different game page, ignore this update (it's from an old game)
      if (currentGameIdFromUrl && currentGameIdFromUrl !== gameState.gameId) {
        console.log('[SignalR] Ignoring GameUpdate for old game:', gameState.gameId, 'current game:', currentGameIdFromUrl)
        return
      }

      // Detect and play sounds BEFORE updating state
      const prevState = prevGameStateRef.current
      if (prevState) {
        // Turn change detection
        if (prevState.currentPlayer !== gameState.currentPlayer) {
          audioService.playSound('turn-change')
        }

        // Move detection (simplified - could be enhanced)
        const prevBornOff =
          prevState.whiteBornOff + prevState.redBornOff
        const newBornOff = gameState.whiteBornOff + gameState.redBornOff
        const prevBar =
          prevState.whiteCheckersOnBar + prevState.redCheckersOnBar
        const newBar = gameState.whiteCheckersOnBar + gameState.redCheckersOnBar

        if (newBornOff > prevBornOff) {
          audioService.playSound('bear-off')
        } else if (newBar > prevBar) {
          audioService.playSound('checker-hit')
        } else if (prevBornOff !== newBornOff || prevBar !== newBar) {
          audioService.playSound('checker-move')
        }
      }

      setGameState(gameState)
      setCurrentGameId(gameState.gameId)
      prevGameStateRef.current = gameState
    })

    // GameStart - Both players connected, game starts
    connection.on(HubEvents.GameStart, (gameState: GameState) => {
      console.log('[SignalR] GameStart', gameState.gameId)

      // Check if we're on a game page and if it's for a different game
      const currentPath = window.location.pathname
      const isOnGamePage = currentPath.startsWith('/game/')
      const currentGameIdFromUrl = isOnGamePage ? currentPath.split('/game/')[1] : null

      // If we're on a different game page, ignore this GameStart (it's from an old game)
      if (currentGameIdFromUrl && currentGameIdFromUrl !== gameState.gameId) {
        console.log('[SignalR] Ignoring GameStart for old game:', gameState.gameId, 'current game:', currentGameIdFromUrl)
        return
      }

      setGameState(gameState)
      setCurrentGameId(gameState.gameId)
      prevGameStateRef.current = gameState

      // Only navigate if we're not already on a game page
      if (!isOnGamePage) {
        navigateRef.current(`/game/${gameState.gameId}`)
      }
    })

    // GameOver - Game completed
    connection.on(
      HubEvents.GameOver,
      (winner: CheckerColor, points: number, gameState: GameState) => {
        console.log('[SignalR] GameOver', { winner, points })

        // Play win/loss sound
        if (gameState.yourColor === winner) {
          audioService.playSound('game-won')
        } else {
          audioService.playSound('game-lost')
        }

        setGameState(gameState)
        prevGameStateRef.current = gameState
      }
    )

    // WaitingForOpponent - Game created, waiting for opponent
    connection.on(HubEvents.WaitingForOpponent, (gameId: string) => {
      console.log('[SignalR] WaitingForOpponent', gameId)
      setCurrentGameId(gameId)
      navigateRef.current(`/game/${gameId}`)
    })

    // OpponentJoined
    connection.on(HubEvents.OpponentJoined, (opponentId: string) => {
      console.log('[SignalR] OpponentJoined', opponentId)
    })

    // OpponentLeft
    connection.on(HubEvents.OpponentLeft, () => {
      console.log('[SignalR] OpponentLeft')
    })

    // DoubleOffered - Opponent offers to double stakes
    connection.on(
      HubEvents.DoubleOffered,
      (currentStakes: number, newStakes: number) => {
        console.log('[SignalR] DoubleOffered', { currentStakes, newStakes })
        audioService.playSound('double-offer')

        // TODO: Show double offer modal
        // For now, log to console
      }
    )

    // DoubleAccepted - Double was accepted
    connection.on(HubEvents.DoubleAccepted, (gameState: GameState) => {
      console.log('[SignalR] DoubleAccepted', gameState.doublingCubeValue)
      setGameState(gameState)
    })

    // ReceiveChatMessage - Chat message from opponent
    connection.on(
      HubEvents.ReceiveChatMessage,
      (senderName: string, message: string, senderConnectionId: string) => {
        const isOwn = senderConnectionId === connection.connectionId
        const displayName = isOwn ? 'You' : senderName

        // Play sound for incoming messages (not our own)
        if (!isOwn) {
          audioService.playSound('chat-message')
        }

        addChatMessage({
          senderName: displayName,
          message,
          timestamp: new Date(),
          isOwn,
        })
      }
    )

    // Error - Server error message
    connection.on(HubEvents.Error, (errorMessage: string) => {
      console.error('[SignalR] Error:', errorMessage)
      // TODO: Show error toast
    })

    // Info - Server info message
    connection.on(HubEvents.Info, (infoMessage: string) => {
      console.log('[SignalR] Info:', infoMessage)
      // TODO: Show info toast
    })

    // MatchCreated - Match and first game created, navigate to game page
    connection.on(HubEvents.MatchCreated, (data: MatchData) => {
      console.log('[SignalR] MatchCreated', { matchId: data.matchId, gameId: data.gameId, opponentType: data.opponentType })
      setCurrentGameId(data.gameId as string)
      // Navigate to game page immediately - game handles waiting state
      navigateRef.current(`/game/${data.gameId}`, { replace: true })
    })

    // OpponentJoinedMatch - Opponent joined the match
    connection.on(HubEvents.OpponentJoinedMatch, (data: MatchData) => {
      console.log('[SignalR] OpponentJoinedMatch', { matchId: data.matchId, player2Name: data.player2Name })
      // Game page will receive WaitingForOpponent → GameStart flow
    })

    // MatchGameStarting - Match game starting, navigate to game
    connection.on(HubEvents.MatchGameStarting, (data: MatchData) => {
      console.log('[SignalR] MatchGameStarting', { matchId: data.matchId, gameId: data.gameId })
      setCurrentGameId(data.gameId as string)
      // Force navigation even if on same route
      navigateRef.current(`/game/${data.gameId}`, { replace: true })
    })

    // TimeUpdate - Server broadcasts updated time state every second
    connection.on(HubEvents.TimeUpdate, (timeUpdate: TimeUpdateEvent) => {
      const currentPath = window.location.pathname
      const isOnGamePage = currentPath.startsWith('/game/')
      const currentGameIdFromUrl = isOnGamePage ? currentPath.split('/game/')[1] : null

      // Ignore time updates for games we're not viewing
      if (currentGameIdFromUrl && currentGameIdFromUrl !== timeUpdate.gameId) {
        return
      }

      // Update game state with new time values
      updateTimeState({
        whiteReserveSeconds: timeUpdate.whiteReserveSeconds,
        redReserveSeconds: timeUpdate.redReserveSeconds,
        whiteIsInDelay: timeUpdate.whiteIsInDelay,
        redIsInDelay: timeUpdate.redIsInDelay,
        whiteDelayRemaining: timeUpdate.whiteDelayRemaining,
        redDelayRemaining: timeUpdate.redDelayRemaining,
      })
    })

    // PlayerTimedOut - Player ran out of time and lost
    connection.on(HubEvents.PlayerTimedOut, (timeoutEvent: TimeoutEvent) => {
      console.log('[SignalR] PlayerTimedOut', timeoutEvent)
      // TODO: Show toast notification about timeout
      audioService.playSound('game-lost')
    })

    // Cleanup on unmount
    return () => {
      console.log('[useSignalREvents] Cleaning up event handlers')
      connection.off(HubEvents.SpectatorJoined)
      connection.off(HubEvents.GameUpdate)
      connection.off(HubEvents.GameStart)
      connection.off(HubEvents.GameOver)
      connection.off(HubEvents.WaitingForOpponent)
      connection.off(HubEvents.OpponentJoined)
      connection.off(HubEvents.OpponentLeft)
      connection.off(HubEvents.DoubleOffered)
      connection.off(HubEvents.DoubleAccepted)
      connection.off(HubEvents.ReceiveChatMessage)
      connection.off(HubEvents.Error)
      connection.off(HubEvents.Info)
      connection.off(HubEvents.MatchCreated)
      connection.off(HubEvents.OpponentJoinedMatch)
      connection.off(HubEvents.MatchGameStarting)
      connection.off(HubEvents.TimeUpdate)
      connection.off(HubEvents.PlayerTimedOut)
    }
  }, [connection, setGameState, updateTimeState, setIsSpectator, setCurrentGameId, addChatMessage])

  return { connection }
}
