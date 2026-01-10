import React, { useEffect, useState, useRef } from 'react'
import { useParams, useNavigate } from 'react-router-dom'
import { useGameStore } from '@/stores/gameStore'
import { useSignalR } from '@/contexts/SignalRContext'
import { HubMethods } from '@/types/signalr.types'
import { BoardSVG } from '@/components/game/BoardSVG'
import { PlayerCard } from '@/components/game/PlayerCard'
import { GameControls } from '@/components/game/GameControls'
import { MatchInfo } from '@/components/game/MatchInfo'
import { BoardOverlayControls } from '@/components/game/BoardOverlayControls'
import { TimeDisplay } from '@/components/game/TimeDisplay'
import { GameResultModal } from '@/components/modals/GameResultModal'
import { DoubleConfirmModal } from '@/components/modals/DoubleConfirmModal'
import { DoubleOfferModal } from '@/components/modals/DoubleOfferModal'
import { NotFound } from '@/components/NotFound'
import { CheckerColor } from '@/types/game.types'
import { Card, CardContent } from '@/components/ui/card'
import { Badge } from '@/components/ui/badge'
import { authService } from '@/services/auth.service'
import { Eye, BarChart3, Trophy, TrendingUp } from 'lucide-react'

export const GamePage: React.FC = () => {
  const { gameId } = useParams<{ gameId: string }>()
  const navigate = useNavigate()
  const { invoke, isConnected, connection } = useSignalR()

  const {
    currentGameState,
    isSpectator,
    setCurrentGameId,
    resetGame,
    doublingCube,
    clearPendingDoubleOffer,
  } = useGameStore()
  const [isLoading, setIsLoading] = useState(true)
  const [lastJoinedGameId, setLastJoinedGameId] = useState<string | null>(null)
  const [gameNotFound, setGameNotFound] = useState(false)
  const [showDoubleConfirmModal, setShowDoubleConfirmModal] = useState(false)
  const lastJoinedGameIdRef = useRef<string | null>(null)

  // Keep ref in sync with state
  useEffect(() => {
    lastJoinedGameIdRef.current = lastJoinedGameId
  }, [lastJoinedGameId])

  // Listen for error events (like "Game not found")
  useEffect(() => {
    if (!connection) return

    const handleError = (errorMessage: string) => {
      console.error('[GamePage] SignalR Error:', errorMessage)
      if (errorMessage.includes('not found') || errorMessage.includes('Game') && errorMessage.includes('not found')) {
        setGameNotFound(true)
        setIsLoading(false)
      }
    }

    connection.on('Error', handleError)

    return () => {
      connection.off('Error', handleError)
    }
  }, [connection])

  // Cleanup when component unmounts (navigating away from game page entirely)
  useEffect(() => {
    return () => {
      if (lastJoinedGameIdRef.current) {
        console.log('[GamePage] Component unmounting, leaving game:', lastJoinedGameIdRef.current)
        invoke(HubMethods.LeaveGame).catch((err) => {
          console.error('[GamePage] Failed to leave game on unmount:', err)
        })
      }
    }
    // Empty deps = only runs on mount/unmount, but uses ref for current value
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [])

  useEffect(() => {
    const joinGame = async () => {
      if (!gameId) {
        navigate('/')
        return
      }

      // Wait for SignalR connection before attempting to join
      if (!isConnected) {
        console.log('[GamePage] Waiting for SignalR connection...')
        return
      }

      // Skip if we've already joined this game
      if (lastJoinedGameId === gameId) {
        console.log('[GamePage] Already joined game:', gameId)
        return
      }

      // If we were in a different game, leave it first
      if (lastJoinedGameId && lastJoinedGameId !== gameId) {
        console.log('[GamePage] Leaving previous game:', lastJoinedGameId)
        try {
          await invoke(HubMethods.LeaveGame)
        } catch (error) {
          console.error('[GamePage] Failed to leave previous game:', error)
          // Continue anyway - we want to join the new game
        }
      }

      console.log('[GamePage] New game detected, resetting state')
      resetGame()
      setCurrentGameId(gameId)
      setIsLoading(true)
      setGameNotFound(false)
      setLastJoinedGameId(gameId)

      // Join the game
      try {
        const playerId = authService.getOrCreatePlayerId()
        const displayName = authService.getDisplayName()
        console.log('[GamePage] ========== JOINING GAME ==========')
        console.log('[GamePage] Game ID:', gameId)
        console.log('[GamePage] Player ID:', playerId)
        console.log('[GamePage] Display Name:', displayName)
        console.log('[GamePage] Auth Token:', authService.getToken() ? 'Present' : 'Missing')
        console.log('[GamePage] =====================================')
        await invoke(HubMethods.JoinGame, playerId, gameId)
        // The GameStart or GameUpdate event will set the game state
        setIsLoading(false)
      } catch (error) {
        console.error('[GamePage] Failed to join game:', error)
        setIsLoading(false)
      }
    }

    joinGame()

    // Cleanup when gameId changes or component unmounts
    return () => {
      // Only leave if we're switching to a different game or unmounting completely
      // Don't leave if lastJoinedGameId is just being set for the first time
      if (lastJoinedGameId && lastJoinedGameId !== gameId) {
        console.log('[GamePage] Cleaning up previous game:', lastJoinedGameId)
        invoke(HubMethods.LeaveGame).catch((err) => {
          console.error('[GamePage] Failed to leave game on cleanup:', err)
        })
      }
    }
    // Note: lastJoinedGameId is intentionally NOT in dependencies
    // We only want to re-run when gameId or connection state changes
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [gameId, isConnected, navigate, setCurrentGameId, invoke, resetGame])

  // Doubling cube handlers
  const handleOfferDouble = () => {
    setShowDoubleConfirmModal(true)
  }

  const handleConfirmDouble = async () => {
    try {
      await invoke(HubMethods.OfferDouble)
      setShowDoubleConfirmModal(false)
    } catch (error) {
      console.error('[GamePage] Failed to offer double:', error)
    }
  }

  if (isLoading) {
    return (
      <div className="min-h-screen bg-background flex items-center justify-center">
        <Card>
          <CardContent className="p-8">
            <div className="text-center">
              <div className="text-lg font-semibold mb-2">Loading game...</div>
              <div className="text-sm text-muted-foreground">Game ID: {gameId}</div>
            </div>
          </CardContent>
        </Card>
      </div>
    )
  }

  if (gameNotFound) {
    return (
      <NotFound
        title="Game Not Found"
        message={`The game with ID ${gameId} could not be found. It may have been deleted or the link may be incorrect.`}
      />
    )
  }

  if (!currentGameState) {
    return (
      <div className="min-h-screen bg-background flex items-center justify-center">
        <Card>
          <CardContent className="p-8">
            <div className="text-center">
              <div className="text-lg font-semibold mb-2">Waiting for game data...</div>
              <div className="text-sm text-muted-foreground">Game ID: {gameId}</div>
            </div>
          </CardContent>
        </Card>
      </div>
    )
  }

  const whitePlayer = {
    playerName: currentGameState.whitePlayerName || 'White',
    username: currentGameState.whiteUsername,
    color: CheckerColor.White,
    isYourTurn: currentGameState.currentPlayer === CheckerColor.White,
    isYou: currentGameState.yourColor === CheckerColor.White,
    pipCount: currentGameState.whitePipCount,
    checkersOnBar: currentGameState.whiteCheckersOnBar,
    bornOff: currentGameState.whiteBornOff,
    rating: currentGameState.whiteRating,
    ratingChange: currentGameState.whiteRatingChange,
  }

  const redPlayer = {
    playerName: currentGameState.redPlayerName || 'Red',
    username: currentGameState.redUsername,
    color: CheckerColor.Red,
    isYourTurn: currentGameState.currentPlayer === CheckerColor.Red,
    isYou: currentGameState.yourColor === CheckerColor.Red,
    pipCount: currentGameState.redPipCount,
    checkersOnBar: currentGameState.redCheckersOnBar,
    bornOff: currentGameState.redBornOff,
    rating: currentGameState.redRating,
    ratingChange: currentGameState.redRatingChange,
  }

  return (
    <div className="min-h-screen bg-background">
      <div className="max-w-[1920px] mx-auto px-2 py-4">
        <div className="grid grid-cols-1 lg:grid-cols-[320px_1fr] gap-4">
          {/* Left Sidebar - Player Info & Controls */}
          <div className="space-y-4">
            {isSpectator && (
              <Badge variant="secondary" className="w-full justify-center py-2">
                <Eye className="h-4 w-4 mr-2" />
                Spectating
              </Badge>
            )}

            <PlayerCard {...whitePlayer} />

            {/* White Player Timer */}
            {currentGameState.timeControlType && currentGameState.timeControlType !== 'None' && (
              <TimeDisplay
                reserveSeconds={currentGameState.whiteReserveSeconds ?? null}
                isInDelay={currentGameState.whiteIsInDelay ?? null}
                delayRemaining={currentGameState.whiteDelayRemaining ?? null}
                isActive={!currentGameState.isOpeningRoll && currentGameState.currentPlayer === CheckerColor.White}
                color={CheckerColor.White}
              />
            )}

            {currentGameState.isMatchGame &&
              currentGameState.targetScore &&
              currentGameState.player1Score !== undefined &&
              currentGameState.player2Score !== undefined && (
                <MatchInfo
                  targetScore={currentGameState.targetScore}
                  player1Score={currentGameState.player1Score}
                  player2Score={currentGameState.player2Score}
                  isCrawfordGame={currentGameState.isCrawfordGame ?? false}
                  player1Name={currentGameState.whitePlayerName}
                  player2Name={currentGameState.redPlayerName}
                />
              )}

            <PlayerCard {...redPlayer} />

            {/* Red Player Timer */}
            {currentGameState.timeControlType && currentGameState.timeControlType !== 'None' && (
              <TimeDisplay
                reserveSeconds={currentGameState.redReserveSeconds ?? null}
                isInDelay={currentGameState.redIsInDelay ?? null}
                delayRemaining={currentGameState.redDelayRemaining ?? null}
                isActive={!currentGameState.isOpeningRoll && currentGameState.currentPlayer === CheckerColor.Red}
                color={CheckerColor.Red}
              />
            )}

            <GameControls gameState={currentGameState} isSpectator={isSpectator} />
          </div>

          {/* Main Board Area */}
          <div>
            <Card>
              <CardContent className="p-2 relative">
                <div className="flex gap-2 mb-4">
                  {currentGameState.isAnalysisMode && (
                    <Badge variant="secondary">
                      <BarChart3 className="h-4 w-4 mr-2" />
                      Analysis Mode
                    </Badge>
                  )}

                  {!currentGameState.isAnalysisMode && (
                    <Badge variant={currentGameState.isRated ? "default" : "outline"}>
                      {currentGameState.isRated ? (
                        <>
                          <Trophy className="h-4 w-4 mr-2" />
                          Rated
                        </>
                      ) : (
                        <>
                          <TrendingUp className="h-4 w-4 mr-2" />
                          Unrated
                        </>
                      )}
                    </Badge>
                  )}
                </div>

                {/* Board with overlay controls */}
                <div className="relative">
                  <BoardSVG
                    gameState={currentGameState}
                    isSpectator={isSpectator}
                    onOfferDouble={!isSpectator ? handleOfferDouble : undefined}
                  />
                  <BoardOverlayControls
                    gameState={currentGameState}
                    isSpectator={isSpectator}
                  />
                </div>
              </CardContent>
            </Card>
          </div>
        </div>
      </div>

      {/* Game Result Modal */}
      <GameResultModal />

      {/* Double Confirm Modal - shown when player wants to offer double */}
      {currentGameState && (
        <DoubleConfirmModal
          isOpen={showDoubleConfirmModal}
          onClose={() => setShowDoubleConfirmModal(false)}
          onConfirm={handleConfirmDouble}
          currentValue={doublingCube.value}
          newValue={doublingCube.value * 2}
        />
      )}

      {/* Double Offer Modal - shown when opponent offers double */}
      {currentGameState && doublingCube.pendingResponse && doublingCube.newValue && (
        <DoubleOfferModal
          isOpen={doublingCube.pendingResponse}
          onClose={clearPendingDoubleOffer}
          currentStakes={doublingCube.value}
          newStakes={doublingCube.newValue}
        />
      )}
    </div>
  )
}
