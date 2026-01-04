import { useEffect } from 'react'
import { useNavigate } from 'react-router-dom'
import { useSignalR } from '@/contexts/SignalRContext'
import { useGameStore } from '@/stores/gameStore'
import { audioService } from '@/services/audio.service'
import { GameState, CheckerColor } from '@/types/game.types'
import { HubEvents } from '@/types/signalr.types'

export const useSignalREvents = () => {
  const navigate = useNavigate()
  const { connection } = useSignalR()
  const {
    setGameState,
    setIsSpectator,
    setCurrentGameId,
    addChatMessage,
    currentGameState,
  } = useGameStore()

  useEffect(() => {
    if (!connection) return

    console.log('[useSignalREvents] Registering event handlers')

    // SpectatorJoined - User joins as observer
    connection.on(HubEvents.SpectatorJoined, (gameState: GameState) => {
      console.log('[SignalR] SpectatorJoined', gameState.gameId)
      setGameState(gameState)
      setIsSpectator(true)
      setCurrentGameId(gameState.gameId)
      navigate(`/game/${gameState.gameId}`)
    })

    // GameUpdate - Board state changes
    connection.on(HubEvents.GameUpdate, (gameState: GameState) => {
      console.log('[SignalR] GameUpdate', {
        gameId: gameState.gameId,
        currentPlayer: gameState.currentPlayer,
        isYourTurn: gameState.isYourTurn,
      })

      // Detect and play sounds BEFORE updating state
      if (currentGameState) {
        // Turn change detection
        if (currentGameState.currentPlayer !== gameState.currentPlayer) {
          audioService.playSound('turn-change')
        }

        // Move detection (simplified - could be enhanced)
        const prevBornOff =
          currentGameState.whiteBornOff + currentGameState.redBornOff
        const newBornOff = gameState.whiteBornOff + gameState.redBornOff
        const prevBar =
          currentGameState.whiteCheckersOnBar + currentGameState.redCheckersOnBar
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
    })

    // GameStart - Both players connected, game starts
    connection.on(HubEvents.GameStart, (gameState: GameState) => {
      console.log('[SignalR] GameStart', gameState.gameId)
      setGameState(gameState)
      setCurrentGameId(gameState.gameId)
      navigate(`/game/${gameState.gameId}`)
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
      }
    )

    // WaitingForOpponent - Game created, waiting for opponent
    connection.on(HubEvents.WaitingForOpponent, (gameId: string) => {
      console.log('[SignalR] WaitingForOpponent', gameId)
      setCurrentGameId(gameId)
      navigate(`/game/${gameId}`)
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
    }
  }, [connection, currentGameState, setGameState, setIsSpectator, setCurrentGameId, addChatMessage, navigate])

  return { connection }
}
