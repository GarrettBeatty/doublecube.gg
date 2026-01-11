import React, { useEffect, useState, useRef } from 'react'
import { useParams, useNavigate } from 'react-router-dom'
import { useGameStore, MatchState } from '@/stores/gameStore'
import { useSignalR } from '@/contexts/SignalRContext'
import type { MatchStateDto } from '@/types/generated/Backgammon.Server.Models'
import { GameBoardAdapter } from '@/components/board'
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
  const { hub, isConnected, connection } = useSignalR()

  const {
    currentGameState,
    isSpectator,
    setCurrentGameId,
    resetGame,
    doublingCube,
    clearPendingDoubleOffer,
    setMatchState,
  } = useGameStore()
  const [isLoading, setIsLoading] = useState(true)
  const [lastJoinedGameId, setLastJoinedGameId] = useState<string | null>(null)
  const [gameNotFound, setGameNotFound] = useState(false)
  const [showDoubleConfirmModal, setShowDoubleConfirmModal] = useState(false)
  const lastJoinedGameIdRef = useRef<string | null>(null)
  const lastSyncedMatchIdRef = useRef<string | null>(null)

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
        hub?.leaveGame().catch((err) => {
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
          await hub?.leaveGame()
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
        await hub?.joinGame(playerId, gameId)
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
        hub?.leaveGame().catch((err) => {
          console.error('[GamePage] Failed to leave game on cleanup:', err)
        })
      }
    }
    // Note: lastJoinedGameId is intentionally NOT in dependencies
    // We only want to re-run when gameId or connection state changes
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [gameId, isConnected, navigate, setCurrentGameId, hub, resetGame])

  // Sync match state from authoritative game state
  // This ensures matchState never becomes stale when we have fresh game data
  // Uses functional update to avoid matchState dependency and potential infinite loops
  useEffect(() => {
    if (
      !currentGameState?.isMatchGame ||
      currentGameState.player1Score === undefined ||
      currentGameState.player2Score === undefined
    ) {
      return
    }

    // Extract values before closure to maintain type narrowing
    const matchId = currentGameState.matchId || ''
    const player1Score = currentGameState.player1Score
    const player2Score = currentGameState.player2Score
    const targetScore = currentGameState.targetScore || 0
    const isCrawfordGame = currentGameState.isCrawfordGame || false

    setMatchState((previousMatchState: MatchState | null): MatchState | null => {
      // Check if we need to update matchState based on game state
      const needsUpdate =
        !previousMatchState ||
        previousMatchState.player1Score !== player1Score ||
        previousMatchState.player2Score !== player2Score ||
        previousMatchState.isCrawfordGame !== isCrawfordGame

      if (!needsUpdate) {
        return previousMatchState
      }

      console.log('[GamePage] Syncing matchState from authoritative GameState')

      return {
        matchId,
        player1Score,
        player2Score,
        targetScore,
        isCrawfordGame,
        // Preserve existing matchComplete/matchWinner (set via MatchUpdate events)
        matchComplete: previousMatchState?.matchComplete || false,
        matchWinner: previousMatchState?.matchWinner || null,
        lastUpdatedAt: new Date().toISOString(),
      }
    })
  }, [currentGameState, setMatchState])

  // Extract matchId to use as a stable dependency value
  const currentMatchId = currentGameState?.isMatchGame ? currentGameState.matchId : null

  // Fetch authoritative match state from server on reconnection for match games
  // Uses ref to track which matchId we've already synced to prevent redundant fetches
  useEffect(() => {
    const syncMatchState = async () => {
      // Only sync if we have a match game and are connected
      if (!isConnected || !currentMatchId) {
        return
      }

      // Skip if we've already synced this matchId
      if (lastSyncedMatchIdRef.current === currentMatchId) {
        return
      }

      try {
        console.log('[GamePage] Fetching authoritative match state for:', currentMatchId)
        const serverMatchState: MatchStateDto | null = await hub?.getMatchState(currentMatchId) ?? null

        if (serverMatchState) {
          // Validate server timestamp - handle both Date and string types
          const lastUpdated = serverMatchState.lastUpdatedAt
          const serverTimestamp = lastUpdated instanceof Date
            ? lastUpdated.getTime()
            : Date.parse(lastUpdated)
          if (Number.isNaN(serverTimestamp)) {
            console.error(
              '[GamePage] Invalid server match state timestamp:',
              lastUpdated
            )
            return
          }

          // Use functional update to avoid matchState dependency
          setMatchState(
            (previousMatchState: MatchState | null): MatchState | null => {
              // Validate local timestamp
              let localTimestamp = 0
              if (previousMatchState?.lastUpdatedAt) {
                const parsedLocal = Date.parse(previousMatchState.lastUpdatedAt)
                if (Number.isNaN(parsedLocal)) {
                  console.warn(
                    '[GamePage] Invalid local match state timestamp, treating as 0:',
                    previousMatchState.lastUpdatedAt
                  )
                } else {
                  localTimestamp = parsedLocal
                }
              }

              // Only update if server data is newer than what we have
              if (serverTimestamp > localTimestamp) {
                console.log('[GamePage] Server match state is newer, updating local state')
                // Convert Date to string and undefined to null for compatibility
                const lastUpdatedStr = lastUpdated instanceof Date
                  ? lastUpdated.toISOString()
                  : lastUpdated
                return {
                  matchId: serverMatchState.matchId,
                  player1Score: serverMatchState.player1Score,
                  player2Score: serverMatchState.player2Score,
                  targetScore: serverMatchState.targetScore,
                  isCrawfordGame: serverMatchState.isCrawfordGame,
                  matchComplete: serverMatchState.matchComplete,
                  matchWinner: serverMatchState.matchWinner ?? null,
                  lastUpdatedAt: lastUpdatedStr,
                }
              } else {
                console.log('[GamePage] Local match state is up to date')
                return previousMatchState
              }
            }
          )

          // Mark this matchId as synced
          lastSyncedMatchIdRef.current = currentMatchId
        }
      } catch (error) {
        console.error('[GamePage] Failed to fetch match state:', error)
        // Non-critical error - game state already has match info
      }
    }

    syncMatchState()
  }, [isConnected, currentMatchId, hub, setMatchState])

  // Doubling cube handlers
  const handleOfferDouble = () => {
    setShowDoubleConfirmModal(true)
  }

  const handleConfirmDouble = async () => {
    try {
      await hub?.offerDouble()
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
                  <GameBoardAdapter
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
