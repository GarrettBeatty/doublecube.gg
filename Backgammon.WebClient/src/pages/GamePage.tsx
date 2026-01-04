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
import { CheckerColor } from '@/types/game.types'
import { Card, CardContent } from '@/components/ui/card'
import { Badge } from '@/components/ui/badge'
import { authService } from '@/services/auth.service'
import { Eye, BarChart3 } from 'lucide-react'

export const GamePage: React.FC = () => {
  const { gameId } = useParams<{ gameId: string }>()
  const navigate = useNavigate()
  const { invoke, isConnected } = useSignalR()

  const { currentGameState, isSpectator, setCurrentGameId, resetGame } = useGameStore()
  const [isLoading, setIsLoading] = useState(true)
  const [lastJoinedGameId, setLastJoinedGameId] = useState<string | null>(null)
  const lastJoinedGameIdRef = useRef<string | null>(null)

  // Keep ref in sync with state
  useEffect(() => {
    lastJoinedGameIdRef.current = lastJoinedGameId
  }, [lastJoinedGameId])

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
      setLastJoinedGameId(gameId)

      // Join the game
      try {
        const playerId = authService.getOrCreatePlayerId()
        console.log('[GamePage] Joining game:', gameId)
        await invoke(HubMethods.JoinGame, playerId, gameId)
        // The GameStart or GameUpdate event will set the game state
        setIsLoading(false)
      } catch (error) {
        console.error('[GamePage] Failed to join game:', error)
        setIsLoading(false)
        // Still show the page, game state might arrive via other events
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
    checkersOnBar: currentGameState.whiteCheckersOnBar,
    bornOff: currentGameState.whiteBornOff,
  }

  const redPlayer = {
    playerName: currentGameState.redPlayerName || 'Red',
    username: currentGameState.redUsername,
    color: CheckerColor.Red,
    isYourTurn: currentGameState.currentPlayer === CheckerColor.Red,
    isYou: currentGameState.yourColor === CheckerColor.Red,
    checkersOnBar: currentGameState.redCheckersOnBar,
    bornOff: currentGameState.redBornOff,
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

            {/* Doubling Cube */}
            {currentGameState.doublingCubeValue > 1 && (
              <Card>
                <CardContent className="p-4 text-center">
                  <div className="text-sm text-muted-foreground mb-2">Stakes</div>
                  <div className="text-3xl font-bold text-yellow-500">
                    {currentGameState.doublingCubeValue}x
                  </div>
                  {currentGameState.doublingCubeOwner !== null && (
                    <div className="text-xs text-muted-foreground mt-1">
                      Owned by{' '}
                      {currentGameState.doublingCubeOwner === CheckerColor.White
                        ? 'White'
                        : 'Red'}
                    </div>
                  )}
                </CardContent>
              </Card>
            )}

            <GameControls gameState={currentGameState} isSpectator={isSpectator} />
          </div>

          {/* Main Board Area */}
          <div>
            <Card>
              <CardContent className="p-2 relative">
                {currentGameState.isAnalysisMode && (
                  <Badge variant="secondary" className="mb-4">
                    <BarChart3 className="h-4 w-4 mr-2" />
                    Analysis Mode
                  </Badge>
                )}

                {/* Board with overlay controls */}
                <div className="relative">
                  <BoardSVG gameState={currentGameState} />
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
    </div>
  )
}
