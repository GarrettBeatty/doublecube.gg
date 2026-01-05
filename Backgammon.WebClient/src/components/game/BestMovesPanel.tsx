import React from 'react'
import { Card, CardContent, CardHeader, CardTitle } from '@/components/ui/card'
import { Badge } from '@/components/ui/badge'
import { Lightbulb, Target } from 'lucide-react'
import { BestMovesAnalysis, MoveSequence } from '@/types/analysis.types'
import { Move } from '@/types/game.types'

interface BestMovesPanelProps {
  analysis: BestMovesAnalysis | null
  isAnalyzing: boolean
  onHighlightMoves: (moves: Move[]) => void
  highlightedMoves: Move[]
  onExecuteMoves: (moves: Move[]) => void
}

export const BestMovesPanel: React.FC<BestMovesPanelProps> = ({
  analysis,
  isAnalyzing,
  onHighlightMoves,
  highlightedMoves,
  onExecuteMoves,
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

  const formatMove = (move: Move): string => {
    if (move.from === 0) return `bar/${move.to}`
    if (move.to === 25) return `${move.from}/off`
    return `${move.from}/${move.to}`
  }

  const formatMoveSequence = (sequence: MoveSequence): string => {
    return sequence.moves.map(formatMove).join(' ')
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
                  {index === 0 && <Target className="h-3 w-3 text-green-500" />}
                  <span className="text-xs text-muted-foreground">#{index + 1}</span>
                </div>
                <span
                  className={`text-sm font-mono font-medium ${
                    isPositive
                      ? 'text-green-500'
                      : moveSeq.equity < 0
                        ? 'text-red-500'
                        : 'text-muted-foreground'
                  }`}
                >
                  {moveSeq.equity > 0 ? '+' : ''}
                  {moveSeq.equity.toFixed(3)}
                </span>
              </div>

              <div className="text-sm font-mono mb-1">{formatMoveSequence(moveSeq)}</div>

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
