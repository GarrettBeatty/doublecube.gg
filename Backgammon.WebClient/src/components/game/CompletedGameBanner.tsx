import React from 'react'
import { useNavigate } from 'react-router-dom'
import { useSignalR } from '@/contexts/SignalRContext'
import { useGameStore } from '@/stores/gameStore'
import { type GameState, GameStatus } from '@/types/generated/Backgammon.Server.Models'
import { CheckerColor } from '@/types/generated/Backgammon.Core'
import { Button } from '@/components/ui/button'
import { Card, CardContent } from '@/components/ui/card'
import { Badge } from '@/components/ui/badge'
import { Trophy, ArrowRight, BarChart3 } from 'lucide-react'

interface CompletedGameBannerProps {
  gameState: GameState
}

export const CompletedGameBanner: React.FC<CompletedGameBannerProps> = ({ gameState }) => {
  const navigate = useNavigate()
  const { hub } = useSignalR()
  const { matchState, myColor, lastGameWinner } = useGameStore()
  const [isContinuing, setIsContinuing] = React.useState(false)

  // Only show for completed games
  if (gameState.status !== GameStatus.Completed) return null

  // Don't show if the overlay is showing (lastGameWinner is set from GameOver event)
  if (lastGameWinner !== null) return null

  // Need a winner to show anything meaningful
  if (gameState.winner === undefined) return null

  const isMatchGame = gameState.matchId && matchState
  const isWinner = gameState.winner === myColor
  const winnerName =
    gameState.winner === CheckerColor.White
      ? gameState.whitePlayerName
      : gameState.redPlayerName

  // Calculate points from winType
  const getPoints = () => {
    const cubeValue = gameState.doublingCubeValue || 1
    if (gameState.winType === 'Backgammon') return 3 * cubeValue
    if (gameState.winType === 'Gammon') return 2 * cubeValue
    return cubeValue
  }
  const points = getPoints()

  const handleContinueMatch = async () => {
    if (!matchState || isContinuing) return

    setIsContinuing(true)
    try {
      await hub?.continueMatch(matchState.matchId)
      // Navigation happens via GameStart event
    } catch (error) {
      console.error('Failed to continue match:', error)
      setIsContinuing(false)
    }
  }

  const handleViewMatchResults = () => {
    if (!matchState?.matchId) return
    navigate(`/match/${matchState.matchId}/results`)
  }

  const handleViewAnalysis = async () => {
    try {
      const sgf = await hub?.exportPosition()
      if (sgf) {
        const encodedSgf = encodeURIComponent(sgf)
        navigate(`/analysis/${encodedSgf}`)
      }
    } catch (error) {
      console.error('Failed to export position for analysis:', error)
    }
  }

  const handlePlayAgain = () => {
    navigate('/')
  }

  return (
    <Card className="border-2 border-primary/20 bg-primary/5">
      <CardContent className="p-4">
        <div className="flex flex-col gap-3">
          {/* Result Header */}
          <div className="flex items-center justify-between">
            <div className="flex items-center gap-2">
              <Trophy className={`h-5 w-5 ${isWinner ? 'text-yellow-500' : 'text-muted-foreground'}`} />
              <span className="font-semibold">
                {isWinner ? 'You Won!' : `${winnerName} Won`}
              </span>
            </div>
            <Badge variant={points > 1 ? 'default' : 'secondary'}>
              {points} {points === 1 ? 'point' : 'points'}
              {gameState.winType && ` (${gameState.winType})`}
            </Badge>
          </div>

          {/* Match Score */}
          {isMatchGame && (
            <div className="text-sm text-muted-foreground">
              Match Score: {gameState.whitePlayerName} {matchState.player1Score} - {matchState.player2Score} {gameState.redPlayerName}
              {' '}(to {matchState.targetScore})
            </div>
          )}

          {/* Match Complete Message */}
          {isMatchGame && matchState.matchComplete && (
            <div className="text-sm font-medium text-green-600 dark:text-green-400">
              Match Complete!
            </div>
          )}

          {/* Action Buttons */}
          <div className="flex flex-wrap gap-2">
            <Button variant="outline" size="sm" onClick={handleViewAnalysis}>
              <BarChart3 className="h-4 w-4 mr-1" />
              Analysis
            </Button>

            {isMatchGame ? (
              matchState.matchComplete ? (
                <Button size="sm" onClick={handleViewMatchResults}>
                  <Trophy className="h-4 w-4 mr-1" />
                  Match Results
                </Button>
              ) : (
                <Button size="sm" onClick={handleContinueMatch} disabled={isContinuing}>
                  {isContinuing ? (
                    'Starting...'
                  ) : (
                    <>
                      <ArrowRight className="h-4 w-4 mr-1" />
                      Continue Match
                    </>
                  )}
                </Button>
              )
            ) : (
              <Button size="sm" onClick={handlePlayAgain}>
                Play Again
              </Button>
            )}
          </div>
        </div>
      </CardContent>
    </Card>
  )
}
