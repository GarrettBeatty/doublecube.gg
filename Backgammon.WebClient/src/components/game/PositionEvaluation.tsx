import React from 'react'
import { Card, CardContent, CardHeader, CardTitle } from '@/components/ui/card'
import { Badge } from '@/components/ui/badge'
import { Skeleton } from '@/components/ui/skeleton'
import { Separator } from '@/components/ui/separator'
import {
  Accordion,
  AccordionContent,
  AccordionItem,
  AccordionTrigger,
} from '@/components/ui/accordion'
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
        <CardHeader className="pb-2">
          <CardTitle className="text-sm font-medium flex items-center gap-2">
            <BarChart3 className="h-4 w-4" />
            Position Evaluation
          </CardTitle>
        </CardHeader>
        <CardContent className="space-y-3">
          <Skeleton className="h-8 w-full" />
          <Skeleton className="h-6 w-24 mx-auto" />
          <Separator />
          <div className="space-y-2">
            <Skeleton className="h-4 w-full" />
            <Skeleton className="h-4 w-full" />
            <Skeleton className="h-4 w-full" />
          </div>
        </CardContent>
      </Card>
    )
  }

  if (!evaluation) {
    return (
      <Card>
        <CardHeader className="pb-2">
          <CardTitle className="text-sm font-medium flex items-center gap-2">
            <BarChart3 className="h-4 w-4" />
            Position Evaluation
          </CardTitle>
        </CardHeader>
        <CardContent>
          <div className="flex flex-col items-center justify-center py-6 text-center">
            <div className="rounded-full bg-muted p-3 mb-3">
              <BarChart3 className="h-6 w-6 text-muted-foreground" />
            </div>
            <p className="text-sm text-muted-foreground mb-1">No evaluation yet</p>
            <p className="text-xs text-muted-foreground/70">
              Position will be analyzed automatically
            </p>
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

        {/* Win probabilities with progress bars */}
        <div className="space-y-3 pt-2 border-t">
          <div className="space-y-1">
            <div className="flex justify-between items-center text-sm">
              <span className="text-muted-foreground">Win</span>
              <span className="font-mono font-medium">
                {(winProbability * 100).toFixed(1)}%
              </span>
            </div>
            <div className="h-2 bg-muted rounded-full overflow-hidden">
              <div
                className="h-full bg-green-500 transition-all duration-300"
                style={{ width: `${winProbability * 100}%` }}
              />
            </div>
          </div>
          <div className="space-y-1">
            <div className="flex justify-between items-center text-sm">
              <span className="text-muted-foreground">Gammon</span>
              <span className="font-mono font-medium">
                {(gammonProbability * 100).toFixed(1)}%
              </span>
            </div>
            <div className="h-2 bg-muted rounded-full overflow-hidden">
              <div
                className="h-full bg-yellow-500 transition-all duration-300"
                style={{ width: `${gammonProbability * 100}%` }}
              />
            </div>
          </div>
        </div>

        {/* Position features accordion */}
        {evaluation.features && (
          <Accordion type="single" collapsible className="border-t pt-2">
            <AccordionItem value="features" className="border-none">
              <AccordionTrigger className="py-2 text-sm hover:no-underline">
                Position Details
              </AccordionTrigger>
              <AccordionContent>
                <div className="grid grid-cols-2 gap-x-4 gap-y-2 text-xs">
                  <div className="flex justify-between">
                    <span className="text-muted-foreground">Blots</span>
                    <span className="font-mono">{evaluation.features.blotCount}</span>
                  </div>
                  <div className="flex justify-between">
                    <span className="text-muted-foreground">Prime</span>
                    <span className="font-mono">{evaluation.features.primeLength}</span>
                  </div>
                  <div className="flex justify-between">
                    <span className="text-muted-foreground">On Bar</span>
                    <span className="font-mono">{evaluation.features.checkersOnBar}</span>
                  </div>
                  <div className="flex justify-between">
                    <span className="text-muted-foreground">Home</span>
                    <span className="font-mono">{evaluation.features.homeboardCoverage}</span>
                  </div>
                  <div className="flex justify-between">
                    <span className="text-muted-foreground">Pip Diff</span>
                    <span className="font-mono">
                      {evaluation.features.pipDifference > 0 ? '+' : ''}
                      {evaluation.features.pipDifference}
                    </span>
                  </div>
                  <div className="flex justify-between">
                    <span className="text-muted-foreground">Anchors</span>
                    <span className="font-mono">{evaluation.features.anchorsInOpponentHome}</span>
                  </div>
                  {evaluation.features.isRace && (
                    <div className="col-span-2">
                      <Badge variant="secondary" className="text-xs">
                        Race Position
                      </Badge>
                    </div>
                  )}
                  {evaluation.features.isContact && !evaluation.features.isRace && (
                    <div className="col-span-2">
                      <Badge variant="secondary" className="text-xs">
                        Contact Position
                      </Badge>
                    </div>
                  )}
                </div>
              </AccordionContent>
            </AccordionItem>
          </Accordion>
        )}
      </CardContent>
    </Card>
  )
}
