import { Card, CardContent } from '@/components/ui/card'
import { Button } from '@/components/ui/button'
import { Slider } from '@/components/ui/slider'
import { Badge } from '@/components/ui/badge'
import { ChevronLeft, ChevronRight, Loader2, Dice1 } from 'lucide-react'
import { TurnSnapshotDto } from '@/types/generated/Backgammon.Server.Models'

interface TurnNavigatorProps {
  turnHistory: TurnSnapshotDto[]
  currentTurnIndex: number
  onTurnChange: (index: number) => void
  isLoading?: boolean
  showCurrentButton?: boolean
  onGoToCurrent?: () => void
}

export const TurnNavigator: React.FC<TurnNavigatorProps> = ({
  turnHistory,
  currentTurnIndex,
  onTurnChange,
  isLoading = false,
  showCurrentButton = false,
  onGoToCurrent,
}) => {
  if (!turnHistory || turnHistory.length === 0) {
    return null
  }

  const currentTurn = turnHistory[currentTurnIndex]
  const totalTurns = turnHistory.length

  const handlePrevious = () => {
    console.log('[TurnNavigator] Previous clicked, currentIndex:', currentTurnIndex)
    if (currentTurnIndex > 0) {
      onTurnChange(currentTurnIndex - 1)
    }
  }

  const handleNext = () => {
    console.log('[TurnNavigator] Next clicked, currentIndex:', currentTurnIndex)
    if (currentTurnIndex < totalTurns - 1) {
      onTurnChange(currentTurnIndex + 1)
    }
  }

  const handleSliderChange = (value: number[]) => {
    console.log('[TurnNavigator] Slider changed to:', value[0])
    if (value[0] !== currentTurnIndex) {
      onTurnChange(value[0])
    }
  }

  const formatDice = (dice: number[]) => {
    if (!dice || dice.length === 0) return null
    if (dice.length === 4) {
      // Doubles
      return `${dice[0]}-${dice[0]} (doubles)`
    }
    return `${dice[0]}-${dice[1]}`
  }

  const formatMoves = (moves: string[]) => {
    if (!moves || moves.length === 0) return 'No moves'
    return moves.join(', ')
  }

  const playerColor = currentTurn?.player === 'White' ? 'bg-white text-black border' : 'bg-red-600 text-white'

  return (
    <Card>
      <CardContent className="p-4 space-y-4">
        {/* Navigation Controls */}
        <div className="flex items-center gap-3">
          <Button
            variant="outline"
            size="icon"
            onClick={handlePrevious}
            disabled={currentTurnIndex === 0 || isLoading}
            aria-label="Previous turn"
          >
            <ChevronLeft className="h-4 w-4" />
          </Button>

          <div className="flex-1">
            <Slider
              value={[currentTurnIndex]}
              min={0}
              max={totalTurns - 1}
              step={1}
              onValueChange={handleSliderChange}
              disabled={isLoading}
              aria-label="Turn slider"
            />
          </div>

          <Button
            variant="outline"
            size="icon"
            onClick={handleNext}
            disabled={currentTurnIndex === totalTurns - 1 || isLoading}
            aria-label="Next turn"
          >
            <ChevronRight className="h-4 w-4" />
          </Button>

          {showCurrentButton && onGoToCurrent && (
            <Button
              variant="default"
              size="sm"
              onClick={onGoToCurrent}
              disabled={isLoading}
              className="ml-2"
            >
              Current
            </Button>
          )}
        </div>

        {/* Turn Info */}
        <div className="flex items-center justify-between">
          <div className="flex items-center gap-2">
            <Badge className={playerColor}>
              {currentTurn?.player}
            </Badge>
            <span className="text-sm text-muted-foreground">
              Turn {currentTurnIndex + 1} of {totalTurns}
            </span>
            {isLoading && <Loader2 className="h-4 w-4 animate-spin" />}
          </div>

          {currentTurn?.doublingAction && (
            <Badge variant="secondary">
              {currentTurn.doublingAction}
            </Badge>
          )}
        </div>

        {/* Dice and Moves */}
        {currentTurn && (
          <div className="space-y-2 text-sm">
            {currentTurn.diceRolled && currentTurn.diceRolled.length > 0 && (
              <div className="flex items-center gap-2">
                <Dice1 className="h-4 w-4 text-muted-foreground" />
                <span>Rolled: <strong>{formatDice(currentTurn.diceRolled)}</strong></span>
              </div>
            )}

            {currentTurn.moves && currentTurn.moves.length > 0 && (
              <div className="text-muted-foreground">
                Moves: <span className="text-foreground">{formatMoves(currentTurn.moves)}</span>
              </div>
            )}
          </div>
        )}
      </CardContent>
    </Card>
  )
}
