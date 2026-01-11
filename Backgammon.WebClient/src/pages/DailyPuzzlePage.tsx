import { useEffect, useState } from 'react'
import { useNavigate } from 'react-router-dom'
import { useSignalR } from '@/contexts/SignalRContext'
import { HubMethods } from '@/types/signalr.types'
import { Card, CardContent, CardHeader, CardTitle } from '@/components/ui/card'
import { Button } from '@/components/ui/button'
import { Badge } from '@/components/ui/badge'
import { Brain, ArrowLeft, RotateCcw, Check, X, Flame, Calendar, Eye } from 'lucide-react'
import { useToast } from '@/hooks/use-toast'
import { usePuzzleStore, formatMovesForSubmission } from '@/stores/puzzleStore'
import { DailyPuzzle, PuzzleResult as PuzzleResultType, PuzzleStreakInfo } from '@/types/puzzle.types'
import { PuzzleBoard } from '@/components/puzzle/PuzzleBoard'
import { PuzzleResult } from '@/components/puzzle/PuzzleResult'

export const DailyPuzzlePage: React.FC = () => {
  const navigate = useNavigate()
  const { invoke, isConnected } = useSignalR()
  const { toast } = useToast()
  const [isSubmitting, setIsSubmitting] = useState(false)
  const [isGivingUp, setIsGivingUp] = useState(false)
  const [hasGivenUp, setHasGivenUp] = useState(false)
  const [revealedAnswer, setRevealedAnswer] = useState<string | null>(null)

  const {
    currentPuzzle,
    isLoading,
    error,
    pendingMoves,
    remainingDice,
    result,
    showResultModal,
    streakInfo,
    setPuzzle,
    setLoading,
    setError,
    setResult,
    setShowResultModal,
    setStreakInfo,
    clearMoves,
    canSubmit,
    undoLastMove,
  } = usePuzzleStore()

  // Load puzzle on mount
  useEffect(() => {
    const loadPuzzle = async () => {
      if (!isConnected) return

      setLoading(true)
      setError(null)

      try {
        const puzzle = await invoke<DailyPuzzle>(HubMethods.GetDailyPuzzle)
        setPuzzle(puzzle)

        // Also load streak info
        const streak = await invoke<PuzzleStreakInfo>(HubMethods.GetPuzzleStreak)
        setStreakInfo(streak)
      } catch (err) {
        console.error('Failed to load puzzle:', err)
        setError('Failed to load today\'s puzzle')
        toast({
          title: 'Error',
          description: 'Failed to load today\'s puzzle',
          variant: 'destructive',
        })
      } finally {
        setLoading(false)
      }
    }

    loadPuzzle()
  }, [isConnected, invoke, setPuzzle, setLoading, setError, setStreakInfo, toast])

  const handleSubmit = async () => {
    if (!canSubmit() || isSubmitting) return

    setIsSubmitting(true)
    try {
      const moves = formatMovesForSubmission(pendingMoves)
      const puzzleResult = await invoke<PuzzleResultType>(HubMethods.SubmitPuzzleAnswer, moves)
      if (!puzzleResult) {
        throw new Error('No result returned')
      }

      setResult(puzzleResult)
      setShowResultModal(true)

      // Update streak info
      if (streakInfo) {
        setStreakInfo({
          ...streakInfo,
          currentStreak: puzzleResult.currentStreak,
        })
      }

      // If correct, update the puzzle to show it's solved
      if (puzzleResult.isCorrect && currentPuzzle) {
        setPuzzle({
          ...currentPuzzle,
          alreadySolved: true,
          bestMoves: puzzleResult.bestMoves,
          bestMovesNotation: puzzleResult.bestMovesNotation,
        })
      }
    } catch (err) {
      console.error('Failed to submit answer:', err)
      toast({
        title: 'Error',
        description: 'Failed to submit your answer',
        variant: 'destructive',
      })
    } finally {
      setIsSubmitting(false)
    }
  }

  const handleReset = () => {
    clearMoves()
    setResult(null)
  }

  const handleGiveUp = async () => {
    if (isGivingUp || hasGivenUp || currentPuzzle?.alreadySolved) return

    setIsGivingUp(true)
    try {
      const puzzleResult = await invoke<PuzzleResultType>(HubMethods.GiveUpPuzzle)
      if (!puzzleResult) {
        throw new Error('No result returned')
      }

      setHasGivenUp(true)
      setRevealedAnswer(puzzleResult.bestMovesNotation || null)

      // Update streak info (giving up resets streak)
      if (streakInfo) {
        setStreakInfo({
          ...streakInfo,
          currentStreak: 0,
        })
      }

      toast({
        title: 'Answer Revealed',
        description: 'Your streak has been reset.',
      })
    } catch (err) {
      console.error('Failed to give up:', err)
      toast({
        title: 'Error',
        description: 'Failed to reveal the answer',
        variant: 'destructive',
      })
    } finally {
      setIsGivingUp(false)
    }
  }

  const handleBack = () => {
    navigate('/')
  }

  if (isLoading) {
    return (
      <div className="min-h-screen bg-background flex items-center justify-center">
        <Card className="w-96">
          <CardContent className="p-8">
            <div className="text-center space-y-4">
              <Brain className="h-12 w-12 text-purple-500 animate-pulse mx-auto" />
              <p className="text-muted-foreground">Loading today's puzzle...</p>
              <div className="flex justify-center">
                <div className="animate-spin rounded-full h-8 w-8 border-b-2 border-primary"></div>
              </div>
            </div>
          </CardContent>
        </Card>
      </div>
    )
  }

  if (error || !currentPuzzle) {
    return (
      <div className="min-h-screen bg-background flex items-center justify-center">
        <Card className="w-96">
          <CardContent className="p-8">
            <div className="text-center space-y-4">
              <X className="h-12 w-12 text-destructive mx-auto" />
              <p className="text-muted-foreground">{error || 'No puzzle available'}</p>
              <Button onClick={handleBack}>Back to Home</Button>
            </div>
          </CardContent>
        </Card>
      </div>
    )
  }

  return (
    <div className="min-h-screen bg-background">
      <div className="max-w-[1920px] mx-auto px-2 py-4">
        {/* Header */}
        <div className="mb-4 flex items-center gap-4">
          <Button variant="ghost" size="icon" onClick={handleBack}>
            <ArrowLeft className="h-5 w-5" />
          </Button>
          <Badge variant="secondary" className="text-lg py-2 px-4">
            <Brain className="h-5 w-5 mr-2 text-purple-500" />
            Daily Puzzle
          </Badge>
          {currentPuzzle.alreadySolved && (
            <Badge variant="default" className="bg-green-600 py-2 px-4">
              <Check className="h-4 w-4 mr-1" />
              Solved
            </Badge>
          )}
        </div>

        <div className="grid grid-cols-1 xl:grid-cols-[280px_1fr_320px] gap-3">
          {/* Left Sidebar - Puzzle Info */}
          <div className="space-y-3">
            {/* Date Card */}
            <Card>
              <CardContent className="p-4">
                <div className="flex items-center gap-3">
                  <Calendar className="h-10 w-10 text-muted-foreground" />
                  <div>
                    <p className="text-sm text-muted-foreground">Puzzle Date</p>
                    <p className="font-semibold">
                      {new Date(currentPuzzle.puzzleDate).toLocaleDateString('en-US', {
                        weekday: 'short',
                        month: 'short',
                        day: 'numeric',
                      })}
                    </p>
                  </div>
                </div>
              </CardContent>
            </Card>

            {/* Player to Move */}
            <Card>
              <CardContent className="p-4">
                <div className="flex items-center justify-between">
                  <div>
                    <p className="text-sm text-muted-foreground">To Play</p>
                    <p className="font-semibold text-lg">{currentPuzzle.currentPlayer}</p>
                  </div>
                  <div
                    className={`w-8 h-8 rounded-full border-2 ${
                      currentPuzzle.currentPlayer === 'White'
                        ? 'bg-white border-gray-300'
                        : 'bg-red-500 border-red-600'
                    }`}
                  />
                </div>
              </CardContent>
            </Card>

            {/* Streak Info */}
            {streakInfo && (
              <Card>
                <CardContent className="p-4">
                  <div className="flex items-center gap-3">
                    <Flame className="h-10 w-10 text-orange-500" />
                    <div>
                      <p className="text-sm text-muted-foreground">Current Streak</p>
                      <p className="font-bold text-2xl">{streakInfo.currentStreak}</p>
                    </div>
                  </div>
                  {streakInfo.bestStreak > 0 && (
                    <p className="text-xs text-muted-foreground mt-2">
                      Best: {streakInfo.bestStreak} days
                    </p>
                  )}
                </CardContent>
              </Card>
            )}

            {/* Attempts info */}
            {currentPuzzle.attemptsToday > 0 && (
              <Card>
                <CardContent className="p-4">
                  <p className="text-sm text-center text-muted-foreground">
                    Attempts today: {currentPuzzle.attemptsToday}
                  </p>
                </CardContent>
              </Card>
            )}
          </div>

          {/* Main Board Area */}
          <div className="space-y-3">
            <Card>
              <CardContent className="p-2">
                <PuzzleBoard puzzle={currentPuzzle} />
              </CardContent>
            </Card>
          </div>

          {/* Right Sidebar - Controls & Analysis */}
          <div className="space-y-3">
            {/* Dice display */}
            <Card>
              <CardHeader className="pb-2">
                <CardTitle className="text-sm">Dice Roll</CardTitle>
              </CardHeader>
              <CardContent>
                <div className="flex items-center justify-center gap-4">
                  {currentPuzzle.dice.map((die, i) => {
                    // Count how many of this die value are remaining
                    const usedCount = currentPuzzle.dice
                      .slice(0, i)
                      .filter((d) => d === die).length
                    const remainingOfThisValue = remainingDice.filter((d) => d === die).length
                    const isUsed = usedCount >= remainingOfThisValue

                    return (
                      <div
                        key={i}
                        className={`w-14 h-14 rounded-lg flex items-center justify-center text-2xl font-bold shadow-md ${
                          !isUsed
                            ? 'bg-white text-gray-900'
                            : 'bg-muted text-muted-foreground opacity-50'
                        }`}
                      >
                        {die}
                      </div>
                    )
                  })}
                </div>
                <p className="text-xs text-center text-muted-foreground mt-3">
                  {remainingDice.length === 0
                    ? 'All dice used'
                    : `${remainingDice.length} move${remainingDice.length !== 1 ? 's' : ''} remaining`}
                </p>
              </CardContent>
            </Card>

            {/* Your Moves */}
            <Card>
              <CardHeader className="pb-2">
                <CardTitle className="text-sm">Your Moves</CardTitle>
              </CardHeader>
              <CardContent>
                {pendingMoves.length > 0 ? (
                  <div className="space-y-2">
                    {pendingMoves.map((move, i) => (
                      <div
                        key={i}
                        className="flex items-center justify-between bg-muted/50 rounded px-3 py-2"
                      >
                        <span className="font-mono">
                          {move.from === 0 ? 'bar' : move.from}/
                          {move.to === 25 || move.to === 0 ? 'off' : move.to}
                          {move.isHit && <span className="text-red-500">*</span>}
                        </span>
                        <Badge variant="outline">{move.dieValue}</Badge>
                      </div>
                    ))}
                  </div>
                ) : (
                  <p className="text-sm text-muted-foreground text-center py-4">
                    Make your moves on the board
                  </p>
                )}
              </CardContent>
            </Card>

            {/* Best Move (shown after solving or giving up) */}
            {((currentPuzzle.alreadySolved && currentPuzzle.bestMovesNotation && remainingDice.length === 0) ||
              (hasGivenUp && revealedAnswer)) && (
              <Card className={currentPuzzle.alreadySolved ? 'bg-green-500/10 border-green-500/30' : 'bg-yellow-500/10 border-yellow-500/30'}>
                <CardHeader className="pb-2">
                  <CardTitle className={`text-sm ${currentPuzzle.alreadySolved ? 'text-green-400' : 'text-yellow-400'}`}>
                    Best Move
                  </CardTitle>
                </CardHeader>
                <CardContent>
                  <p className="font-mono text-lg">
                    {currentPuzzle.bestMovesNotation || revealedAnswer}
                  </p>
                </CardContent>
              </Card>
            )}

            {/* Action buttons */}
            <Card>
              <CardContent className="p-4 space-y-3">
                {!currentPuzzle.alreadySolved && !hasGivenUp && (
                  <Button
                    className="w-full"
                    size="lg"
                    onClick={handleSubmit}
                    disabled={!canSubmit() || isSubmitting}
                  >
                    {isSubmitting ? 'Submitting...' : 'Submit Answer'}
                  </Button>
                )}

                <div className="flex gap-2">
                  <Button
                    variant="outline"
                    className="flex-1"
                    onClick={undoLastMove}
                    disabled={pendingMoves.length === 0}
                  >
                    Undo
                  </Button>
                  <Button
                    variant="outline"
                    className="flex-1"
                    onClick={handleReset}
                    disabled={
                      pendingMoves.length === 0 &&
                      remainingDice.length === currentPuzzle.dice.length
                    }
                  >
                    <RotateCcw className="h-4 w-4 mr-1" />
                    Reset
                  </Button>
                </div>

                {/* Show Answer button - only visible when puzzle is not solved and user hasn't given up */}
                {!currentPuzzle.alreadySolved && !hasGivenUp && (
                  <Button
                    variant="ghost"
                    className="w-full text-muted-foreground hover:text-foreground"
                    onClick={handleGiveUp}
                    disabled={isGivingUp}
                  >
                    <Eye className="h-4 w-4 mr-2" />
                    {isGivingUp ? 'Revealing...' : 'Show Answer'}
                  </Button>
                )}
              </CardContent>
            </Card>
          </div>
        </div>

        {/* Result modal */}
        <PuzzleResult
          result={result}
          isOpen={showResultModal}
          onClose={() => setShowResultModal(false)}
          onTryAgain={handleReset}
        />
      </div>
    </div>
  )
}
