import { useNavigate } from 'react-router-dom'
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from '@/components/ui/card'
import { Button } from '@/components/ui/button'
import { Badge } from '@/components/ui/badge'
import { RefreshCw, Trophy, Clock } from 'lucide-react'
import { useActiveMatches } from '@/hooks/useActiveMatches'
import { ActiveMatch } from '@/types/home.types'

interface MatchCardProps {
  match: ActiveMatch
  onViewMatch: (matchId: string) => void
  onPlayCurrentGame: (matchId: string, gameId: string) => void
}

function MatchCard({ match, onViewMatch, onPlayCurrentGame }: MatchCardProps) {
  const hasCurrentGame = !!match.currentGameId

  return (
    <div className="flex items-center justify-between p-4 border rounded-lg hover:bg-accent transition-colors">
      <div className="flex items-center gap-3">
        <Trophy className="h-5 w-5 text-muted-foreground" />
        <div>
          <div className="flex items-center gap-2">
            <span className="font-medium">vs {match.opponentName}</span>
          </div>
          <div className="flex flex-wrap items-center gap-2 mt-1">
            <Badge variant="outline" className="text-xs">
              {match.myScore} - {match.opponentScore} / {match.targetScore}pt
            </Badge>
            {match.gamesPlayed > 0 && (
              <span className="text-xs text-muted-foreground">
                {match.gamesPlayed} game{match.gamesPlayed !== 1 ? 's' : ''} played
              </span>
            )}
            {match.isCrawford && (
              <Badge variant="secondary" className="text-xs">
                Crawford
              </Badge>
            )}
            {match.isCorrespondence && (
              <Badge variant="outline" className="text-xs flex items-center gap-1">
                <Clock className="h-3 w-3" />
                Correspondence
              </Badge>
            )}
          </div>
        </div>
      </div>
      <div className="flex gap-2">
        {hasCurrentGame && (
          <Button
            size="sm"
            variant="default"
            onClick={() => onPlayCurrentGame(match.matchId, match.currentGameId!)}
          >
            Play Game
          </Button>
        )}
        <Button
          size="sm"
          variant="outline"
          onClick={() => onViewMatch(match.matchId)}
        >
          Match Details
        </Button>
      </div>
    </div>
  )
}

export function ActiveMatches() {
  const navigate = useNavigate()
  const { matches, isLoading, error, refresh } = useActiveMatches()

  const handleViewMatch = (matchId: string) => {
    navigate(`/match/${matchId}`)
  }

  const handlePlayCurrentGame = (matchId: string, gameId: string) => {
    navigate(`/match/${matchId}/game/${gameId}`)
  }

  if (isLoading) {
    return (
      <Card>
        <CardHeader className="flex flex-row items-center justify-between space-y-0 pb-2">
          <div>
            <CardTitle>Active Matches</CardTitle>
            <CardDescription>Your ongoing multi-game matches</CardDescription>
          </div>
        </CardHeader>
        <CardContent className="flex items-center justify-center py-8">
          <div className="text-muted-foreground">Loading matches...</div>
        </CardContent>
      </Card>
    )
  }

  if (error) {
    return (
      <Card>
        <CardHeader className="flex flex-row items-center justify-between space-y-0 pb-2">
          <div>
            <CardTitle>Active Matches</CardTitle>
            <CardDescription>Your ongoing multi-game matches</CardDescription>
          </div>
        </CardHeader>
        <CardContent className="flex flex-col items-center justify-center py-8 gap-2">
          <div className="text-muted-foreground">{error}</div>
          <Button variant="outline" size="sm" onClick={refresh}>
            <RefreshCw className="h-4 w-4 mr-2" />
            Retry
          </Button>
        </CardContent>
      </Card>
    )
  }

  // Don't render if no matches
  if (matches.length === 0) {
    return null
  }

  return (
    <Card>
      <CardHeader className="flex flex-row items-center justify-between space-y-0 pb-2">
        <div>
          <CardTitle className="flex items-center gap-2">
            Active Matches
            <Badge variant="secondary">{matches.length}</Badge>
          </CardTitle>
          <CardDescription>Your ongoing multi-game matches</CardDescription>
        </div>
        <Button variant="ghost" size="icon" onClick={refresh} title="Refresh">
          <RefreshCw className="h-4 w-4" />
        </Button>
      </CardHeader>
      <CardContent className="space-y-2">
        {matches.map((match) => (
          <MatchCard
            key={match.matchId}
            match={match}
            onViewMatch={handleViewMatch}
            onPlayCurrentGame={handlePlayCurrentGame}
          />
        ))}
      </CardContent>
    </Card>
  )
}
