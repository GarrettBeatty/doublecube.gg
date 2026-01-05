import React from 'react'
import { Card, CardContent, CardHeader, CardTitle } from '@/components/ui/card'
import { Badge } from '@/components/ui/badge'
import { BarChart3, TrendingUp, TrendingDown } from 'lucide-react'
import { PositionEvaluation as PositionEvaluationType } from '@/types/analysis.types'

interface PositionEvaluationProps {
  evaluation: PositionEvaluationType | null
  isAnalyzing: boolean
}

export const PositionEvaluation: React.FC<PositionEvaluationProps> = ({
  evaluation,
  isAnalyzing,
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

  return (
    <Card>
      <CardHeader className="pb-2">
        <CardTitle className="text-sm font-medium flex items-center gap-2">
          <BarChart3 className="h-4 w-4" />
          Position Evaluation
        </CardTitle>
      </CardHeader>
      <CardContent className="space-y-3 pb-3">
        {/* Equity bar visualization */}
        <div className="space-y-2">
          <div className="h-8 bg-muted rounded flex overflow-hidden">
            <div
              className="bg-blue-500 transition-all duration-300"
              style={{ width: `${50 + equityPercent / 2}%` }}
            />
            <div className="bg-red-500 flex-1" />
          </div>

          <div className="text-center">
            <div
              className={`text-base font-bold font-mono flex items-center justify-center gap-1 ${
                isPositive ? 'text-green-500' : equity < 0 ? 'text-red-500' : 'text-muted-foreground'
              }`}
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
