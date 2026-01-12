import { useNavigate } from 'react-router-dom'
import { Card, CardContent } from '@/components/ui/card'
import { Button } from '@/components/ui/button'
import { Badge } from '@/components/ui/badge'
import { User, RefreshCw, AlertCircle, Dice6 } from 'lucide-react'
import { useActiveGames } from '@/hooks/useActiveGames'
import { ActiveGame } from '@/types/home.types'

function GameCard({
  game,
  onPlay,
}: {
  game: ActiveGame
  onPlay: (gameId: string, matchId: string) => void
}) {
  const opponentName = game.myColor === 'White' ? game.player2Name : game.player1Name
  const opponentRating = game.myColor === 'White' ? game.player2Rating : game.player1Rating

  return (
    <div
      className={`flex items-center justify-between p-4 border rounded-lg hover:bg-accent transition-colors ${
        game.isYourTurn ? 'bg-green-500/5 border-green-500/20' : 'opacity-70'
      }`}
    >
      <div className="flex items-center gap-3">
        <User className="h-5 w-5 text-muted-foreground" />
        <div>
          <div className="flex items-center gap-2">
            <span className={game.isYourTurn ? 'font-semibold' : ''}>{opponentName}</span>
            {opponentRating && (
              <span className="text-sm text-muted-foreground">({opponentRating})</span>
            )}
          </div>
          <div className="flex flex-wrap items-center gap-2 mt-1">
            {game.matchScore && (
              <Badge variant="outline" className="text-xs">
                {game.matchScore} / {game.matchLength}pt
              </Badge>
            )}
            {game.cubeValue && game.cubeValue > 1 && (
              <Badge variant="secondary" className="text-xs flex items-center gap-1">
                <Dice6 className="h-3 w-3" />
                {game.cubeValue}x
              </Badge>
            )}
            {game.isCrawford && (
              <Badge variant="outline" className="text-xs">
                Crawford
              </Badge>
            )}
            {game.isYourTurn && (
              <Badge variant="default" className="text-xs bg-green-600">
                Your turn
              </Badge>
            )}
          </div>
        </div>
      </div>
      <Button
        size="sm"
        variant={game.isYourTurn ? 'default' : 'outline'}
        className={game.isYourTurn ? 'bg-green-600 hover:bg-green-700' : ''}
        onClick={() => onPlay(game.gameId, game.matchId)}
      >
        {game.isYourTurn ? 'Play' : 'View'}
      </Button>
    </div>
  )
}

export function ActiveGames() {
  const navigate = useNavigate()
  const { games, isLoading, error, refresh, yourTurnCount } = useActiveGames()

  const handlePlayGame = (gameId: string, matchId: string) => {
    navigate(`/match/${matchId}/game/${gameId}`)
  }

  if (isLoading) {
    return (
      <div className="flex items-center justify-center py-8">
        <div className="text-muted-foreground">Loading active games...</div>
      </div>
    )
  }

  if (error) {
    return (
      <div className="flex flex-col items-center justify-center py-8 gap-4">
        <AlertCircle className="h-8 w-8 text-muted-foreground" />
        <div className="text-muted-foreground">{error}</div>
        <Button variant="outline" onClick={refresh}>
          <RefreshCw className="h-4 w-4 mr-2" />
          Retry
        </Button>
      </div>
    )
  }

  // Sort: your turn games first
  const sortedGames = [...games].sort((a, b) => {
    if (a.isYourTurn && !b.isYourTurn) return -1
    if (!a.isYourTurn && b.isYourTurn) return 1
    return 0
  })

  return (
    <div className="space-y-4">
      <div className="flex items-center gap-2">
        <h2 className="text-xl font-semibold">My Active Games</h2>
        {yourTurnCount > 0 && (
          <Badge variant="destructive" className="animate-pulse">
            {yourTurnCount} your turn
          </Badge>
        )}
        <Button variant="ghost" size="icon" onClick={refresh} title="Refresh">
          <RefreshCw className="h-4 w-4" />
        </Button>
      </div>

      {games.length === 0 ? (
        <Card>
          <CardContent className="flex flex-col items-center justify-center py-8 gap-2">
            <p className="text-muted-foreground">No active games</p>
            <p className="text-sm text-muted-foreground">
              Create or join a game to get started
            </p>
          </CardContent>
        </Card>
      ) : (
        <div className="space-y-2">
          {sortedGames.map((game) => (
            <GameCard key={game.gameId} game={game} onPlay={handlePlayGame} />
          ))}
        </div>
      )}
    </div>
  )
}
