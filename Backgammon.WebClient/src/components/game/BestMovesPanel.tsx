import React from 'react'
import { Card, CardContent, CardHeader, CardTitle } from '@/components/ui/card'
import { Badge } from '@/components/ui/badge'
import { Skeleton } from '@/components/ui/skeleton'
import { ScrollArea } from '@/components/ui/scroll-area'
import {
  Table,
  TableBody,
  TableCell,
  TableHead,
  TableHeader,
  TableRow,
} from '@/components/ui/table'
import { Lightbulb, Target, Dices } from 'lucide-react'
import { cn } from '@/lib/utils'
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
        <CardHeader className="pb-2">
          <CardTitle className="text-sm font-medium flex items-center gap-2">
            <Lightbulb className="h-4 w-4" />
            Best Moves
          </CardTitle>
        </CardHeader>
        <CardContent>
          <div className="space-y-2">
            {[1, 2, 3].map((i) => (
              <div key={i} className="flex items-center gap-3 p-2">
                <Skeleton className="h-5 w-5 rounded-full" />
                <Skeleton className="h-4 flex-1" />
                <Skeleton className="h-4 w-16" />
              </div>
            ))}
          </div>
        </CardContent>
      </Card>
    )
  }

  if (!analysis) {
    return (
      <Card>
        <CardHeader className="pb-2">
          <CardTitle className="text-sm font-medium flex items-center gap-2">
            <Lightbulb className="h-4 w-4" />
            Best Moves
          </CardTitle>
        </CardHeader>
        <CardContent>
          <div className="flex flex-col items-center justify-center py-6 text-center">
            <div className="rounded-full bg-muted p-3 mb-3">
              <Dices className="h-6 w-6 text-muted-foreground" />
            </div>
            <p className="text-sm text-muted-foreground mb-1">No dice rolled</p>
            <p className="text-xs text-muted-foreground/70">
              Set dice values to see move recommendations
            </p>
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

  const getEquityColorClass = (moveSeq: MoveSequence) => {
    const isPositive = moveSeq.equity > 0
    const whiteHasAdvantage =
      (currentPlayer === CheckerColor.White && isPositive) ||
      (currentPlayer === CheckerColor.Red && !isPositive && moveSeq.equity !== 0)
    const redHasAdvantage =
      (currentPlayer === CheckerColor.Red && isPositive) ||
      (currentPlayer === CheckerColor.White && !isPositive && moveSeq.equity !== 0)

    return whiteHasAdvantage
      ? 'text-slate-700'
      : redHasAdvantage
        ? 'text-red-500'
        : 'text-muted-foreground'
  }

  const getIconColorClass = () => {
    // For best move, use green to indicate it's the optimal choice
    return 'text-green-600'
  }

  return (
    <Card>
      <CardHeader className="pb-2">
        <div className="flex items-center justify-between">
          <CardTitle className="text-sm font-medium flex items-center gap-2">
            <Lightbulb className="h-4 w-4" />
            Best Moves
          </CardTitle>
          <span className="text-xs text-muted-foreground">
            {analysis.totalSequencesExplored} explored
          </span>
        </div>
      </CardHeader>
      <CardContent className="p-0">
        {analysis.topMoves.length === 0 ? (
          <div className="text-center py-6 text-sm text-muted-foreground px-4">
            No valid moves available
          </div>
        ) : (
          <ScrollArea className="h-[280px]">
            <Table>
              <TableHeader>
                <TableRow>
                  <TableHead className="w-10">#</TableHead>
                  <TableHead>Move</TableHead>
                  <TableHead className="w-20 text-right">Equity</TableHead>
                  <TableHead className="w-20 text-right">Delta</TableHead>
                </TableRow>
              </TableHeader>
              <TableBody>
                {analysis.topMoves.map((moveSeq, index) => {
                  const equityColorClass = getEquityColorClass(moveSeq)
                  const isActive = isHighlighted(moveSeq)

                  return (
                    <TableRow
                      key={index}
                      className={cn(
                        'cursor-pointer transition-colors',
                        isActive && 'bg-primary/10',
                        'focus-visible:ring-2 focus-visible:ring-ring focus-visible:ring-inset'
                      )}
                      tabIndex={0}
                      role="button"
                      aria-label={`Execute move ${index + 1}: ${moveSeq.notation}`}
                      onMouseEnter={() => onHighlightMoves(moveSeq.moves)}
                      onMouseLeave={() => onHighlightMoves([])}
                      onClick={() => onExecuteMoves(moveSeq.moves)}
                      onKeyDown={(e) => {
                        if (e.key === 'Enter' || e.key === ' ') {
                          e.preventDefault()
                          onExecuteMoves(moveSeq.moves)
                        }
                      }}
                    >
                      <TableCell className="text-center py-2">
                        {index === 0 ? (
                          <Target className={cn('h-4 w-4 mx-auto', getIconColorClass())} />
                        ) : (
                          <span className="text-muted-foreground">{index + 1}</span>
                        )}
                      </TableCell>
                      <TableCell className="font-mono text-sm py-2">
                        {moveSeq.notation}
                      </TableCell>
                      <TableCell className={cn('text-right font-mono py-2', equityColorClass)}>
                        {moveSeq.equity > 0 ? '+' : ''}
                        {moveSeq.equity.toFixed(3)}
                      </TableCell>
                      <TableCell className="text-right py-2">
                        {index === 0 ? (
                          <Badge variant="default" className="text-xs">
                            Best
                          </Badge>
                        ) : (
                          <span
                            className={cn(
                              'text-xs font-mono',
                              moveSeq.equityGain < -0.1
                                ? 'text-destructive'
                                : 'text-muted-foreground'
                            )}
                          >
                            {moveSeq.equityGain.toFixed(3)}
                          </span>
                        )}
                      </TableCell>
                    </TableRow>
                  )
                })}
              </TableBody>
            </Table>
          </ScrollArea>
        )}
      </CardContent>
    </Card>
  )
}
