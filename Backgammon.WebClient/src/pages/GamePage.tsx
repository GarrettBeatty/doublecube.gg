import React, { useEffect, useState } from 'react'
import { useParams, useNavigate } from 'react-router-dom'
import { useGameStore } from '@/stores/gameStore'
import { useSignalR } from '@/contexts/SignalRContext'
import { HubMethods } from '@/types/signalr.types'
import { BoardSVG } from '@/components/game/BoardSVG'
import { PlayerCard } from '@/components/game/PlayerCard'
import { GameControls } from '@/components/game/GameControls'
import { MatchInfo } from '@/components/game/MatchInfo'
import { CheckerColor } from '@/types/game.types'
import { Card, CardContent } from '@/components/ui/card'
import { Badge } from '@/components/ui/badge'
import { Button } from '@/components/ui/button'
import { authService } from '@/services/auth.service'
import { Eye, BarChart3 } from 'lucide-react'

export const GamePage: React.FC = () => {
  const { gameId } = useParams<{ gameId: string }>()
  const navigate = useNavigate()
  const { invoke } = useSignalR()

  const { currentGameState, isSpectator, setCurrentGameId, resetGame } = useGameStore()
  const [isLoading, setIsLoading] = useState(true)
  const [lastJoinedGameId, setLastJoinedGameId] = useState<string | null>(null)

  useEffect(() => {
    const joinGame = async () => {
      if (!gameId) {
        navigate('/')
        return
      }

      // Skip if we've already joined this game
      if (lastJoinedGameId === gameId) {
        console.log('[GamePage] Already joined game:', gameId)
        return
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
  }, [gameId, navigate, setCurrentGameId, invoke, resetGame, lastJoinedGameId])

  const handleLeaveGame = async () => {
    try {
      console.log('[GamePage] Leaving game:', gameId)
      await invoke(HubMethods.LeaveGame)
      resetGame()
      setLastJoinedGameId(null)
      navigate('/')
    } catch (error) {
      console.error('[GamePage] Failed to leave game:', error)
      resetGame()
      setLastJoinedGameId(null)
      navigate('/')
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
            <div className="flex items-center justify-between mb-4">
              <Button
                variant="outline"
                onClick={handleLeaveGame}
                className="bg-white/10 hover:bg-white/20"
              >
                ← Leave Game
              </Button>
            </div>

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
              <CardContent className="p-2">
                {currentGameState.isAnalysisMode && (
                  <Badge variant="secondary" className="mb-4">
                    <BarChart3 className="h-4 w-4 mr-2" />
                    Analysis Mode
                  </Badge>
                )}

                <BoardSVG gameState={currentGameState} />

                {/* Dice Display - Only show if dice have non-zero values */}
                {currentGameState.dice &&
                  currentGameState.dice.length > 0 &&
                  currentGameState.dice.some((d) => d > 0) && (
                    <div className="mt-4 text-center">
                      <div className="inline-flex gap-2 bg-white/10 p-3 rounded-lg">
                        {currentGameState.dice.map((die, index) => (
                          <div
                            key={index}
                            className="w-12 h-12 bg-white rounded flex items-center justify-center text-2xl font-bold text-gray-800 shadow-lg"
                          >
                            {die}
                          </div>
                        ))}
                      </div>
                      {currentGameState.remainingMoves && (
                        <div className="text-sm text-white/70 mt-2">
                          {currentGameState.remainingMoves.length} moves remaining
                        </div>
                      )}
                    </div>
                  )}
              </CardContent>
            </Card>
          </div>
        </div>
      </div>
    </div>
  )
}
