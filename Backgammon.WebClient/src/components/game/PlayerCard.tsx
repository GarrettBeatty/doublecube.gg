import React from 'react'
import { Card, CardContent } from '@/components/ui/card'
import { Badge } from '@/components/ui/badge'
import { CheckerColor } from '@/types/game.types'
import { cn } from '@/lib/utils'

interface PlayerCardProps {
  playerName: string
  username: string | null
  color: CheckerColor
  isYourTurn: boolean
  isYou: boolean
  pipCount?: number
  checkersOnBar?: number
  bornOff?: number
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
            </div>
          </div>
          {isYou && <Badge variant="secondary">You</Badge>}
        </div>

        <div className="grid grid-cols-3 gap-2 text-sm mt-3">
          <div className="text-center">
            <div className="text-muted-foreground text-xs">Pip Count</div>
            <div className="font-semibold">{pipCount || 0}</div>
          </div>
          <div className="text-center">
            <div className="text-muted-foreground text-xs">On Bar</div>
            <div className="font-semibold">{checkersOnBar || 0}</div>
          </div>
          <div className="text-center">
            <div className="text-muted-foreground text-xs">Born Off</div>
            <div className="font-semibold">{bornOff || 0}</div>
          </div>
        </div>
      </CardContent>
    </Card>
  )
}
