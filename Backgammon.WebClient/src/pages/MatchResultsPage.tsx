import { useParams, useNavigate } from 'react-router-dom'
import { useState, useEffect } from 'react'
import { useSignalR } from '@/contexts/SignalRContext'
import type { MatchResultsDto } from '@/types/generated/Backgammon.Server.Models.SignalR'
import { Card, CardContent, CardHeader, CardTitle } from '@/components/ui/card'
import { Button } from '@/components/ui/button'
import { ArrowLeft, Trophy } from 'lucide-react'

export const MatchResultsPage: React.FC = () => {
  const { matchId } = useParams<{ matchId: string }>()
  const { hub, isConnected } = useSignalR()
  const navigate = useNavigate()
  const [matchResults, setMatchResults] = useState<MatchResultsDto | null>(null)
  const [isLoading, setIsLoading] = useState(true)
  const [error, setError] = useState<string | null>(null)

  useEffect(() => {
    const fetchMatchResults = async () => {
      if (!matchId || !isConnected || !hub) return

      try {
        const results = await hub.getMatchResults(matchId)
        if (results) {
          setMatchResults(results)
        } else {
          setError('Match not found')
        }
      } catch (err) {
        setError('Failed to load match results')
        console.error('[MatchResultsPage] Error:', err)
      } finally {
        setIsLoading(false)
      }
    }

    fetchMatchResults()
  }, [matchId, isConnected, hub])

  if (isLoading) {
    return (
      <div className="flex items-center justify-center h-screen">
        <p className="text-muted-foreground">Loading match results...</p>
      </div>
    )
  }

  if (error || !matchResults) {
    return (
      <div className="container mx-auto px-4 py-8">
        <Card>
          <CardContent className="pt-6">
            <p className="text-destructive">{error || 'Match not found'}</p>
            <Button onClick={() => navigate('/')} className="mt-4">
              <ArrowLeft className="mr-2 h-4 w-4" />
              Back to Home
            </Button>
          </CardContent>
        </Card>
      </div>
    )
  }

  return (
    <div className="container mx-auto px-4 py-8 max-w-4xl">
      <Button
        variant="ghost"
        onClick={() => navigate('/')}
        className="mb-4"
      >
        <ArrowLeft className="mr-2 h-4 w-4" />
        Back to Home
      </Button>

      <Card className="mb-6">
        <CardHeader>
          <CardTitle className="flex items-center gap-2">
            <Trophy className="h-6 w-6 text-yellow-500" />
            Match Complete
          </CardTitle>
        </CardHeader>
        <CardContent>
          <div className="space-y-4">
            <div>
              <p className="text-2xl font-bold">
                Winner: {matchResults.winnerUsername}
              </p>
              <p className="text-muted-foreground">
                Final Score: {matchResults.finalScore.player1} - {matchResults.finalScore.player2}
              </p>
            </div>
            <div className="grid grid-cols-2 md:grid-cols-3 gap-4">
              <div>
                <p className="text-sm text-muted-foreground">Target Score</p>
                <p className="text-lg font-semibold">{matchResults.targetScore}</p>
              </div>
              <div>
                <p className="text-sm text-muted-foreground">Total Games</p>
                <p className="text-lg font-semibold">{matchResults.totalGames}</p>
              </div>
              <div>
                <p className="text-sm text-muted-foreground">Duration</p>
                <p className="text-lg font-semibold">{matchResults.duration}</p>
              </div>
            </div>
          </div>
        </CardContent>
      </Card>

      <Card>
        <CardHeader>
          <CardTitle>Game History</CardTitle>
        </CardHeader>
        <CardContent>
          {matchResults.games.length === 0 ? (
            <p className="text-muted-foreground text-center py-4">No games recorded</p>
          ) : (
            <div className="space-y-2">
              {matchResults.games.map((game) => (
                <div
                  key={game.gameId}
                  className="flex items-center justify-between p-3 border rounded hover:bg-accent transition-colors"
                >
                  <div className="flex items-center gap-2">
                    <span className="font-semibold text-sm">
                      Game {game.gameNumber}
                    </span>
                    {game.isCrawford && (
                      <span className="text-xs bg-yellow-100 dark:bg-yellow-900 text-yellow-800 dark:text-yellow-200 px-2 py-1 rounded">
                        Crawford
                      </span>
                    )}
                  </div>
                  <div className="text-right">
                    <p className="font-semibold text-sm">
                      {game.winner} wins
                    </p>
                    <p className="text-xs text-muted-foreground">
                      {game.winType || 'Normal'}
                      {' '}
                      ({game.pointsScored} {game.pointsScored === 1 ? 'point' : 'points'})
                    </p>
                  </div>
                </div>
              ))}
            </div>
          )}
        </CardContent>
      </Card>
    </div>
  )
}
