import React from 'react'
import { Card, CardContent, CardHeader, CardTitle } from '@/components/ui/card'
import { Badge } from '@/components/ui/badge'
import { Trophy } from 'lucide-react'

interface MatchInfoProps {
  targetScore: number
  player1Score: number
  player2Score: number
  isCrawfordGame: boolean
  player1Name: string
  player2Name: string
}

export const MatchInfo: React.FC<MatchInfoProps> = ({
  targetScore,
  player1Score,
  player2Score,
  isCrawfordGame,
  player1Name,
  player2Name,
}) => {
  return (
    <Card>
      <CardHeader className="pb-3">
        <CardTitle className="text-sm font-medium flex items-center gap-2">
          <Trophy className="h-4 w-4" />
          Match to {targetScore}
        </CardTitle>
      </CardHeader>
      <CardContent className="space-y-2">
        <div className="flex justify-between items-center">
          <span className="text-sm text-muted-foreground">{player1Name}</span>
          <Badge variant="outline" className="font-mono">
            {player1Score}
          </Badge>
        </div>
        <div className="flex justify-between items-center">
          <span className="text-sm text-muted-foreground">{player2Name}</span>
          <Badge variant="outline" className="font-mono">
            {player2Score}
          </Badge>
        </div>
        {isCrawfordGame && (
          <Badge variant="secondary" className="w-full justify-center">
            Crawford Game
          </Badge>
        )}
      </CardContent>
    </Card>
  )
}
