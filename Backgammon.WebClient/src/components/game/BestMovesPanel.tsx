import React from 'react'
import { Card, CardContent, CardHeader, CardTitle } from '@/components/ui/card'
import { Badge } from '@/components/ui/badge'
import { Lightbulb, Target } from 'lucide-react'
import { BestMovesAnalysis, MoveSequence } from '@/types/analysis.types'
import { Move, CheckerColor } from '@/types/game.types'

interface BestMovesPanelProps {
  analysis: BestMovesAnalysis | null
  isAnalyzing: boolean
  onHighlightMoves: (moves: Move[]) => void
  highlightedMoves: Move[]
  onExecuteMoves: (moves: Move[]) => void
  currentPlayer: CheckerColor
}

export const BestMovesPanel: React.FC<BestMovesPanelProps> = ({
  analysis,
  isAnalyzing,
  onHighlightMoves,
  highlightedMoves,
  onExecuteMoves,
  currentPlayer,
}) => {
  if (isAnalyzing) {
    return (
      <Card>
        <CardHeader className="pb-3">
          <CardTitle className="text-sm font-medium flex items-center gap-2">
            <Lightbulb className="h-4 w-4" />
            Best Moves
          </CardTitle>
        </CardHeader>
        <CardContent>
          <div className="flex items-center justify-center py-4 text-sm text-muted-foreground">
            Analyzing...
          </div>
        </CardContent>
      </Card>
    )
  }

  if (!analysis) {
    return (
      <Card>
        <CardHeader className="pb-3">
          <CardTitle className="text-sm font-medium flex items-center gap-2">
            <Lightbulb className="h-4 w-4" />
            Best Moves
          </CardTitle>
        </CardHeader>
        <CardContent>
          <div className="flex items-center justify-center py-4 text-sm text-muted-foreground">
            Roll dice and click "Find Best Moves"
          </div>
        </CardContent>
      </Card>
    )
  }

  const isHighlighted = (sequence: MoveSequence): boolean => {
    if (highlightedMoves.length !== sequence.moves.length) return false
    return sequence.moves.every((move, i) => {
      const hm = highlightedMoves[i]
      return hm && move.from === hm.from && move.to === hm.to
    })
  }

  return (
    <Card>
      <CardHeader className="pb-2">
        <CardTitle className="text-sm font-medium flex items-center gap-2">
          <Lightbulb className="h-4 w-4" />
          Best Moves
        </CardTitle>
        <div className="text-xs text-muted-foreground">
          {analysis.totalSequencesExplored} sequences explored
        </div>
      </CardHeader>
      <CardContent className="space-y-2 pb-3">
        {analysis.topMoves.map((moveSeq, index) => {
          const isActive = isHighlighted(moveSeq)
          const isPositive = moveSeq.equity > 0

          // Determine which player has the advantage based on equity sign and current player
          const whiteHasAdvantage =
            (currentPlayer === CheckerColor.White && isPositive) ||
            (currentPlayer === CheckerColor.Red && !isPositive && moveSeq.equity !== 0)
          const redHasAdvantage =
            (currentPlayer === CheckerColor.Red && isPositive) ||
            (currentPlayer === CheckerColor.White && !isPositive && moveSeq.equity !== 0)

          // Color for equity display - slate for White's advantage, red for Red's advantage
          const equityColorClass = whiteHasAdvantage
            ? 'text-slate-700'
            : redHasAdvantage
              ? 'text-red-500'
              : 'text-muted-foreground'

          // Icon color for best move indicator
          const iconColorClass = whiteHasAdvantage ? 'text-slate-700' : 'text-red-500'

          return (
            <button
              key={index}
              onMouseEnter={() => onHighlightMoves(moveSeq.moves)}
              onMouseLeave={() => onHighlightMoves([])}
              onClick={() => onExecuteMoves(moveSeq.moves)}
              className={`w-full text-left p-2 rounded-lg transition-all border ${
                isActive
                  ? 'bg-primary/10 border-primary'
                  : 'hover:bg-muted border-transparent'
              }`}
            >
              <div className="flex items-center justify-between mb-1">
                <div className="flex items-center gap-2">
                  {index === 0 && <Target className={`h-3 w-3 ${iconColorClass}`} />}
                  <span className="text-xs text-muted-foreground">#{index + 1}</span>
                </div>
                <span className={`text-sm font-mono font-medium ${equityColorClass}`}>
                  {moveSeq.equity > 0 ? '+' : ''}
                  {moveSeq.equity.toFixed(3)}
                </span>
              </div>

              <div className="text-sm font-mono mb-1">{moveSeq.notation}</div>

              {index === 0 ? (
                <Badge variant="default" className="text-xs">
                  Best Move
                </Badge>
              ) : moveSeq.equityGain < -0.1 ? (
                <Badge variant="destructive" className="text-xs">
                  {moveSeq.equityGain.toFixed(3)} equity loss
                </Badge>
              ) : (
                <Badge variant="secondary" className="text-xs">
                  {moveSeq.equityGain.toFixed(3)} vs best
                </Badge>
              )}
            </button>
          )
        })}

        {analysis.topMoves.length === 0 && (
          <div className="text-center py-4 text-sm text-muted-foreground">
            No valid moves available
          </div>
        )}
      </CardContent>
    </Card>
  )
}
