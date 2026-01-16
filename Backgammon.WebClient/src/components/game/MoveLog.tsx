import React, { useEffect, useRef } from 'react'
import { Accordion, AccordionContent, AccordionItem, AccordionTrigger } from '@/components/ui/accordion'
import { ScrollArea } from '@/components/ui/scroll-area'
import { Badge } from '@/components/ui/badge'
import type { TurnSnapshotDto } from '@/types/generated/Backgammon.Server.Models'
import { CheckerColor } from '@/types/game.types'
import { List } from 'lucide-react'

interface MoveLogProps {
  turnHistory: TurnSnapshotDto[]
  currentTurnMoves: string[]
  currentPlayer: CheckerColor
  dice: number[]
}

export const MoveLog: React.FC<MoveLogProps> = ({
  turnHistory,
  currentTurnMoves,
  currentPlayer,
  dice,
}) => {
  const scrollRef = useRef<HTMLDivElement>(null)

  // Auto-scroll to bottom when new moves are added
  useEffect(() => {
    if (scrollRef.current) {
      scrollRef.current.scrollTop = scrollRef.current.scrollHeight
    }
  }, [turnHistory, currentTurnMoves])

  const formatDice = (diceRolled: number[]): string => {
    if (diceRolled.length === 0) return ''
    // For doubles (4 dice), show as "6-6"
    if (diceRolled.length === 4) {
      return `${diceRolled[0]}-${diceRolled[0]}`
    }
    // For normal rolls, show as "5-3"
    return `${diceRolled[0]}-${diceRolled[1]}`
  }

  const formatMoves = (moves: string[]): string => {
    if (moves.length === 0) return '(no moves)'
    return moves.join(', ')
  }

  const getPlayerBadge = (player: string) => {
    const isWhite = player === 'White'
    return (
      <Badge
        variant="outline"
        className={`w-6 h-5 flex items-center justify-center text-xs font-bold ${
          isWhite
            ? 'bg-amber-50 text-amber-900 border-amber-300'
            : 'bg-red-50 text-red-900 border-red-300'
        }`}
      >
        {isWhite ? 'W' : 'R'}
      </Badge>
    )
  }

  const hasAnyMoves = turnHistory.length > 0 || currentTurnMoves.length > 0 || (dice[0] > 0 && dice[1] > 0)

  if (!hasAnyMoves) {
    return null
  }

  return (
    <Accordion type="single" collapsible defaultValue="move-log" className="mt-4">
      <AccordionItem value="move-log" className="border rounded-lg">
        <AccordionTrigger className="px-4 py-2 hover:no-underline">
          <div className="flex items-center gap-2 text-sm font-medium">
            <List className="h-4 w-4" />
            Move Log
            <span className="text-muted-foreground text-xs">
              ({turnHistory.length} turn{turnHistory.length !== 1 ? 's' : ''})
            </span>
          </div>
        </AccordionTrigger>
        <AccordionContent className="px-4 pb-3">
          <ScrollArea className="h-40">
            <div ref={scrollRef} className="space-y-1 text-xs font-mono pr-4">
              {turnHistory.map((turn) => (
                <div
                  key={`turn-${turn.turnNumber}`}
                  className="flex items-center gap-2 py-0.5"
                >
                  <span className="text-muted-foreground w-6 text-right">
                    #{turn.turnNumber}
                  </span>
                  {getPlayerBadge(turn.player)}
                  <span className="text-muted-foreground w-8">
                    {formatDice(turn.diceRolled)}:
                  </span>
                  <span className="flex-1">{formatMoves(turn.moves)}</span>
                </div>
              ))}

              {/* Current turn in progress */}
              {(currentTurnMoves.length > 0 || (dice[0] > 0 && dice[1] > 0)) && (
                <div className="flex items-center gap-2 py-0.5 bg-muted/50 rounded px-1 -mx-1">
                  <span className="text-muted-foreground w-6 text-right">
                    #{turnHistory.length + 1}
                  </span>
                  {getPlayerBadge(currentPlayer === CheckerColor.White ? 'White' : 'Red')}
                  <span className="text-muted-foreground w-8">
                    {dice[0] > 0 && dice[1] > 0 ? `${dice[0]}-${dice[1]}:` : ''}
                  </span>
                  <span className="flex-1">
                    {currentTurnMoves.length > 0 ? formatMoves(currentTurnMoves) : '...'}
                  </span>
                  <span className="text-muted-foreground italic text-[10px]">
                    (in progress)
                  </span>
                </div>
              )}

              {/* Empty state */}
              {turnHistory.length === 0 && currentTurnMoves.length === 0 && !(dice[0] > 0 && dice[1] > 0) && (
                <div className="text-muted-foreground text-center py-2">
                  No moves yet
                </div>
              )}
            </div>
          </ScrollArea>
        </AccordionContent>
      </AccordionItem>
    </Accordion>
  )
}
