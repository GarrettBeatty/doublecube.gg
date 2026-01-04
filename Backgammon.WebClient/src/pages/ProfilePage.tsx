import React, { useEffect, useState } from 'react'
import { useParams, useNavigate } from 'react-router-dom'
import { useSignalR } from '@/contexts/SignalRContext'
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from '@/components/ui/card'
import { Button } from '@/components/ui/button'
import { Badge } from '@/components/ui/badge'
import { Tabs, TabsContent, TabsList, TabsTrigger } from '@/components/ui/tabs'
import { Avatar, AvatarFallback } from '@/components/ui/avatar'
import { Separator } from '@/components/ui/separator'
import { useToast } from '@/hooks/use-toast'
import { User, Calendar, Trophy, TrendingUp } from 'lucide-react'

interface PlayerProfile {
  userId: string
  username: string
  displayName: string
  createdAt: string
  isPrivate: boolean
  statistics: {
    totalGamesPlayed: number
    wins: number
    losses: number
    winRate: number
    totalMatchesPlayed: number
    matchWins: number
    matchLosses: number
    matchWinRate: number
    averageGameDuration: number
  }
  recentGames: Array<{
    gameId: string
    opponentName: string
    result: 'won' | 'lost'
    pointsScored: number
    datePlayed: string
  }>
}

