import { useEffect, useRef } from 'react'
import { useNavigate } from 'react-router-dom'
import { useSignalR } from '@/contexts/SignalRContext'
import { useGameStore } from '@/stores/gameStore'
import { audioService } from '@/services/audio.service'
import { useToast } from '@/hooks/use-toast'
import { GameState, CheckerColor } from '@/types/game.types'
import { HubEvents } from '@/types/signalr.types'
import {
  MatchCreatedDto,
  MatchGameStartingDto,
  MatchUpdateDto,
  MatchContinuedDto,
  OpponentJoinedMatchDto,
  TimeUpdateDto,
  PlayerTimedOutDto,
} from '@/types/generated/Backgammon.Server.Models.SignalR'

export const useSignalREvents = () => {
  const navigate = useNavigate()
  const { connection } = useSignalR()
  const {
    setGameState,
    updateTimeState,
    setIsSpectator,
    setCurrentGameId,
    addChatMessage,
    setMatchState,
    setShowGameResultModal,
    setLastGameResult,
    setPendingDoubleOffer,
    myColor,
  } = useGameStore()
  const { toast } = useToast()
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
      console.log('[SignalR] ========== GAME UPDATE RECEIVED ==========')
      console.log('[SignalR] Game ID:', gameState.gameId)
      console.log('[SignalR] White Player Name:', gameState.whitePlayerName)
      console.log('[SignalR] Red Player Name:', gameState.redPlayerName)
      console.log('[SignalR] Current Player:', gameState.currentPlayer)
      console.log('[SignalR] Is Your Turn:', gameState.isYourTurn)
      console.log('[SignalR] Your Color:', gameState.yourColor)
      console.log('[SignalR] =================================================')

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
        // Dice roll detection - check if dice went from no values to having values
        const prevHasDice = prevState.dice && prevState.dice.length > 0 && prevState.dice.some((d) => d > 0)
        const nowHasDice = gameState.dice && gameState.dice.length > 0 && gameState.dice.some((d) => d > 0)
        if (!prevHasDice && nowHasDice) {
          audioService.playSound('dice-roll')
        }

        // Turn change detection - only play sound when it becomes YOUR turn
        const wasPrevYourTurn = prevState.isYourTurn
        const isNowYourTurn = gameState.isYourTurn
        if (!wasPrevYourTurn && isNowYourTurn) {
          audioService.playSound('turn-change')
        }

        // Move detection - detect when a move was made by checking if remainingMoves decreased
        const prevBornOff =
          prevState.whiteBornOff + prevState.redBornOff
        const newBornOff = gameState.whiteBornOff + gameState.redBornOff
        const prevBar =
          prevState.whiteCheckersOnBar + prevState.redCheckersOnBar
        const newBar = gameState.whiteCheckersOnBar + gameState.redCheckersOnBar
        const prevRemainingCount = prevState.remainingMoves?.length ?? 0
        const newRemainingCount = gameState.remainingMoves?.length ?? 0

        // Detect what type of move occurred (only if remainingMoves decreased, meaning a move was made)
        if (prevRemainingCount > newRemainingCount && prevRemainingCount > 0) {
          if (newBornOff > prevBornOff) {
            // Checker was born off
            audioService.playSound('bear-off')
          } else if (newBar > prevBar) {
            // Checker was hit (sent to bar)
            audioService.playSound('checker-hit')
          } else {
            // Regular move
            audioService.playSound('checker-move')
          }
        }
      }

      setGameState(gameState)
      setCurrentGameId(gameState.gameId)
      prevGameStateRef.current = gameState
    })

    // GameStart - Both players connected, game starts
    connection.on(HubEvents.GameStart, (gameState: GameState) => {
      console.log('[SignalR] ========== GAME START RECEIVED ==========')
      console.log('[SignalR] Game ID:', gameState.gameId)
      console.log('[SignalR] White Player Name:', gameState.whitePlayerName)
      console.log('[SignalR] Red Player Name:', gameState.redPlayerName)
      console.log('[SignalR] Current Player:', gameState.currentPlayer)
      console.log('[SignalR] Is Your Turn:', gameState.isYourTurn)
      console.log('[SignalR] Your Color:', gameState.yourColor)
      console.log('[SignalR] ================================================')

      // Check if we're on a game page and if it's for a different game
      const currentPath = window.location.pathname
      const isOnGamePage = currentPath.startsWith('/game/')
      const isOnAnalysisPage = currentPath.startsWith('/analysis')
      const currentGameIdFromUrl = isOnGamePage ? currentPath.split('/game/')[1] : null

      // If we're on a different game page, ignore this GameStart (it's from an old game)
      if (currentGameIdFromUrl && currentGameIdFromUrl !== gameState.gameId) {
        console.log('[SignalR] Ignoring GameStart for old game:', gameState.gameId, 'current game:', currentGameIdFromUrl)
        return
      }

      setGameState(gameState)
      setCurrentGameId(gameState.gameId)
      prevGameStateRef.current = gameState

      // Only navigate if we're not already on a game page or analysis page
      if (!isOnGamePage && !isOnAnalysisPage) {
        navigateRef.current(`/game/${gameState.gameId}`)
      }
    })

    // GameOver - Game completed
    connection.on(
      HubEvents.GameOver,
      (gameState: GameState) => {
        console.log('[SignalR] GameOver', { winner: gameState.winner, winType: gameState.winType })

        // Play win/loss sound
        if (gameState.yourColor === gameState.winner) {
          audioService.playSound('game-won')
        } else {
          audioService.playSound('game-lost')
        }

        setGameState(gameState)
        prevGameStateRef.current = gameState

        // Store game result for modal display
        // Calculate points from win type and cube value
        const points = gameState.winType === 'Gammon' ? 2 * (gameState.doublingCubeValue || 1)
          : gameState.winType === 'Backgammon' ? 3 * (gameState.doublingCubeValue || 1)
          : (gameState.doublingCubeValue || 1)

        setLastGameResult(gameState.winner, points)

        // Show result modal for all game types
        setShowGameResultModal(true)
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

        // Determine which player offered the double (opposite of your color)
        const offerFrom = myColor === CheckerColor.White ? CheckerColor.Red : CheckerColor.White

        // Update store to show the response modal
        setPendingDoubleOffer(offerFrom, newStakes)
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
      toast({
        title: 'Error',
        description: errorMessage,
        variant: 'destructive',
      })
    })

    // Info - Server info message
    connection.on(HubEvents.Info, (infoMessage: string) => {
      console.log('[SignalR] Info:', infoMessage)
      toast({
        title: 'Info',
        description: infoMessage,
      })
    })

    // MatchCreated - Match and first game created, navigate to game page
    connection.on(HubEvents.MatchCreated, (data: MatchCreatedDto) => {
      console.log('[SignalR] MatchCreated', { matchId: data.matchId, gameId: data.gameId, opponentType: data.opponentType })
      setCurrentGameId(data.gameId)
      // Navigate to game page immediately - game handles waiting state
      navigateRef.current(`/game/${data.gameId}`, { replace: true })
    })

    // OpponentJoinedMatch - Opponent joined the match
    connection.on(HubEvents.OpponentJoinedMatch, (data: OpponentJoinedMatchDto) => {
      console.log('[SignalR] OpponentJoinedMatch', { matchId: data.matchId, player2Name: data.player2Name })
      // Game page will receive WaitingForOpponent → GameStart flow
    })

    // MatchGameStarting - Match game starting, navigate to game
    connection.on(HubEvents.MatchGameStarting, (data: MatchGameStartingDto) => {
      console.log('[SignalR] MatchGameStarting', {
        matchId: data.matchId,
        gameId: data.gameId,
        gameNumber: data.gameNumber,
        score: `${data.currentScore.player1}-${data.currentScore.player2}`,
        isCrawford: data.isCrawfordGame
      })

      // Update game state with the new game
      // Add missing fields to match the GameState interface
      const now = new Date().toISOString()
      const gameState = {
        ...data.state,
        createdAt: now,
        updatedAt: now,
      } as unknown as GameState
      setGameState(gameState)
      setCurrentGameId(data.gameId)
      prevGameStateRef.current = gameState

      // Update match state
      setMatchState({
        matchId: data.matchId,
        player1Score: data.currentScore.player1,
        player2Score: data.currentScore.player2,
        targetScore: data.state.targetScore || 0,
        isCrawfordGame: data.isCrawfordGame,
        matchComplete: false,
        matchWinner: null as string | null,
        lastUpdatedAt: new Date().toISOString(),
      })

      // Close any open game result modal
      setShowGameResultModal(false)

      // Check if we're already on a game page for this match
      const currentPath = window.location.pathname
      const isOnGamePage = currentPath.startsWith('/game/')

      if (isOnGamePage) {
        // If on a game page, force navigation to the new game
        navigateRef.current(`/game/${data.gameId}`, { replace: true })
      } else {
        // If not on a game page, navigate normally
        navigateRef.current(`/game/${data.gameId}`)
      }
    })

    // TimeUpdate - Server broadcasts updated time state every second
    connection.on(HubEvents.TimeUpdate, (timeUpdate: TimeUpdateDto) => {
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
    connection.on(HubEvents.PlayerTimedOut, (timeoutEvent: PlayerTimedOutDto) => {
      console.log('[SignalR] PlayerTimedOut', timeoutEvent)

      // Show toast notification about timeout
      toast({
        title: 'Time Out!',
        description: `${timeoutEvent.timedOutPlayer} ran out of time. ${timeoutEvent.winner} wins!`,
        variant: 'destructive',
      })

      audioService.playSound('game-lost')
    })

    // MatchUpdate - Match score/state updated
    connection.on(HubEvents.MatchUpdate, (data: MatchUpdateDto) => {
      console.log('[SignalR] MatchUpdate', data)

      // Include current timestamp when receiving updates
      // Server events are authoritative, so we always apply them
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
    })

    // MatchContinued - Next game in match started
    connection.on(HubEvents.MatchContinued, (data: MatchContinuedDto) => {
      console.log('[SignalR] MatchContinued', data)

      // Update match state with timestamp
      setMatchState({
        matchId: data.matchId,
        player1Score: data.player1Score,
        player2Score: data.player2Score,
        targetScore: data.targetScore,
        isCrawfordGame: data.isCrawfordGame,
        matchComplete: false,
        matchWinner: null,
        lastUpdatedAt: new Date().toISOString(),
      })

      // Close the result modal
      setShowGameResultModal(false)

      // Navigate to new game
      setCurrentGameId(data.gameId)
      navigateRef.current(`/game/${data.gameId}`, { replace: true })
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
      connection.off(HubEvents.MatchUpdate)
      connection.off(HubEvents.MatchContinued)
    }
  }, [connection, setGameState, updateTimeState, setIsSpectator, setCurrentGameId, addChatMessage, setMatchState, setShowGameResultModal, setLastGameResult, setPendingDoubleOffer, myColor, toast])

  return { connection }
}
