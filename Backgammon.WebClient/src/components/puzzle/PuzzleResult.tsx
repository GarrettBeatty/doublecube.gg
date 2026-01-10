import {
  Dialog,
  DialogContent,
  DialogDescription,
  DialogHeader,
  DialogTitle,
} from '@/components/ui/dialog'
import { Button } from '@/components/ui/button'
import { Badge } from '@/components/ui/badge'
import { Check, X, Trophy, Flame, RotateCcw } from 'lucide-react'
import { PuzzleResult as PuzzleResultType } from '@/types/puzzle.types'

interface PuzzleResultProps {
  result: PuzzleResultType | null
  isOpen: boolean
  onClose: () => void
  onTryAgain: () => void
}

export function PuzzleResult({
  result,
  isOpen,
  onClose,
  onTryAgain,
}: PuzzleResultProps) {
  if (!result) return null

  const isCorrect = result.isCorrect
  const isPerfect = isCorrect && result.equityLoss === 0

  return (
    <Dialog open={isOpen} onOpenChange={onClose}>
      <DialogContent className="sm:max-w-md">
        <DialogHeader>
          <DialogTitle className="flex items-center gap-2">
            {isCorrect ? (
              <>
                <Check className="h-6 w-6 text-green-500" />
                <span className="text-green-500">
                  {isPerfect ? 'Perfect!' : 'Correct!'}
                </span>
              </>
            ) : (
              <>
                <X className="h-6 w-6 text-red-500" />
                <span className="text-red-500">Not Quite</span>
              </>
            )}
          </DialogTitle>
          <DialogDescription>{result.feedback}</DialogDescription>
        </DialogHeader>

        <div className="space-y-4 py-4">
          {/* Equity info for correct answers */}
          {isCorrect && result.equityLoss > 0 && (
            <div className="bg-muted rounded-lg p-3">
              <p className="text-sm text-muted-foreground">
                Your move was within{' '}
                <span className="font-mono font-bold">
                  {result.equityLoss.toFixed(3)}
                </span>{' '}
                equity of optimal
              </p>
            </div>
          )}

          {/* Best move (shown when correct) */}
          {isCorrect && result.bestMovesNotation && (
            <div className="bg-green-500/10 border border-green-500/30 rounded-lg p-3">
              <p className="text-sm font-medium text-green-400 mb-1">
                Best Move
              </p>
              <p className="font-mono">{result.bestMovesNotation}</p>
            </div>
          )}

          {/* Streak info */}
          {isCorrect && (
            <div className="flex items-center justify-center gap-6 pt-2">
              <div className="flex items-center gap-2">
                <Flame className="h-5 w-5 text-orange-500" />
                <div>
                  <p className="text-xs text-muted-foreground">Current Streak</p>
                  <p className="font-bold text-lg">{result.currentStreak}</p>
                </div>
              </div>

              {result.attemptCount === 1 && (
                <Badge className="bg-yellow-500/20 text-yellow-400 border-yellow-500/30">
                  <Trophy className="h-3 w-3 mr-1" />
                  First Try!
                </Badge>
              )}
            </div>
          )}

          {/* Attempt count for wrong answers */}
          {!isCorrect && (
            <div className="text-center text-sm text-muted-foreground">
              Attempt #{result.attemptCount}
            </div>
          )}
        </div>

        <div className="flex justify-end gap-2">
          {isCorrect ? (
            <Button onClick={onClose}>Done</Button>
          ) : (
            <>
              <Button variant="outline" onClick={onClose}>
                Close
              </Button>
              <Button onClick={onTryAgain}>
                <RotateCcw className="h-4 w-4 mr-2" />
                Try Again
              </Button>
            </>
          )}
        </div>
      </DialogContent>
    </Dialog>
  )
}
