import React from 'react'
import { useNavigate } from 'react-router-dom'
import { useSignalR } from '@/contexts/SignalRContext'
import { useGameStore } from '@/stores/gameStore'
import { CheckerColor } from '@/types/generated/Backgammon.Core'
import { Button } from '@/components/ui/button'
import { Badge } from '@/components/ui/badge'
import { Card, CardContent, CardHeader, CardTitle, CardFooter } from '@/components/ui/card'
import { Trophy, Target, BarChart3, Home, ArrowRight } from 'lucide-react'

export const GameCompletedOverlay: React.FC = () => {
  const navigate = useNavigate()
  const { hub } = useSignalR()
  const {
    lastGameWinner,
    lastGamePoints,
    matchState,
    currentGameState,
    myColor,
  } = useGameStore()
  const [isContinuing, setIsContinuing] = React.useState(false)

  // Don't show if there's no winner (game not complete)
  // Note: Can't use !lastGameWinner since CheckerColor.White = 0 is falsy
  if (lastGameWinner === null || !currentGameState) return null

  const handleContinueMatch = async () => {
    if (!matchState || isContinuing) return

    setIsContinuing(true)
    try {
      console.log('[GameCompletedOverlay] Continuing match:', matchState.matchId)
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

  const handlePlayAgain = () => {
    navigate('/')
  }

  const handleViewAnalysis = async () => {
    if (!currentGameState?.gameId) return
    try {
      // Export full game SGF with move history
      const sgf = await hub?.exportGameSgf()
      if (sgf) {
        const encodedSgf = encodeURIComponent(sgf)
        navigate(`/analysis/${encodedSgf}`)
      } else {
        // Fallback to position-only export
        const positionSgf = await hub?.exportPosition()
        if (positionSgf) {
          navigate(`/analysis/${encodeURIComponent(positionSgf)}`)
        } else {
          navigate('/')
        }
      }
    } catch (error) {
      console.error('Failed to export game for analysis:', error)
      navigate('/')
    }
  }

  const isMatchGame = currentGameState.matchId && matchState
  const isWinner = lastGameWinner === myColor
  const winnerName =
    lastGameWinner === CheckerColor.White
      ? currentGameState.whitePlayerName
      : currentGameState.redPlayerName

  // For match score display
  const isWhitePlayer1 = true

  const getTitle = () => {
    if (!isMatchGame) return 'Game Over!'
    if (matchState.matchComplete) return 'Match Complete!'
    return 'Game Complete!'
  }

  // Adjust points display based on cube value (already incorporated in lastGamePoints)
  const basePoints = lastGamePoints / (currentGameState.doublingCubeValue || 1)
  const getPointsLabel = () => {
    if (basePoints === 3) return 'Backgammon'
    if (basePoints === 2) return 'Gammon'
    return 'Normal'
  }

  return (
    <div className="absolute inset-0 bg-background/80 backdrop-blur-sm flex items-center justify-center z-10 rounded-lg">
      <Card className="w-full max-w-md mx-4 shadow-lg border-2">
        <CardHeader className="text-center pb-2">
          <div className="flex items-center justify-center gap-2 mb-2">
            <Trophy className={`h-8 w-8 ${isWinner ? 'text-yellow-500' : 'text-muted-foreground'}`} />
          </div>
          <CardTitle className="text-2xl">{getTitle()}</CardTitle>
          <p className="text-muted-foreground">
            {isWinner ? 'Congratulations! You won!' : 'Better luck next time!'}
          </p>
        </CardHeader>

        <CardContent className="space-y-4">
          {/* Game Result */}
          <div className="rounded-lg bg-muted p-4 space-y-3">
            <div className="flex items-center justify-between">
              <span className="font-semibold">Winner:</span>
              <span className={isWinner ? 'text-green-600 font-bold' : 'text-foreground'}>
                {winnerName}
              </span>
            </div>
            <div className="flex items-center justify-between">
              <span className="font-semibold">Points Scored:</span>
              <Badge variant={lastGamePoints > 1 ? 'default' : 'secondary'}>
                {lastGamePoints} {lastGamePoints === 1 ? 'point' : 'points'}
                {basePoints > 1 && ` (${getPointsLabel()})`}
              </Badge>
            </div>
          </div>

          {/* Match Score - only shown for match games */}
          {isMatchGame && (
            <div className="rounded-lg border p-4 space-y-2">
              <div className="flex items-center gap-2 mb-2">
                <Target className="h-4 w-4" />
                <span className="font-semibold">
                  Match Score (to {matchState.targetScore})
                </span>
              </div>
              <div className="flex items-center justify-between">
                <span className={myColor === CheckerColor.White ? 'font-bold' : ''}>
                  {currentGameState.whitePlayerName}:
                </span>
                <span className={myColor === CheckerColor.White ? 'font-bold text-lg' : ''}>
                  {isWhitePlayer1 ? matchState.player1Score : matchState.player2Score}
                </span>
              </div>
              <div className="flex items-center justify-between">
                <span className={myColor === CheckerColor.Red ? 'font-bold' : ''}>
                  {currentGameState.redPlayerName}:
                </span>
                <span className={myColor === CheckerColor.Red ? 'font-bold text-lg' : ''}>
                  {isWhitePlayer1 ? matchState.player2Score : matchState.player1Score}
                </span>
              </div>
            </div>
          )}

          {/* Crawford Game Indicator */}
          {isMatchGame && matchState.isCrawfordGame && !matchState.matchComplete && (
            <div className="rounded-lg bg-amber-500/10 border border-amber-500/20 p-3 text-sm">
              <span className="font-semibold text-amber-700 dark:text-amber-400">
                Crawford Game:
              </span>
              <span className="ml-2">Doubling cube is disabled for the next game</span>
            </div>
          )}

          {/* Match Complete Message */}
          {isMatchGame && matchState.matchComplete && (
            <div className="rounded-lg bg-green-500/10 border border-green-500/20 p-3">
              <p className="text-center font-semibold text-green-700 dark:text-green-400">
                {matchState.matchWinner ? (
                  matchState.matchWinner === currentGameState.whitePlayerId ? (
                    myColor === CheckerColor.White ? 'You won the match!' : `${currentGameState.whitePlayerName} won the match!`
                  ) : (
                    myColor === CheckerColor.Red ? 'You won the match!' : `${currentGameState.redPlayerName} won the match!`
                  )
                ) : 'Match is complete!'}
              </p>
            </div>
          )}
        </CardContent>

        <CardFooter className="flex flex-wrap gap-2 justify-center pt-2">
          {isMatchGame ? (
            matchState.matchComplete ? (
              <>
                <Button variant="outline" onClick={handleViewAnalysis}>
                  <BarChart3 className="h-4 w-4 mr-2" />
                  View in Analysis
                </Button>
                <Button onClick={handleViewMatchResults}>
                  <Trophy className="h-4 w-4 mr-2" />
                  View Match Results
                </Button>
              </>
            ) : (
              <>
                <Button variant="outline" onClick={handleViewAnalysis} disabled={isContinuing}>
                  <BarChart3 className="h-4 w-4 mr-2" />
                  View in Analysis
                </Button>
                <Button onClick={handleContinueMatch} disabled={isContinuing}>
                  {isContinuing ? (
                    'Starting next game...'
                  ) : (
                    <>
                      <ArrowRight className="h-4 w-4 mr-2" />
                      Continue to Next Game
                    </>
                  )}
                </Button>
              </>
            )
          ) : (
            <>
              <Button variant="outline" onClick={handleViewAnalysis}>
                <BarChart3 className="h-4 w-4 mr-2" />
                View in Analysis
              </Button>
              <Button onClick={handlePlayAgain}>
                <Home className="h-4 w-4 mr-2" />
                Play Again
              </Button>
            </>
          )}
        </CardFooter>
      </Card>
    </div>
  )
}