export const ProfilePage: React.FC = () => {
  const { username } = useParams<{ username: string }>()
  const navigate = useNavigate()
  const { invoke, isConnected } = useSignalR()
  const { toast } = useToast()

  const [profile, setProfile] = useState<PlayerProfile | null>(null)
  const [isLoading, setIsLoading] = useState(true)
  const [activeTab, setActiveTab] = useState('overview')

  useEffect(() => {
    if (!username) {
      navigate('/')
      return
    }

    loadProfile()
  }, [username, isConnected])

  const loadProfile = async () => {
    if (!isConnected || !username) return

    setIsLoading(true)
    try {
      const profileData = await invoke('GetPlayerProfile', username)
      if (profileData) {
        setProfile(profileData)
      } else {
        toast({
          title: 'Profile not found',
          description: `User ${username} does not exist`,
          variant: 'destructive',
        })
        navigate('/')
      }
    } catch (error) {
      console.error('Failed to load profile:', error)
      toast({
        title: 'Error',
        description: 'Failed to load profile',
        variant: 'destructive',
      })
    } finally {
      setIsLoading(false)
    }
  }

  if (isLoading) {
    return (
      <div className="min-h-screen bg-background flex items-center justify-center">
        <Card>
          <CardContent className="p-8">
            <div className="text-center">
              <div className="text-lg font-semibold">Loading profile...</div>
            </div>
          </CardContent>
        </Card>
      </div>
    )
  }

  if (!profile) {
    return null
  }

  const joinDate = new Date(profile.createdAt).toLocaleDateString()
  const initial = profile.displayName.charAt(0).toUpperCase()

  return (
    <div className="min-h-screen bg-background">
      <div className="max-w-6xl mx-auto px-4 py-8">
        <Button variant="outline" onClick={() => navigate('/')} className="mb-6">
          ← Back to Home
        </Button>

        {/* Profile Header */}
        <Card className="mb-6">
          <CardContent className="p-6">
            <div className="flex items-start gap-6">
              <Avatar className="h-24 w-24">
                <AvatarFallback className="text-3xl">{initial}</AvatarFallback>
              </Avatar>

              <div className="flex-1">
                <div className="flex items-center gap-3 mb-2">
                  <h1 className="text-3xl font-bold">{profile.displayName}</h1>
                  {profile.isPrivate && (
                    <Badge variant="secondary">
                      <User className="h-3 w-3 mr-1" />
                      Private
                    </Badge>
                  )}
                </div>
                <p className="text-muted-foreground mb-2">@{profile.username}</p>
                <div className="flex items-center gap-2 text-sm text-muted-foreground">
                  <Calendar className="h-4 w-4" />
                  <span>Joined {joinDate}</span>
                </div>
              </div>
            </div>
          </CardContent>
        </Card>

        {/* Statistics Cards */}
        <div className="grid grid-cols-1 md:grid-cols-4 gap-4 mb-6">
          <Card>
            <CardHeader className="pb-3">
              <CardTitle className="text-sm font-medium text-muted-foreground">
                Games Played
              </CardTitle>
            </CardHeader>
            <CardContent>
              <div className="text-2xl font-bold">{profile.statistics.totalGamesPlayed}</div>
              <p className="text-xs text-muted-foreground mt-1">
                {profile.statistics.wins}W - {profile.statistics.losses}L
              </p>
            </CardContent>
          </Card>

          <Card>
            <CardHeader className="pb-3">
              <CardTitle className="text-sm font-medium text-muted-foreground">
                Win Rate
              </CardTitle>
            </CardHeader>
            <CardContent>
              <div className="text-2xl font-bold">
                {(profile.statistics.winRate * 100).toFixed(1)}%
              </div>
              <p className="text-xs text-muted-foreground mt-1">
                <TrendingUp className="h-3 w-3 inline mr-1" />
                Single games
              </p>
            </CardContent>
          </Card>

          <Card>
            <CardHeader className="pb-3">
              <CardTitle className="text-sm font-medium text-muted-foreground">
                Matches Won
              </CardTitle>
            </CardHeader>
            <CardContent>
              <div className="text-2xl font-bold">{profile.statistics.matchWins}</div>
              <p className="text-xs text-muted-foreground mt-1">
                {profile.statistics.totalMatchesPlayed} total matches
              </p>
            </CardContent>
          </Card>

          <Card>
            <CardHeader className="pb-3">
              <CardTitle className="text-sm font-medium text-muted-foreground">
                Match Win Rate
              </CardTitle>
            </CardHeader>
            <CardContent>
              <div className="text-2xl font-bold">
                {(profile.statistics.matchWinRate * 100).toFixed(1)}%
              </div>
              <p className="text-xs text-muted-foreground mt-1">
                <Trophy className="h-3 w-3 inline mr-1" />
                Multi-game matches
              </p>
            </CardContent>
          </Card>
        </div>

        {/* Tabs */}
        <Tabs value={activeTab} onValueChange={setActiveTab}>
          <TabsList className="grid w-full grid-cols-3">
            <TabsTrigger value="overview">Overview</TabsTrigger>
            <TabsTrigger value="games">Recent Games</TabsTrigger>
            <TabsTrigger value="stats">Statistics</TabsTrigger>
          </TabsList>

          <TabsContent value="overview" className="mt-6">
            <Card>
              <CardHeader>
                <CardTitle>Profile Overview</CardTitle>
                <CardDescription>
                  Performance summary and recent activity
                </CardDescription>
              </CardHeader>
              <CardContent>
                <div className="space-y-4">
                  <div>
                    <h3 className="font-semibold mb-2">Career Statistics</h3>
                    <div className="grid grid-cols-2 gap-4">
                      <div className="flex justify-between p-3 bg-muted rounded-lg">
                        <span className="text-sm">Total Games:</span>
                        <span className="font-semibold">{profile.statistics.totalGamesPlayed}</span>
                      </div>
                      <div className="flex justify-between p-3 bg-muted rounded-lg">
                        <span className="text-sm">Total Matches:</span>
                        <span className="font-semibold">
                          {profile.statistics.totalMatchesPlayed}
                        </span>
                      </div>
                    </div>
                  </div>
                </div>
              </CardContent>
            </Card>
          </TabsContent>

          <TabsContent value="games" className="mt-6">
            <Card>
              <CardHeader>
                <CardTitle>Recent Games</CardTitle>
                <CardDescription>Last 10 games played</CardDescription>
              </CardHeader>
              <CardContent>
                {profile.recentGames && profile.recentGames.length > 0 ? (
                  <div className="space-y-2">
                    {profile.recentGames.map((game, index) => (
                      <div key={game.gameId || index}>
                        <div className="flex items-center justify-between p-3 rounded-lg hover:bg-muted transition-colors">
                          <div className="flex items-center gap-4">
                            <Badge variant={game.result === 'won' ? 'default' : 'secondary'}>
                              {game.result === 'won' ? 'Won' : 'Lost'}
                            </Badge>
                            <span className="font-medium">vs {game.opponentName}</span>
                            <span className="text-sm text-muted-foreground">
                              +{game.pointsScored} pts
                            </span>
                          </div>
                          <span className="text-sm text-muted-foreground">
                            {new Date(game.datePlayed).toLocaleDateString()}
                          </span>
                        </div>
                        {index < profile.recentGames.length - 1 && <Separator />}
                      </div>
                    ))}
                  </div>
                ) : (
                  <p className="text-center text-muted-foreground py-8">No recent games</p>
                )}
              </CardContent>
            </Card>
          </TabsContent>

          <TabsContent value="stats" className="mt-6">
            <Card>
              <CardHeader>
                <CardTitle>Detailed Statistics</CardTitle>
                <CardDescription>Complete performance breakdown</CardDescription>
              </CardHeader>
              <CardContent>
                <div className="space-y-6">
                  <div>
                    <h3 className="font-semibold mb-3">Single Game Performance</h3>
                    <div className="space-y-2">
                      <div className="flex justify-between p-2">
                        <span className="text-muted-foreground">Total Games:</span>
                        <span className="font-medium">{profile.statistics.totalGamesPlayed}</span>
                      </div>
                      <div className="flex justify-between p-2">
                        <span className="text-muted-foreground">Wins:</span>
                        <span className="font-medium text-green-600">
                          {profile.statistics.wins}
                        </span>
                      </div>
                      <div className="flex justify-between p-2">
                        <span className="text-muted-foreground">Losses:</span>
                        <span className="font-medium text-red-600">
                          {profile.statistics.losses}
                        </span>
                      </div>
                      <div className="flex justify-between p-2">
                        <span className="text-muted-foreground">Win Rate:</span>
                        <span className="font-medium">
                          {(profile.statistics.winRate * 100).toFixed(1)}%
                        </span>
                      </div>
                    </div>
                  </div>

                  <Separator />

                  <div>
                    <h3 className="font-semibold mb-3">Match Performance</h3>
                    <div className="space-y-2">
                      <div className="flex justify-between p-2">
                        <span className="text-muted-foreground">Total Matches:</span>
                        <span className="font-medium">
                          {profile.statistics.totalMatchesPlayed}
                        </span>
                      </div>
                      <div className="flex justify-between p-2">
                        <span className="text-muted-foreground">Match Wins:</span>
                        <span className="font-medium text-green-600">
                          {profile.statistics.matchWins}
                        </span>
                      </div>
                      <div className="flex justify-between p-2">
                        <span className="text-muted-foreground">Match Losses:</span>
                        <span className="font-medium text-red-600">
                          {profile.statistics.matchLosses}
                        </span>
                      </div>
                      <div className="flex justify-between p-2">
                        <span className="text-muted-foreground">Match Win Rate:</span>
                        <span className="font-medium">
                          {(profile.statistics.matchWinRate * 100).toFixed(1)}%
                        </span>
                      </div>
                    </div>
                  </div>
                </div>
              </CardContent>
            </Card>
          </TabsContent>
        </Tabs>
      </div>
    </div>
  )
}
