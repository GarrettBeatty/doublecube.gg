import React, { useEffect, useState } from 'react'
import { useParams, useNavigate } from 'react-router-dom'
import { useSignalR } from '@/contexts/SignalRContext'
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from '@/components/ui/card'
import { Button } from '@/components/ui/button'
import { Badge } from '@/components/ui/badge'
import { Separator } from '@/components/ui/separator'
import { useToast } from '@/hooks/use-toast'
import { Trophy, Clock, Target, Award, TrendingUp, Home } from 'lucide-react'

interface GameResult {
  gameNumber: number
  winnerId: string
  winnerName: string
  pointsScored: number
  winType: 'Normal' | 'Gammon' | 'Backgammon'
  duration: number
  timestamp: string
}

interface MatchResult {
  matchId: string
  player1Id: string
  player1Name: string
  player1Score: number
  player2Id: string
  player2Name: string
  player2Score: number
  targetScore: number
  winnerId: string
  winnerName: string
  totalGamesPlayed: number
  hasCrawfordGameBeenPlayed: boolean
  startedAt: string
  completedAt: string
  duration: number
  games: GameResult[]
}

export const MatchResultsPage: React.FC = () => {
  const { matchId } = useParams<{ matchId: string }>()
  const navigate = useNavigate()
  const { invoke, isConnected } = useSignalR()
  const { toast } = useToast()

  const [results, setResults] = useState<MatchResult | null>(null)
  const [isLoading, setIsLoading] = useState(true)

  useEffect(() => {
    if (!matchId) {
      navigate('/')
      return
    }

    loadMatchResults()
  }, [matchId, isConnected])

  const loadMatchResults = async () => {
    if (!isConnected || !matchId) return

    setIsLoading(true)
    try {
      // Try to get match results from server
      const matchData = await invoke('GetMatchResults', matchId)
      if (matchData) {
        setResults(matchData)
      } else {
        toast({
          title: 'Match not found',
          description: 'Unable to load match results',
          variant: 'destructive',
        })
        navigate('/')
      }
    } catch (error) {
      console.error('[MatchResults] Failed to load match results:', error)
      toast({
        title: 'Error',
        description: 'Failed to load match results',
        variant: 'destructive',
      })
    } finally {
      setIsLoading(false)
    }
  }

  const formatDuration = (seconds: number): string => {
    const hours = Math.floor(seconds / 3600)
    const minutes = Math.floor((seconds % 3600) / 60)

    if (hours > 0) {
      return `${hours}h ${minutes}m`
    }
    return `${minutes}m`
  }

  if (isLoading) {
    return (
      <div className="min-h-screen bg-background flex items-center justify-center">
        <Card>
          <CardContent className="p-8">
            <div className="text-center">
              <div className="text-lg font-semibold">Loading results...</div>
            </div>
          </CardContent>
        </Card>
      </div>
    )
  }

  if (!results) {
    return null
  }

  const isPlayer1Winner = results.winnerId === results.player1Id

  return (
    <div className="min-h-screen bg-background">
      <div className="max-w-5xl mx-auto px-4 py-8">
        <Button variant="outline" onClick={() => navigate('/')} className="mb-6">
          <Home className="h-4 w-4 mr-2" />
          Back to Home
        </Button>

        {/* Winner Banner */}
        <Card className="mb-6 border-2 border-primary">
          <CardContent className="p-8">
            <div className="text-center">
              <Trophy className="h-16 w-16 mx-auto mb-4 text-primary" />
              <h1 className="text-4xl font-bold mb-2">{results.winnerName} Wins!</h1>
              <p className="text-xl text-muted-foreground">
                Match to {results.targetScore} points
              </p>
            </div>
          </CardContent>
        </Card>

        {/* Final Score */}
        <div className="grid grid-cols-2 gap-4 mb-6">
          <Card className={isPlayer1Winner ? 'border-2 border-primary' : ''}>
            <CardHeader>
              <CardTitle className="flex items-center justify-between">
                <span>{results.player1Name}</span>
                {isPlayer1Winner && <Award className="h-5 w-5 text-primary" />}
              </CardTitle>
            </CardHeader>
            <CardContent>
              <div className="text-5xl font-bold text-center">{results.player1Score}</div>
              <p className="text-center text-muted-foreground mt-2">
                {isPlayer1Winner ? 'Winner' : 'Runner-up'}
              </p>
            </CardContent>
          </Card>

          <Card className={!isPlayer1Winner ? 'border-2 border-primary' : ''}>
            <CardHeader>
              <CardTitle className="flex items-center justify-between">
                <span>{results.player2Name}</span>
                {!isPlayer1Winner && <Award className="h-5 w-5 text-primary" />}
              </CardTitle>
            </CardHeader>
            <CardContent>
              <div className="text-5xl font-bold text-center">{results.player2Score}</div>
              <p className="text-center text-muted-foreground mt-2">
                {!isPlayer1Winner ? 'Winner' : 'Runner-up'}
              </p>
            </CardContent>
          </Card>
        </div>

        {/* Match Statistics */}
        <Card className="mb-6">
          <CardHeader>
            <CardTitle>Match Statistics</CardTitle>
            <CardDescription>Overall performance summary</CardDescription>
          </CardHeader>
          <CardContent>
            <div className="grid grid-cols-1 md:grid-cols-3 gap-4">
              <div className="flex items-center gap-3 p-4 bg-muted rounded-lg">
                <Target className="h-8 w-8 text-primary" />
                <div>
                  <div className="text-sm text-muted-foreground">Games Played</div>
                  <div className="text-2xl font-bold">{results.totalGamesPlayed}</div>
                </div>
              </div>

              <div className="flex items-center gap-3 p-4 bg-muted rounded-lg">
                <Clock className="h-8 w-8 text-primary" />
                <div>
                  <div className="text-sm text-muted-foreground">Duration</div>
                  <div className="text-2xl font-bold">{formatDuration(results.duration)}</div>
                </div>
              </div>

              <div className="flex items-center gap-3 p-4 bg-muted rounded-lg">
                <TrendingUp className="h-8 w-8 text-primary" />
                <div>
                  <div className="text-sm text-muted-foreground">Crawford Game</div>
                  <div className="text-2xl font-bold">
                    {results.hasCrawfordGameBeenPlayed ? 'Yes' : 'No'}
                  </div>
                </div>
              </div>
            </div>
          </CardContent>
        </Card>

        {/* Game-by-Game Breakdown */}
        {results.games && results.games.length > 0 && (
          <Card>
            <CardHeader>
              <CardTitle>Game-by-Game Results</CardTitle>
              <CardDescription>Detailed breakdown of each game</CardDescription>
            </CardHeader>
            <CardContent>
              <div className="space-y-2">
                {results.games.map((game, index) => (
                  <div key={index}>
                    <div className="flex items-center justify-between p-4 rounded-lg hover:bg-muted transition-colors">
                      <div className="flex items-center gap-4">
                        <Badge variant="outline" className="w-20 justify-center">
                          Game {game.gameNumber}
                        </Badge>
                        <div>
                          <div className="font-semibold">{game.winnerName} won</div>
                          <div className="text-sm text-muted-foreground">
                            {game.winType} • +{game.pointsScored} pts
                          </div>
                        </div>
                      </div>
                      <div className="text-right">
                        <div className="text-sm text-muted-foreground">
                          {formatDuration(game.duration)}
                        </div>
                        <div className="text-xs text-muted-foreground">
                          {new Date(game.timestamp).toLocaleTimeString()}
                        </div>
                      </div>
                    </div>
                    {index < results.games.length - 1 && <Separator />}
                  </div>
                ))}
              </div>
            </CardContent>
          </Card>
        )}

        {/* Match Info */}
        <Card className="mt-6">
          <CardContent className="p-4">
            <div className="text-sm text-muted-foreground">
              <p>
                <strong>Match ID:</strong> {results.matchId}
              </p>
              <p className="mt-1">
                <strong>Started:</strong> {new Date(results.startedAt).toLocaleString()}
              </p>
              <p className="mt-1">
                <strong>Completed:</strong> {new Date(results.completedAt).toLocaleString()}
              </p>
            </div>
          </CardContent>
        </Card>

        {/* Actions */}
        <div className="mt-6 flex gap-4">
          <Button onClick={() => navigate('/')} className="flex-1">
            <Home className="h-4 w-4 mr-2" />
            Return to Home
          </Button>
          <Button variant="outline" onClick={() => window.location.reload()} className="flex-1">
            View Again
          </Button>
        </div>
      </div>
    </div>
  )
}
