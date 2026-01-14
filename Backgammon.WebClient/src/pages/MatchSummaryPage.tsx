import { useEffect, useState } from 'react'
import { useParams, useNavigate } from 'react-router-dom'
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from '@/components/ui/card'
import { Button } from '@/components/ui/button'
import { Badge } from '@/components/ui/badge'
import { ArrowLeft, Play, Eye, Trophy } from 'lucide-react'
import { useSignalR } from '@/contexts/SignalRContext'
import type { MatchGameDto } from '@/types/generated/Backgammon.Server.Models.SignalR'

interface MatchDetails {
  matchId: string
  player1Name: string
  player2Name: string
  player1Score: number
  player2Score: number
  targetScore: number
  matchComplete: boolean
  matchWinner?: string
  currentGameId?: string
  isCrawford: boolean
}

export function MatchSummaryPage() {
  const { matchId } = useParams<{ matchId: string }>()
  const navigate = useNavigate()
  const { hub, isConnected } = useSignalR()
  const [match, setMatch] = useState<MatchDetails | null>(null)
  const [games, setGames] = useState<MatchGameDto[]>([])
  const [isLoading, setIsLoading] = useState(true)
  const [error, setError] = useState<string | null>(null)

  useEffect(() => {
    const fetchMatchDetails = async () => {
      if (!isConnected || !hub || !matchId) {
        return
      }

      try {
        setIsLoading(true)
        setError(null)

        // Get match state and games in parallel
        const [matchState, matchGames] = await Promise.all([
          hub.getMatchState(matchId),
          hub.getMatchGames(matchId),
        ])

        if (!matchState) {
          setError('Match not found')
          return
        }

        // Transform to our local type using available fields
        setMatch({
          matchId: matchState.matchId,
          player1Name: matchState.player1Name ?? 'Player 1',
          player2Name: matchState.player2Name ?? 'Player 2',
          player1Score: matchState.player1Score,
          player2Score: matchState.player2Score,
          targetScore: matchState.targetScore,
          matchComplete: matchState.matchComplete,
          matchWinner: matchState.matchWinner ?? undefined,
          currentGameId: matchState.currentGameId ?? undefined,
          isCrawford: matchState.isCrawfordGame,
        })

        setGames(matchGames || [])
      } catch (err) {
        console.error('Failed to fetch match details:', err)
        setError('Failed to load match details')
      } finally {
        setIsLoading(false)
      }
    }

    fetchMatchDetails()
  }, [isConnected, hub, matchId])

  const handlePlayGame = (gameId: string) => {
    navigate(`/match/${matchId}/game/${gameId}`)
  }

  const handleViewGame = (gameId: string) => {
    navigate(`/match/${matchId}/game/${gameId}`)
  }

  const handleBack = () => {
    navigate('/')
  }

  if (isLoading) {
    return (
      <div className="container mx-auto px-4 py-8">
        <div className="max-w-3xl mx-auto">
          <Card>
            <CardContent className="flex items-center justify-center py-12">
              <div className="text-muted-foreground">Loading match details...</div>
            </CardContent>
          </Card>
        </div>
      </div>
    )
  }

  if (error || !match) {
    return (
      <div className="container mx-auto px-4 py-8">
        <div className="max-w-3xl mx-auto">
          <Card>
            <CardContent className="flex flex-col items-center justify-center py-12 gap-4">
              <div className="text-muted-foreground">{error || 'Match not found'}</div>
              <Button variant="outline" onClick={handleBack}>
                <ArrowLeft className="h-4 w-4 mr-2" />
                Back to Home
              </Button>
            </CardContent>
          </Card>
        </div>
      </div>
    )
  }

  return (
    <div className="container mx-auto px-4 py-8">
      <div className="max-w-3xl mx-auto space-y-6">
        {/* Back button */}
        <Button variant="ghost" onClick={handleBack} className="mb-4">
          <ArrowLeft className="h-4 w-4 mr-2" />
          Back
        </Button>

        {/* Match Header */}
        <Card>
          <CardHeader>
            <div className="flex items-center justify-between">
              <div>
                <CardTitle className="text-2xl flex items-center gap-2">
                  <Trophy className="h-6 w-6" />
                  Match Summary
                </CardTitle>
                <CardDescription>
                  {match.player1Name} vs {match.player2Name}
                </CardDescription>
              </div>
              <div className="text-right">
                <div className="text-3xl font-bold">
                  {match.player1Score} - {match.player2Score}
                </div>
                <div className="text-sm text-muted-foreground">
                  First to {match.targetScore}
                </div>
              </div>
            </div>
          </CardHeader>
          <CardContent>
            <div className="flex flex-wrap gap-2">
              <Badge variant={match.matchComplete ? 'secondary' : 'default'}>
                {match.matchComplete ? 'Completed' : 'In Progress'}
              </Badge>
              {match.matchWinner && (
                <Badge variant="default" className="bg-yellow-600">
                  Winner: {match.matchWinner}
                </Badge>
              )}
              {match.isCrawford && (
                <Badge variant="outline">Crawford Game</Badge>
              )}
            </div>
          </CardContent>
        </Card>

        {/* Current Game CTA */}
        {match.currentGameId && !match.matchComplete && (
          <Card className="bg-green-500/5 border-green-500/20">
            <CardContent className="flex items-center justify-between py-4">
              <div>
                <div className="font-medium">Current Game Ready</div>
                <div className="text-sm text-muted-foreground">
                  Continue playing the current game
                </div>
              </div>
              <Button onClick={() => handlePlayGame(match.currentGameId!)}>
                <Play className="h-4 w-4 mr-2" />
                Play Now
              </Button>
            </CardContent>
          </Card>
        )}

        {/* Games List */}
        {games.length > 0 && (
          <Card>
            <CardHeader>
              <CardTitle>Games</CardTitle>
              <CardDescription>
                {games.length} game{games.length !== 1 ? 's' : ''} in this match
              </CardDescription>
            </CardHeader>
            <CardContent className="space-y-3">
              {games.map((game) => (
                <div
                  key={game.gameId}
                  className="flex items-center justify-between p-4 border rounded-lg"
                >
                  <div className="flex items-center gap-3">
                    <div className="font-medium">Game {game.gameNumber}</div>
                    <div className="flex flex-wrap gap-2">
                      {game.status === 'InProgress' ? (
                        <Badge variant="default" className="bg-green-600">
                          In Progress
                        </Badge>
                      ) : (
                        <>
                          {game.winner && (
                            <Badge variant="secondary">
                              {game.winner} won
                            </Badge>
                          )}
                          {game.winType && game.winType !== 'Normal' && (
                            <Badge variant="outline">{game.winType}</Badge>
                          )}
                          {game.pointsScored > 1 && (
                            <Badge variant="outline">+{game.pointsScored} pts</Badge>
                          )}
                        </>
                      )}
                      {game.isCrawford && (
                        <Badge variant="outline">Crawford</Badge>
                      )}
                    </div>
                  </div>
                  <div className="flex gap-2">
                    {game.status === 'InProgress' ? (
                      <Button
                        size="sm"
                        onClick={() => handlePlayGame(game.gameId)}
                      >
                        <Play className="h-4 w-4 mr-1" />
                        Play
                      </Button>
                    ) : (
                      <Button
                        size="sm"
                        variant="outline"
                        onClick={() => handleViewGame(game.gameId)}
                      >
                        <Eye className="h-4 w-4 mr-1" />
                        Review
                      </Button>
                    )}
                  </div>
                </div>
              ))}
            </CardContent>
          </Card>
        )}

        {/* Match Complete - View Results */}
        {match.matchComplete && (
          <Card>
            <CardContent className="flex items-center justify-between py-4">
              <div>
                <div className="font-medium">Match Complete</div>
                <div className="text-sm text-muted-foreground">
                  View the full match results
                </div>
              </div>
              <Button variant="outline" onClick={() => navigate(`/match/${matchId}/results`)}>
                View Results
              </Button>
            </CardContent>
          </Card>
        )}
      </div>
    </div>
  )
}
