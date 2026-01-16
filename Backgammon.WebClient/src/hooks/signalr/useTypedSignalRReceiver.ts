import { useEffect, useRef } from 'react'
import { useNavigate } from 'react-router-dom'
import type { HubConnection } from '@microsoft/signalr'
import { getReceiverRegister } from '@/types/generated/TypedSignalR.Client'
import type { IGameHubClient } from '@/types/generated/TypedSignalR.Client/Backgammon.Server.Hubs.Interfaces'
import type { GameState } from '@/types/generated/Backgammon.Server.Models'
import { CheckerColor } from '@/types/generated/Backgammon.Core'
import { useGameStore } from '@/stores/gameStore'
import { useToast } from '@/hooks/use-toast'
import { parseGameContextFromPath } from '@/hooks/useGameContext'
import { audioService } from '@/services/audio.service'

/**
 * Type-safe SignalR event receiver hook.
 *
 * Uses getReceiverRegister() to enforce compile-time type safety for all event handlers.
 * If the server changes an event signature, TypeScript will catch the mismatch at build time.
 */
export function useTypedSignalRReceiver(connection: HubConnection | null) {
  const navigate = useNavigate()
  const { toast } = useToast()

  // Get store actions
  const {
    setGameState,
    setCurrentGameId,
    setIsSpectator,
    setShowGameResultModal,
    setLastGameResult,
    setMatchState,
    setPendingDoubleOffer,
    updateTimeState,
    addChatMessage,
    clearChatMessages,
    myColor,
  } = useGameStore()

  // Track previous game state for sound detection
  const prevGameStateRef = useRef<GameState | null>(null)

  // Use refs to avoid stale closures in the receiver
  const navigateRef = useRef(navigate)
  const toastRef = useRef(toast)
  const myColorRef = useRef(myColor)
  const connectionRef = useRef(connection)

  // Keep refs in sync
  useEffect(() => {
    navigateRef.current = navigate
  }, [navigate])

  useEffect(() => {
    toastRef.current = toast
  }, [toast])

  useEffect(() => {
    myColorRef.current = myColor
  }, [myColor])

  useEffect(() => {
    connectionRef.current = connection
  }, [connection])

  useEffect(() => {
    if (!connection) return

    // Helper: Play game sounds based on state transitions
    const playGameSounds = (newState: GameState) => {
      const prevState = prevGameStateRef.current
      if (prevState) {
        // Dice roll detection
        const prevHasDice = prevState.dice?.some((d) => d > 0)
        const nowHasDice = newState.dice?.some((d) => d > 0)
        if (!prevHasDice && nowHasDice) {
          audioService.playSound('dice-roll')
        }

        // Turn change detection
        if (!prevState.isYourTurn && newState.isYourTurn) {
          audioService.playSound('turn-change')
        }

        // Move detection
        const prevBornOff = prevState.whiteBornOff + prevState.redBornOff
        const newBornOff = newState.whiteBornOff + newState.redBornOff
        const prevBar = prevState.whiteCheckersOnBar + prevState.redCheckersOnBar
        const newBar = newState.whiteCheckersOnBar + newState.redCheckersOnBar
        const prevRemainingCount = prevState.remainingMoves?.length ?? 0
        const newRemainingCount = newState.remainingMoves?.length ?? 0

        if (prevRemainingCount > newRemainingCount && prevRemainingCount > 0) {
          if (newBornOff > prevBornOff) {
            audioService.playSound('bear-off')
          } else if (newBar > prevBar) {
            audioService.playSound('checker-hit')
          } else {
            audioService.playSound('checker-move')
          }
        }
      }
      prevGameStateRef.current = newState
    }

    // Create typed receiver - TypeScript enforces all method signatures
    const receiver: IGameHubClient = {
      // ===== Game State Events =====
      async gameUpdate(gameState) {
        console.log('[SignalR] GameUpdate', gameState.gameId)
        const context = parseGameContextFromPath(window.location.pathname)
        if (context.gameId && context.gameId !== gameState.gameId) {
          console.log('[SignalR] Ignoring GameUpdate for old game:', gameState.gameId)
          return
        }
        playGameSounds(gameState)
        setGameState(gameState)
        setCurrentGameId(gameState.gameId)
      },

      async gameStart(gameState) {
        console.log('[SignalR] GameStart', gameState.gameId)
        const context = parseGameContextFromPath(window.location.pathname)

        if (context.gameId && context.gameId !== gameState.gameId) {
          const isSameMatchDifferentGame =
            context.matchId && gameState.matchId &&
            context.matchId === gameState.matchId
          if (!isSameMatchDifferentGame) {
            console.log('[SignalR] Ignoring GameStart for unrelated game:', gameState.gameId)
            return
          }
          console.log('[SignalR] GameStart for next game in match:', gameState.gameId)
        }

        setGameState(gameState)
        setCurrentGameId(gameState.gameId)
        prevGameStateRef.current = gameState
        setShowGameResultModal(false)

        if (gameState.matchId && context.gameId !== gameState.gameId) {
          navigateRef.current(`/match/${gameState.matchId}/game/${gameState.gameId}`)
        } else if (!context.isOnGamePage && !context.isOnAnalysisPage && gameState.matchId) {
          navigateRef.current(`/match/${gameState.matchId}/game/${gameState.gameId}`)
        }
      },

      async gameOver(gameState) {
        console.log('[SignalR] GameOver', { winner: gameState.winner, winType: gameState.winType })

        // Play win/loss sound
        if (gameState.yourColor === gameState.winner) {
          audioService.playSound('game-won')
        } else {
          audioService.playSound('game-lost')
        }

        setGameState(gameState)
        prevGameStateRef.current = gameState

        const points =
          gameState.winType === 'Gammon' ? 2 * (gameState.doublingCubeValue || 1) :
          gameState.winType === 'Backgammon' ? 3 * (gameState.doublingCubeValue || 1) :
          (gameState.doublingCubeValue || 1)

        setLastGameResult(gameState.winner ?? null, points)
        setShowGameResultModal(true)
      },

      async spectatorJoined(gameState) {
        console.log('[SignalR] SpectatorJoined', gameState.gameId)
        setGameState(gameState)
        setIsSpectator(true)
        setCurrentGameId(gameState.gameId)
        prevGameStateRef.current = gameState
        if (gameState.matchId) {
          navigateRef.current(`/match/${gameState.matchId}/game/${gameState.gameId}`)
        }
      },

      // ===== Connection Events =====
      async waitingForOpponent(gameId) {
        console.log('[SignalR] WaitingForOpponent', gameId)
        setCurrentGameId(gameId)
      },

      async opponentJoined(opponentId) {
        console.log('[SignalR] OpponentJoined', opponentId)
      },

      async opponentLeft() {
        console.log('[SignalR] OpponentLeft')
      },

      // ===== Doubling Events =====
      async doubleOffered(offer) {
        console.log('[SignalR] DoubleOffered', offer)
        audioService.playSound('double-offer')
        const offerFrom = myColorRef.current === CheckerColor.White
          ? CheckerColor.Red
          : CheckerColor.White
        setPendingDoubleOffer(offerFrom, offer.newStakes)
      },

      async doubleAccepted(gameState) {
        console.log('[SignalR] DoubleAccepted', gameState.doublingCubeValue)
        setGameState(gameState)
      },

      // ===== Chat Events =====
      async receiveChatMessage(senderName, message, senderConnectionId) {
        const conn = connectionRef.current
        const isOwn = conn ? senderConnectionId === conn.connectionId : false
        const displayName = isOwn ? 'You' : senderName

        if (!isOwn) {
          audioService.playSound('chat-message')
        }

        addChatMessage({
          senderName: displayName,
          message,
          timestamp: new Date(),
          isOwn,
        })
      },

      async receiveChatHistory(history) {
        clearChatMessages()
        history.messages.forEach((msg) => {
          addChatMessage({
            senderName: msg.senderName,
            message: msg.message,
            timestamp: new Date(msg.timestamp),
            isOwn: msg.isOwn,
          })
        })
      },

      // ===== Error/Info Events =====
      async error(errorMessage) {
        console.error('[SignalR] Error:', errorMessage)
        toastRef.current({
          title: 'Error',
          description: errorMessage,
          variant: 'destructive',
        })
      },

      async info(infoMessage) {
        console.log('[SignalR] Info:', infoMessage)
        toastRef.current({
          title: 'Info',
          description: infoMessage,
        })
      },

      // ===== Match Events =====
      async matchCreated(data) {
        console.log('[SignalR] MatchCreated', { matchId: data.matchId, gameId: data.gameId })
        setCurrentGameId(data.gameId)
        navigateRef.current(`/match/${data.matchId}/game/${data.gameId}`, { replace: true })
      },

      async opponentJoinedMatch(data) {
        console.log('[SignalR] OpponentJoinedMatch', { matchId: data.matchId, player2Name: data.player2Name })
      },

      async matchUpdate(data) {
        console.log('[SignalR] MatchUpdate', data)
        setMatchState({
          matchId: data.matchId,
          player1Score: data.player1Score,
          player2Score: data.player2Score,
          targetScore: data.targetScore,
          isCrawfordGame: data.isCrawfordGame,
          matchComplete: data.matchComplete,
          matchWinner: data.matchWinner ?? null,
          lastUpdatedAt: new Date().toISOString(),
        })
      },

      // ===== Time Events =====
      async timeUpdate(data) {
        const context = parseGameContextFromPath(window.location.pathname)
        if (context.gameId && context.gameId !== data.gameId) {
          return
        }
        updateTimeState({
          whiteReserveSeconds: data.whiteReserveSeconds,
          redReserveSeconds: data.redReserveSeconds,
          whiteIsInDelay: data.whiteIsInDelay,
          redIsInDelay: data.redIsInDelay,
          whiteDelayRemaining: data.whiteDelayRemaining,
          redDelayRemaining: data.redDelayRemaining,
        })
      },

      async playerTimedOut(data) {
        console.log('[SignalR] PlayerTimedOut', data)
        toastRef.current({
          title: 'Time Out!',
          description: `${data.timedOutPlayer} ran out of time. ${data.winner} wins!`,
          variant: 'destructive',
        })
        audioService.playSound('game-lost')
      },

      // ===== Unhandled Events (stubs for type safety) =====
      async matchGameStarting(data) {
        console.log('[SignalR] MatchGameStarting', data)
      },

      async matchContinued(data) {
        console.log('[SignalR] MatchContinued', data)
      },

      async matchStatus(data) {
        console.log('[SignalR] MatchStatus', data)
      },

      async matchGameCompleted(data) {
        console.log('[SignalR] MatchGameCompleted', data)
      },

      async matchCompleted(data) {
        console.log('[SignalR] MatchCompleted', data)
      },

      async matchInvite(data) {
        console.log('[SignalR] MatchInvite', data)
      },

      async myMatches(matches) {
        console.log('[SignalR] MyMatches', matches.length, 'matches')
      },

      async correspondenceMatchInvite(data) {
        console.log('[SignalR] CorrespondenceMatchInvite', data)
      },

      async correspondenceTurnNotification(data) {
        console.log('[SignalR] CorrespondenceTurnNotification', data)
      },

      async correspondenceLobbyCreated(data) {
        console.log('[SignalR] CorrespondenceLobbyCreated', data)
      },

      async lobbyCreated(data) {
        console.log('[SignalR] LobbyCreated', data)
      },

      async friendRequestReceived() {
        console.log('[SignalR] FriendRequestReceived')
      },

      async friendRequestAccepted() {
        console.log('[SignalR] FriendRequestAccepted')
      },
    }

    // Single type-safe registration
    const disposable = getReceiverRegister('IGameHubClient').register(connection, receiver)

    // Single cleanup
    return () => disposable.dispose()
  }, [
    connection,
    setGameState,
    setCurrentGameId,
    setIsSpectator,
    setShowGameResultModal,
    setLastGameResult,
    setMatchState,
    setPendingDoubleOffer,
    updateTimeState,
    addChatMessage,
    clearChatMessages,
  ])
}
