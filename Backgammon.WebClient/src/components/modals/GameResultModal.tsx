import React from 'react'
import { useNavigate } from 'react-router-dom'
import { useSignalR } from '@/contexts/SignalRContext'
import { useGameStore } from '@/stores/gameStore'
import { CheckerColor } from '@/types/game.types'
import {
  Dialog,
  DialogContent,
  DialogDescription,
  DialogHeader,
  DialogTitle,
  DialogFooter,
} from '@/components/ui/dialog'
import { Button } from '@/components/ui/button'
import { Badge } from '@/components/ui/badge'
import { Trophy, Target } from 'lucide-react'

export const GameResultModal: React.FC = () => {
  const navigate = useNavigate()
  const { hub } = useSignalR()
  const {
    showGameResultModal,
    setShowGameResultModal,
    lastGameWinner,
    lastGamePoints,
    matchState,
    currentGameState,
    myColor,
  } = useGameStore()
  const [isContinuing, setIsContinuing] = React.useState(false)

  const handleContinueMatch = async () => {
    if (!matchState || isContinuing) return

    setIsContinuing(true)
    try {
      // Call server method to create and start next game
      console.log('[GameResultModal] Continuing match:', matchState.matchId)
      await hub?.continueMatch(matchState.matchId)

      // Keep modal open until GameStart event received
      // Modal will close when useSignalREvents receives GameStart and navigates
    } catch (error) {
      console.error('Failed to continue match:', error)
      setShowGameResultModal(false)
      setIsContinuing(false)
    }
  }

  // Reset continuing state when modal closes
  React.useEffect(() => {
    if (!showGameResultModal) {
      setIsContinuing(false)
    }
  }, [showGameResultModal])

  const handleViewMatchResults = () => {
    if (!matchState?.matchId) return
    setShowGameResultModal(false)
    navigate(`/match/${matchState.matchId}/results`)
  }

  const handleClose = () => {
    setShowGameResultModal(false)
  }

  const handlePlayAgain = () => {
    setShowGameResultModal(false)
    navigate('/')
  }

  const handleViewAnalysis = async () => {
    if (!currentGameState?.gameId) return
    setShowGameResultModal(false)
    try {
      const sgf = await hub?.exportPosition()
      if (sgf) {
        const encodedSgf = encodeURIComponent(sgf)
        navigate(`/analysis/${encodedSgf}`)
      } else {
        // Fallback to home if export fails
        navigate('/')
      }
    } catch (error) {
      console.error('Failed to export position for analysis:', error)
      navigate('/')
    }
  }

  if (!currentGameState) return null

  // Determine if this is a match game with valid match state
  const isMatchGame = currentGameState.matchId && matchState

  const isWinner = lastGameWinner === myColor
  const winnerName =
    lastGameWinner === CheckerColor.White
      ? currentGameState.whitePlayerName
      : currentGameState.redPlayerName

  // Assuming player1 is White and player2 is Red (standard match setup)
  const isWhitePlayer1 = true

  // Determine the title based on game type
  const getTitle = () => {
    if (!isMatchGame) return 'Game Over!'
    if (matchState.matchComplete) return 'Match Complete!'
    return 'Game Complete!'
  }

  return (
    <Dialog open={showGameResultModal} onOpenChange={setShowGameResultModal}>
      <DialogContent className="sm:max-w-md">
        <DialogHeader>
          <DialogTitle className="flex items-center gap-2">
            <Trophy className={isWinner ? 'text-yellow-500' : 'text-muted-foreground'} />
            {getTitle()}
          </DialogTitle>
          <DialogDescription>
            {isWinner ? 'Congratulations! You won!' : 'Better luck next time!'}
          </DialogDescription>
        </DialogHeader>

        <div className="space-y-4 py-4">
          {/* Game Result */}
          <div className="rounded-lg bg-muted p-4 space-y-2">
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
                {lastGamePoints === 2 && ' (Gammon)'}
                {lastGamePoints === 3 && ' (Backgammon)'}
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

          {/* Crawford Game Indicator - only for match games */}
          {isMatchGame && matchState.isCrawfordGame && !matchState.matchComplete && (
            <div className="rounded-lg bg-amber-500/10 border border-amber-500/20 p-3 text-sm">
              <span className="font-semibold text-amber-700 dark:text-amber-400">
                Crawford Game:
              </span>
              <span className="ml-2">Doubling cube is disabled for this game</span>
            </div>
          )}

          {/* Match Complete Message - only for match games */}
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
        </div>

        <DialogFooter className="gap-2">
          {isMatchGame ? (
            // Match game buttons
            matchState.matchComplete ? (
              <>
                <Button variant="outline" onClick={handleClose}>
                  Close
                </Button>
                <Button variant="outline" onClick={handleViewAnalysis}>
                  View in Analysis
                </Button>
                <Button onClick={handleViewMatchResults}>
                  View Match Results
                </Button>
              </>
            ) : (
              <>
                <Button variant="outline" onClick={handleClose} disabled={isContinuing}>
                  Stay Here
                </Button>
                <Button variant="outline" onClick={handleViewAnalysis} disabled={isContinuing}>
                  View in Analysis
                </Button>
                <Button onClick={handleContinueMatch} disabled={isContinuing}>
                  {isContinuing ? 'Starting next game...' : 'Continue to Next Game'}
                </Button>
              </>
            )
          ) : (
            // Standalone game buttons
            <>
              <Button variant="outline" onClick={handleClose}>
                Close
              </Button>
              <Button variant="outline" onClick={handleViewAnalysis}>
                View in Analysis
              </Button>
              <Button onClick={handlePlayAgain}>
                Play Again
              </Button>
            </>
          )}
        </DialogFooter>
      </DialogContent>
    </Dialog>
  )
}
