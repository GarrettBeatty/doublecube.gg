import React from 'react'
import { Card, CardContent, CardHeader, CardTitle } from '@/components/ui/card'
import { Badge } from '@/components/ui/badge'
import { BarChart3, TrendingUp, TrendingDown } from 'lucide-react'
import { PositionEvaluation as PositionEvaluationType } from '@/types/analysis.types'
import { CheckerColor } from '@/types/game.types'

interface PositionEvaluationProps {
  evaluation: PositionEvaluationType | null
  isAnalyzing: boolean
  currentPlayer: CheckerColor
}

export const PositionEvaluation: React.FC<PositionEvaluationProps> = ({
  evaluation,
  isAnalyzing,
  currentPlayer,
}) => {
  if (isAnalyzing) {
    return (
      <Card>
        <CardHeader className="pb-3">
          <CardTitle className="text-sm font-medium flex items-center gap-2">
            <BarChart3 className="h-4 w-4" />
            Position Evaluation
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

  if (!evaluation) {
    return (
      <Card>
        <CardHeader className="pb-3">
          <CardTitle className="text-sm font-medium flex items-center gap-2">
            <BarChart3 className="h-4 w-4" />
            Position Evaluation
          </CardTitle>
        </CardHeader>
        <CardContent>
          <div className="flex items-center justify-center py-4 text-sm text-muted-foreground">
            Click "Analyze Position" to evaluate
          </div>
        </CardContent>
      </Card>
    )
  }

  const { equity, winProbability, gammonProbability } = evaluation
  const equityPercent = (equity / 3.0) * 100 // Map -3 to +3 -> -100% to +100%
  const isPositive = equity > 0

  // Determine which player has the advantage based on equity sign and current player
  const whiteHasAdvantage =
    (currentPlayer === CheckerColor.White && isPositive) ||
    (currentPlayer === CheckerColor.Red && !isPositive && equity !== 0)
  const redHasAdvantage =
    (currentPlayer === CheckerColor.Red && isPositive) ||
    (currentPlayer === CheckerColor.White && !isPositive && equity !== 0)

  // Color for equity display - slate for White's advantage, red for Red's advantage
  const equityColorClass = whiteHasAdvantage
    ? 'text-slate-700'
    : redHasAdvantage
      ? 'text-red-500'
      : 'text-muted-foreground'

  return (
    <Card>
      <CardHeader className="pb-2">
        <div className="flex items-center justify-between">
          <CardTitle className="text-sm font-medium flex items-center gap-2">
            <BarChart3 className="h-4 w-4" />
            Position Evaluation
          </CardTitle>
          {evaluation.evaluatorName && (
            <Badge variant="secondary" className="text-xs">
              {evaluation.evaluatorName}
            </Badge>
          )}
        </div>
      </CardHeader>
      <CardContent className="space-y-3 pb-3">
        {/* Equity bar visualization */}
        <div className="space-y-2">
          <div className="h-8 bg-muted rounded flex overflow-hidden border border-border">
            <div
              className="bg-white transition-all duration-300"
              style={{ width: `${50 + equityPercent / 2}%` }}
            />
            <div className="bg-red-500 flex-1" />
          </div>

          <div className="text-center">
            <div
              className={`text-base font-bold font-mono flex items-center justify-center gap-1 ${equityColorClass}`}
            >
              {isPositive && <TrendingUp className="h-3 w-3" />}
              {equity < 0 && <TrendingDown className="h-3 w-3" />}
              {equity > 0 ? '+' : ''}
              {equity.toFixed(2)}
            </div>
            <div className="text-xs text-muted-foreground">Equity</div>
          </div>
        </div>

        {/* Win probabilities */}
        <div className="space-y-2 pt-2 border-t">
          <div className="flex justify-between items-center">
            <span className="text-sm text-muted-foreground">Win:</span>
            <Badge variant="outline" className="font-mono">
              {(winProbability * 100).toFixed(1)}%
            </Badge>
          </div>
          <div className="flex justify-between items-center">
            <span className="text-sm text-muted-foreground">Gammon:</span>
            <Badge variant="outline" className="font-mono">
              {(gammonProbability * 100).toFixed(1)}%
            </Badge>
          </div>
        </div>
      </CardContent>
    </Card>
  )
}
