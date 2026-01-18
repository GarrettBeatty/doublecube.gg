import React from 'react'
import { Card, CardContent } from '@/components/ui/card'
import { Badge } from '@/components/ui/badge'
import { CheckerColor } from '@/types/generated/Backgammon.Core'
import { cn } from '@/lib/utils'
import { AlertCircle } from 'lucide-react'

interface PlayerCardProps {
  playerName: string
  username?: string
  color: CheckerColor
  isYourTurn: boolean
  isYou: boolean
  pipCount?: number
  checkersOnBar?: number
  bornOff?: number
  rating?: number
  ratingChange?: number
}

export const PlayerCard: React.FC<PlayerCardProps> = ({
  playerName,
  username,
  color,
  isYourTurn,
  isYou,
  pipCount,
  checkersOnBar,
  bornOff,
  rating,
  ratingChange,
}) => {
  const colorClass = color === CheckerColor.White ? 'bg-gray-100 text-gray-900' : 'bg-red-600 text-white'

  return (
    <Card
      className={cn(
        'transition-all',
        isYourTurn && 'ring-2 ring-blue-500 shadow-lg animate-subtle-pulse'
      )}
    >
      <CardContent className="p-4">
        <div className="flex items-center justify-between mb-2">
          <div className="flex items-center gap-2">
            <div className={cn('w-6 h-6 rounded-full', colorClass)} />
            <div>
              <div className="font-semibold">{playerName}</div>
              {username && (
                <div className="text-xs text-muted-foreground">@{username}</div>
              )}
              {rating !== undefined && (
                <div className="text-xs font-medium text-muted-foreground">
                  Rating: {rating}
                  {ratingChange !== undefined && ratingChange !== 0 && (
                    <span className={cn('ml-1', ratingChange > 0 ? 'text-green-600' : 'text-red-600')}>
                      ({ratingChange > 0 ? '+' : ''}{ratingChange})
                    </span>
                  )}
                </div>
              )}
            </div>
          </div>
          {isYou && <Badge variant="secondary">You</Badge>}
        </div>

        <div className="grid grid-cols-3 gap-2 text-sm mt-3">
          {/* Pip Count */}
          <div
            className="text-center cursor-help"
            title="Total moves needed to bear off all checkers. Lower is better."
          >
            <div className="text-muted-foreground text-xs">Pip Count</div>
            <div className="font-semibold">{pipCount || 0}</div>
          </div>

          {/* On Bar - highlighted when > 0 */}
          <div
            className={cn(
              "text-center cursor-help rounded-md py-1 -my-1 transition-colors",
              checkersOnBar && checkersOnBar > 0 && "bg-orange-100 dark:bg-orange-950/50"
            )}
            title={checkersOnBar && checkersOnBar > 0
              ? "Checkers on the bar must re-enter before any other moves!"
              : "No checkers captured. You can move freely."}
          >
            <div className={cn(
              "text-xs flex items-center justify-center gap-1",
              checkersOnBar && checkersOnBar > 0 ? "text-orange-700 dark:text-orange-400 font-medium" : "text-muted-foreground"
            )}>
              {checkersOnBar && checkersOnBar > 0 && <AlertCircle className="h-3 w-3" />}
              On Bar
            </div>
            <div className={cn(
              "font-semibold",
              checkersOnBar && checkersOnBar > 0 && "text-orange-700 dark:text-orange-400 text-lg"
            )}>
              {checkersOnBar || 0}
            </div>
          </div>

          {/* Born Off - highlighted when > 0 */}
          <div
            className={cn(
              "text-center cursor-help rounded-md py-1 -my-1 transition-colors",
              bornOff && bornOff > 0 && "bg-green-50 dark:bg-green-950/30"
            )}
            title="Bear off all 15 checkers to win!"
          >
            <div className={cn(
              "text-xs",
              bornOff && bornOff > 0 ? "text-green-700 dark:text-green-400" : "text-muted-foreground"
            )}>
              Born Off
            </div>
            <div className={cn(
              "font-semibold",
              bornOff && bornOff > 0 && "text-green-700 dark:text-green-400"
            )}>
              {bornOff || 0}/15
            </div>
          </div>
        </div>
      </CardContent>
    </Card>
  )
}
