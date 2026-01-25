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
import { MoveLog } from '@/components/game/MoveLog'
import { TimeDisplay } from '@/components/game/TimeDisplay'
import { CorrespondenceDeadline } from '@/components/game/CorrespondenceDeadline'
import { GameCompletedOverlay } from '@/components/game/GameCompletedOverlay'
import { CompletedGameBanner } from '@/components/game/CompletedGameBanner'
import { DoubleConfirmModal } from '@/components/modals/DoubleConfirmModal'
import { DoubleOfferModal } from '@/components/modals/DoubleOfferModal'
import { WaitingForDoubleResponseModal } from '@/components/modals/WaitingForDoubleResponseModal'
import { NotFound } from '@/components/NotFound'
import { CheckerColor, GameStatus } from '@/types/game.types'
import { Card, CardContent } from '@/components/ui/card'
import { Badge } from '@/components/ui/badge'
import { authService } from '@/services/auth.service'
import { Eye, BarChart3, Trophy, TrendingUp, Clock, Loader2 } from 'lucide-react'

export const GamePage: React.FC = () => {
  const { matchId, gameId } = useParams<{ matchId: string; gameId: string }>()
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
      if (!gameId || !matchId) {
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
        const displayName = authService.getDisplayName()
        console.log('[GamePage] ========== JOINING GAME ==========')
        console.log('[GamePage] Game ID:', gameId)
        console.log('[GamePage] Display Name:', displayName)
        console.log('[GamePage] Auth Token:', authService.getToken() ? 'Present' : 'Missing')
        console.log('[GamePage] =====================================')
        await hub?.joinGame(gameId)
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
      !currentGameState?.matchId ||
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
        // Use previous timestamp or epoch 0 so server fetch can override with accurate matchComplete
        lastUpdatedAt: previousMatchState?.lastUpdatedAt || new Date(0).toISOString(),
      }
    })
  }, [currentGameState, setMatchState])

  // Extract matchId to use as a stable dependency value
  const currentMatchId = currentGameState?.matchId || null

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
        <Card className="w-full max-w-md mx-4">
          <CardContent className="p-8">
            <div className="flex flex-col items-center gap-4">
              <div className="relative">
                <Loader2 className="h-12 w-12 animate-spin text-primary" />
              </div>
              <div className="text-center">
                <div className="text-lg font-semibold mb-1">Loading game...</div>
                <div className="text-sm text-muted-foreground">Connecting to game session</div>
              </div>
              {/* Loading skeleton for board */}
              <div className="w-full aspect-[1.5] bg-muted rounded-lg animate-pulse mt-4" />
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
        <Card className="w-full max-w-md mx-4">
          <CardContent className="p-8">
            <div className="flex flex-col items-center gap-4">
              <div className="relative">
                <Clock className="h-12 w-12 text-muted-foreground animate-pulse" />
              </div>
              <div className="text-center">
                <div className="text-lg font-semibold mb-1">Waiting for game data...</div>
                <div className="text-sm text-muted-foreground">
                  The game board will appear shortly
                </div>
              </div>
              {/* Loading skeleton for board */}
              <div className="w-full aspect-[1.5] bg-muted rounded-lg animate-pulse mt-4" />
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
        {/* Mobile-first: board comes first on small screens */}
        <div className="grid grid-cols-1 lg:grid-cols-[320px_1fr] gap-4">
          {/* Mobile Condensed Header - Only shown on mobile */}
          <div className="lg:hidden space-y-2">
            {isSpectator && (
              <Badge variant="secondary" className="w-full justify-center py-1 text-xs">
                <Eye className="h-3 w-3 mr-1" />
                Spectating
              </Badge>
            )}
            {/* Condensed player info row */}
            <div className="flex items-center justify-between gap-2 p-2 bg-card rounded-lg border">
              {/* White player mini */}
              <div className="flex items-center gap-2 flex-1 min-w-0">
                <div className="w-4 h-4 rounded-full bg-gray-100 border flex-shrink-0" />
                <div className="truncate">
                  <div className="text-sm font-medium truncate">{whitePlayer.playerName}</div>
                  {whitePlayer.isYou && <span className="text-xs text-muted-foreground">You</span>}
                </div>
                {whitePlayer.checkersOnBar && whitePlayer.checkersOnBar > 0 && (
                  <Badge variant="outline" className="text-xs text-orange-600 border-orange-300">
                    {whitePlayer.checkersOnBar} bar
                  </Badge>
                )}
              </div>

              {/* Match score if applicable */}
              {currentGameState.matchId && currentGameState.targetScore && (
                <div className="text-center px-3 flex-shrink-0">
                  <div className="text-xs text-muted-foreground">Score</div>
                  <div className="font-bold">
                    {currentGameState.player1Score} - {currentGameState.player2Score}
                  </div>
                </div>
              )}

              {/* Red player mini */}
              <div className="flex items-center gap-2 flex-1 min-w-0 justify-end">
                {redPlayer.checkersOnBar && redPlayer.checkersOnBar > 0 && (
                  <Badge variant="outline" className="text-xs text-orange-600 border-orange-300">
                    {redPlayer.checkersOnBar} bar
                  </Badge>
                )}
                <div className="truncate text-right">
                  <div className="text-sm font-medium truncate">{redPlayer.playerName}</div>
                  {redPlayer.isYou && <span className="text-xs text-muted-foreground">You</span>}
                </div>
                <div className="w-4 h-4 rounded-full bg-red-600 flex-shrink-0" />
              </div>
            </div>
          </div>

          {/* Desktop Sidebar - Hidden on mobile */}
          <div className="hidden lg:block space-y-4">
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
                isActive={currentGameState.status === GameStatus.InProgress && !currentGameState.isOpeningRoll && currentGameState.currentPlayer === CheckerColor.White}
                color={CheckerColor.White}
              />
            )}

            {currentGameState.matchId &&
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

            {/* Correspondence Game Deadline */}
            {currentGameState.isCorrespondence && (
              <CorrespondenceDeadline
                turnDeadline={currentGameState.turnDeadline}
                timePerMoveDays={currentGameState.timePerMoveDays}
                isYourTurn={currentGameState.isYourTurn}
              />
            )}

            <PlayerCard {...redPlayer} />

            {/* Red Player Timer */}
            {currentGameState.timeControlType && currentGameState.timeControlType !== 'None' && (
              <TimeDisplay
                reserveSeconds={currentGameState.redReserveSeconds ?? null}
                isInDelay={currentGameState.redIsInDelay ?? null}
                delayRemaining={currentGameState.redDelayRemaining ?? null}
                isActive={currentGameState.status === GameStatus.InProgress && !currentGameState.isOpeningRoll && currentGameState.currentPlayer === CheckerColor.Red}
                color={CheckerColor.Red}
              />
            )}

            <CompletedGameBanner gameState={currentGameState} />

            <GameControls gameState={currentGameState} isSpectator={isSpectator} />
          </div>

          {/* Main Board Area */}
          <div>
            <Card>
              <CardContent className="p-2 relative">
                {/* Turn Indicator Banner - prominent display of whose turn it is */}
                {currentGameState.winner === null && !isSpectator && !currentGameState.isAnalysisMode && (
                  <div className={`mb-4 p-3 rounded-lg flex items-center justify-center gap-2 ${
                    currentGameState.yourColor === currentGameState.currentPlayer
                      ? 'bg-green-100 dark:bg-green-950/50 border border-green-300 dark:border-green-800'
                      : 'bg-muted border border-border'
                  }`}>
                    {currentGameState.yourColor === currentGameState.currentPlayer ? (
                      <>
                        <div className="h-3 w-3 rounded-full bg-green-500 animate-pulse" />
                        <span className="font-semibold text-green-800 dark:text-green-200">
                          Your Turn
                        </span>
                        {currentGameState.dice[0] === 0 && currentGameState.dice[1] === 0 && (
                          <span className="text-sm text-green-700 dark:text-green-300">
                            — Roll the dice to start
                          </span>
                        )}
                      </>
                    ) : (
                      <>
                        <Clock className="h-4 w-4 text-muted-foreground" />
                        <span className="text-muted-foreground">
                          Waiting for {currentGameState.currentPlayer === CheckerColor.White
                            ? currentGameState.whitePlayerName
                            : currentGameState.redPlayerName}...
                        </span>
                      </>
                    )}
                  </div>
                )}

                {/* Spectator turn indicator */}
                {isSpectator && currentGameState.winner === null && (
                  <div className="mb-4 p-3 rounded-lg bg-muted border border-border flex items-center justify-center gap-2">
                    <div className={`h-3 w-3 rounded-full ${
                      currentGameState.currentPlayer === CheckerColor.White ? 'bg-amber-200' : 'bg-red-500'
                    }`} />
                    <span className="text-muted-foreground">
                      {currentGameState.currentPlayer === CheckerColor.White
                        ? currentGameState.whitePlayerName
                        : currentGameState.redPlayerName}'s turn
                    </span>
                  </div>
                )}

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
                  <GameCompletedOverlay />
                </div>

                {/* Move Log */}
                <MoveLog
                  turnHistory={currentGameState.turnHistory}
                  currentTurnMoves={currentGameState.currentTurnMoves}
                  currentPlayer={currentGameState.currentPlayer}
                  dice={currentGameState.dice}
                />
              </CardContent>
            </Card>

            {/* Mobile Game Controls - Only shown on mobile */}
            <div className="lg:hidden mt-4 space-y-4">
              <CompletedGameBanner gameState={currentGameState} />
              <GameControls gameState={currentGameState} isSpectator={isSpectator} />
            </div>
          </div>
        </div>
      </div>

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

      {/* Waiting for Double Response Modal - shown when we offered double and waiting for opponent */}
      {currentGameState && currentGameState.isAwaitingDoubleResponse && (
        <WaitingForDoubleResponseModal
          isOpen={currentGameState.isAwaitingDoubleResponse}
          currentValue={doublingCube.value}
          newValue={currentGameState.pendingDoubleNewValue}
        />
      )}

      {/* Double Offer Modal - shown when opponent offers double (using server state) */}
      {currentGameState && currentGameState.hasReceivedDoubleOffer && (
        <DoubleOfferModal
          isOpen={currentGameState.hasReceivedDoubleOffer}
          onClose={clearPendingDoubleOffer}
          currentStakes={doublingCube.value}
          newStakes={currentGameState.pendingDoubleNewValue}
        />
      )}

    </div>
  )
}
