import { useNavigate } from 'react-router-dom'
import { Card, CardContent } from '@/components/ui/card'
import { Button } from '@/components/ui/button'
import { Badge } from '@/components/ui/badge'
import { User, RefreshCw, Clock, Dice6 } from 'lucide-react'
import { useActiveGames } from '@/hooks/useActiveGames'
import { useCorrespondenceGames } from '@/hooks/useCorrespondenceGames'

interface GameItemProps {
  gameId: string
  opponentName: string
  opponentRating?: number
  isYourTurn: boolean
  isCorrespondence: boolean
  matchScore?: string
  matchLength?: number
  targetScore?: number
  cubeValue?: number
  isCrawford?: boolean
  timeRemaining?: string | null
  onPlay: (gameId: string) => void
}

function GameItem({
  gameId,
  opponentName,
  opponentRating,
  isYourTurn,
  isCorrespondence,
  matchScore,
  matchLength,
  targetScore,
  cubeValue,
  isCrawford,
  timeRemaining,
  onPlay,
}: GameItemProps) {
  return (
    <div
      className={`flex items-center justify-between p-4 border rounded-lg hover:bg-accent transition-colors ${
        isYourTurn ? 'bg-green-500/5 border-green-500/20' : 'opacity-70'
      }`}
    >
      <div className="flex items-center gap-3">
        <User className="h-5 w-5 text-muted-foreground" />
        <div>
          <div className="flex items-center gap-2">
            <span className={isYourTurn ? 'font-semibold' : ''}>{opponentName}</span>
            {opponentRating && (
              <span className="text-sm text-muted-foreground">({opponentRating})</span>
            )}
          </div>
          <div className="flex flex-wrap items-center gap-2 mt-1">
            <Badge variant={isCorrespondence ? 'secondary' : 'outline'} className="text-xs">
              {isCorrespondence ? 'Correspondence' : 'Live'}
            </Badge>
            {(matchScore || targetScore) && (
              <Badge variant="outline" className="text-xs">
                {matchScore || '0-0'} / {matchLength || targetScore}pt
              </Badge>
            )}
            {isCorrespondence && timeRemaining && (
              <Badge variant="secondary" className="text-xs flex items-center gap-1">
                <Clock className="h-3 w-3" />
                {formatTimeRemaining(timeRemaining)}
              </Badge>
            )}
            {cubeValue && cubeValue > 1 && (
              <Badge variant="secondary" className="text-xs flex items-center gap-1">
                <Dice6 className="h-3 w-3" />
                {cubeValue}x
              </Badge>
            )}
            {isCrawford && (
              <Badge variant="outline" className="text-xs">
                Crawford
              </Badge>
            )}
            {isYourTurn && (
              <Badge variant="default" className="text-xs bg-green-600">
                Your turn
              </Badge>
            )}
          </div>
        </div>
      </div>
      <Button
        size="sm"
        variant={isYourTurn ? 'default' : 'outline'}
        className={isYourTurn ? 'bg-green-600 hover:bg-green-700' : ''}
        onClick={() => onPlay(gameId)}
      >
        {isYourTurn ? 'Play' : 'View'}
      </Button>
    </div>
  )
}

function formatTimeRemaining(timeRemaining: string | null): string {
  if (!timeRemaining) return 'No deadline'
  const match = timeRemaining.match(/^(\d+)\.?(\d{2}):(\d{2}):(\d{2})/)
  if (!match) return timeRemaining
  const days = parseInt(match[1]) || 0
  const hours = parseInt(match[2]) || 0
  if (days > 0) return `${days}d ${hours}h`
  if (hours > 0) return `${hours}h`
  return '< 1h'
}

export function GamesInPlay() {
  const navigate = useNavigate()
  const { games: liveGames, isLoading: liveLoading, refresh: refreshLive, yourTurnCount: liveYourTurn } = useActiveGames()
  const { yourTurnGames, waitingGames, isLoading: corrLoading, refresh: refreshCorr, totalYourTurn: corrYourTurn } = useCorrespondenceGames()

  const handlePlayGame = (gameId: string) => {
    navigate(`/game/${gameId}`)
  }

  const handleRefresh = () => {
    refreshLive()
    refreshCorr()
  }

  const isLoading = liveLoading || corrLoading
  const totalYourTurn = liveYourTurn + corrYourTurn

  // Combine all games
  const allCorrespondenceGames = [...yourTurnGames, ...waitingGames]

  // Build unified list
  type UnifiedGame = {
    gameId: string
    opponentName: string
    opponentRating?: number
    isYourTurn: boolean
    isCorrespondence: boolean
    matchScore?: string
    matchLength?: number
    targetScore?: number
    cubeValue?: number
    isCrawford?: boolean
    timeRemaining?: string | null
  }

  const unifiedGames: UnifiedGame[] = [
    // Live games
    ...liveGames.map(g => ({
      gameId: g.gameId,
      opponentName: g.myColor === 'White' ? g.player2Name : g.player1Name,
      opponentRating: g.myColor === 'White' ? g.player2Rating : g.player1Rating,
      isYourTurn: g.isYourTurn,
      isCorrespondence: false,
      matchScore: g.matchScore,
      matchLength: g.matchLength,
      cubeValue: g.cubeValue,
      isCrawford: g.isCrawford,
    })),
    // Correspondence games
    ...allCorrespondenceGames.map(g => ({
      gameId: g.gameId,
      opponentName: g.opponentName,
      opponentRating: g.opponentRating,
      isYourTurn: g.isYourTurn,
      isCorrespondence: true,
      matchScore: g.matchScore,
      targetScore: g.targetScore,
      timeRemaining: g.timeRemaining,
    })),
  ]

  // Sort: your turn first, then by type (live first)
  const sortedGames = unifiedGames.sort((a, b) => {
    if (a.isYourTurn && !b.isYourTurn) return -1
    if (!a.isYourTurn && b.isYourTurn) return 1
    if (!a.isCorrespondence && b.isCorrespondence) return -1
    if (a.isCorrespondence && !b.isCorrespondence) return 1
    return 0
  })

  if (isLoading) {
    return (
      <div className="flex items-center justify-center py-12">
        <div className="text-muted-foreground">Loading games...</div>
      </div>
    )
  }

  return (
    <div className="space-y-4">
      <div className="flex items-center gap-2">
        <h2 className="text-xl font-semibold">Games in Play</h2>
        {totalYourTurn > 0 && (
          <Badge variant="destructive" className="animate-pulse">
            {totalYourTurn} your turn
          </Badge>
        )}
        <Button variant="ghost" size="icon" onClick={handleRefresh} title="Refresh">
          <RefreshCw className="h-4 w-4" />
        </Button>
      </div>

      {sortedGames.length === 0 ? (
        <Card>
          <CardContent className="flex flex-col items-center justify-center py-12 gap-2">
            <p className="text-muted-foreground">No active games</p>
            <p className="text-sm text-muted-foreground">
              Join a game from the Lobby or Correspondence tabs
            </p>
          </CardContent>
        </Card>
      ) : (
        <div className="space-y-2">
          {sortedGames.map((game) => (
            <GameItem
              key={game.gameId}
              {...game}
              onPlay={handlePlayGame}
            />
          ))}
        </div>
      )}
    </div>
  )
}
