import React, { useEffect, useState } from 'react'
import { useParams, useNavigate } from 'react-router-dom'
import { useSignalR } from '@/contexts/SignalRContext'
import { useAuth } from '@/contexts/AuthContext'
import { useUserStats } from '@/hooks/useUserStats'
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from '@/components/ui/card'
import { Button } from '@/components/ui/button'
import { Badge } from '@/components/ui/badge'
import { Tabs, TabsContent, TabsList, TabsTrigger } from '@/components/ui/tabs'
import { Avatar, AvatarFallback } from '@/components/ui/avatar'
import { Separator } from '@/components/ui/separator'
import { Skeleton } from '@/components/ui/skeleton'
import { User, Calendar, Trophy, TrendingUp, TrendingDown, Target, Zap, Eye } from 'lucide-react'
import { PerformanceGraph } from '@/components/home/PerformanceGraph'

interface PlayerProfile {
  userId: string
  username: string
  displayName: string
  createdAt: string
  isPrivate: boolean
  stats?: {
    totalGames: number
    wins: number
    losses: number
    totalStakes: number
    normalWins: number
    gammonWins: number
    backgammonWins: number
    winStreak: number
    bestWinStreak: number
  }
  recentGames?: Array<{
    gameId: string
    opponentName: string
    result: 'won' | 'lost'
    pointsScored: number
    datePlayed: string
  }>
  friends?: Array<{
    userId: string
    username: string
    displayName: string
    status: string
  }>
  isFriend: boolean
  profilePrivacy: number
  gameHistoryPrivacy: number
  friendsListPrivacy: number
}

export const ProfilePage: React.FC = () => {
  const { username } = useParams<{ username: string }>()
  const navigate = useNavigate()
  const { hub, isConnected } = useSignalR()
  const { user } = useAuth()
  const { stats: userStats, isLoading: userStatsLoading } = useUserStats()

  const [profile, setProfile] = useState<PlayerProfile | null>(null)
  const [isLoading, setIsLoading] = useState(true)
  const [notFound, setNotFound] = useState(false)
  const [activeTab, setActiveTab] = useState('overview')

  // Check if viewing own profile
  const isOwnProfile = user && profile && user.username === profile.username

  useEffect(() => {
    if (!username) {
      navigate('/')
      return
    }

    loadProfile()
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [username, isConnected])

  const loadProfile = async () => {
    if (!isConnected || !username) return

    setIsLoading(true)
    setNotFound(false)
    try {
      const profileData = await hub?.getPlayerProfile(username)
      if (profileData) {
        // Map PlayerProfileDto to local PlayerProfile type
        setProfile({
          ...profileData,
          createdAt: typeof profileData.createdAt === 'string'
            ? profileData.createdAt
            : profileData.createdAt.toISOString(),
          friends: profileData.friends?.map((f) => ({
            userId: f.userId,
            username: f.username,
            displayName: f.displayName,
            status: f.isOnline ? 'Online' : 'Offline',
          })),
          recentGames: profileData.recentGames?.map((g) => ({
            gameId: g.gameId,
            opponentName: g.opponentUsername,
            result: g.won ? 'won' as const : 'lost' as const,
            pointsScored: g.stakes,
            datePlayed: typeof g.completedAt === 'string'
              ? g.completedAt
              : g.completedAt.toISOString(),
          })),
        })
      } else {
        setNotFound(true)
      }
    } catch (error) {
      console.error('Failed to load profile:', error)
      setNotFound(true)
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

  if (notFound) {
    return (
      <div className="min-h-screen bg-background">
        <div className="max-w-6xl mx-auto px-4 py-8">
          <Button variant="outline" onClick={() => navigate('/')} className="mb-6">
            ← Back to Home
          </Button>

          <Card className="border-destructive">
            <CardHeader>
              <CardTitle className="flex items-center gap-2">
                <User className="h-5 w-5" />
                User Not Found
              </CardTitle>
              <CardDescription>
                The profile you're looking for doesn't exist
              </CardDescription>
            </CardHeader>
            <CardContent>
              <p className="text-muted-foreground mb-4">
                User <span className="font-mono font-semibold">@{username}</span> does not exist or may have been deleted.
              </p>
              <Button onClick={() => navigate('/')}>Return to Home</Button>
            </CardContent>
          </Card>
        </div>
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

        {/* Personal Stats (Own Profile Only) */}
        {isOwnProfile && (
          <Card className="mb-6">
            <CardHeader>
              <CardTitle className="flex items-center gap-2">
                <Trophy className="h-5 w-5 text-yellow-500" />
                Your Stats
              </CardTitle>
            </CardHeader>
            <CardContent>
              {userStatsLoading ? (
                <div className="grid grid-cols-1 md:grid-cols-3 gap-4">
                  <Skeleton className="h-24 w-full" />
                  <Skeleton className="h-24 w-full" />
                  <Skeleton className="h-24 w-full" />
                </div>
              ) : userStats ? (
                <div className="grid grid-cols-1 md:grid-cols-3 gap-4">
                  {/* Rating */}
                  <div className="p-4 bg-accent rounded-lg">
                    <div className="flex items-center gap-2 mb-2">
                      <TrendingUp className="h-4 w-4 text-muted-foreground" />
                      <p className="text-sm text-muted-foreground">Rating</p>
                    </div>
                    <div className="flex items-center gap-2">
                      <span className="text-3xl font-bold">{userStats.rating}</span>
                    </div>
                  </div>

                  {/* Current Streak */}
                  <div className="p-4 bg-accent rounded-lg">
                    <div className="flex items-center gap-2 mb-2">
                      <Target className="h-4 w-4 text-muted-foreground" />
                      <p className="text-sm text-muted-foreground">Current Streak</p>
                    </div>
                    <div className="flex items-center gap-2">
                      {userStats.streakType === 'win' ? (
                        <TrendingUp className="h-5 w-5 text-green-500" />
                      ) : (
                        <TrendingDown className="h-5 w-5 text-red-500" />
                      )}
                      <span className="text-2xl font-bold">
                        {userStats.currentStreak} {userStats.streakType}
                      </span>
                    </div>
                  </div>

                  {/* Activity */}
                  <div className="p-4 bg-accent rounded-lg">
                    <div className="flex items-center gap-2 mb-2">
                      <Zap className="h-4 w-4 text-muted-foreground" />
                      <p className="text-sm text-muted-foreground">Recent Activity</p>
                    </div>
                    <div className="space-y-1">
                      <p className="text-sm">
                        Today: <span className="font-semibold">{userStats.gamesToday} games</span>
                      </p>
                      <p className="text-sm">
                        This week: <span className="font-semibold">{userStats.gamesThisWeek} games</span>
                      </p>
                    </div>
                  </div>
                </div>
              ) : (
                <p className="text-center text-muted-foreground py-4">Stats unavailable</p>
              )}
            </CardContent>
          </Card>
        )}

        {/* Rating Progress (Own Profile Only) */}
        {isOwnProfile && (
          <div className="mb-6">
            <PerformanceGraph />
          </div>
        )}

        {/* Statistics Cards */}
        {profile.stats && !profile.isPrivate && (
          <div className="grid grid-cols-1 md:grid-cols-4 gap-4 mb-6">
            <Card>
              <CardHeader className="pb-3">
                <CardTitle className="text-sm font-medium text-muted-foreground">
                  Games Played
                </CardTitle>
              </CardHeader>
              <CardContent>
                <div className="text-2xl font-bold">{profile.stats.totalGames}</div>
                <p className="text-xs text-muted-foreground mt-1">
                  {profile.stats.wins}W - {profile.stats.losses}L
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
                  {profile.stats.totalGames > 0
                    ? ((profile.stats.wins / profile.stats.totalGames) * 100).toFixed(1)
                    : '0.0'}%
                </div>
                <p className="text-xs text-muted-foreground mt-1">
                  <TrendingUp className="h-3 w-3 inline mr-1" />
                  {profile.stats.totalGames} games
                </p>
              </CardContent>
            </Card>

            <Card>
              <CardHeader className="pb-3">
                <CardTitle className="text-sm font-medium text-muted-foreground">
                  Win Streak
                </CardTitle>
              </CardHeader>
              <CardContent>
                <div className="text-2xl font-bold">{profile.stats.winStreak}</div>
                <p className="text-xs text-muted-foreground mt-1">
                  Best: {profile.stats.bestWinStreak}
                </p>
              </CardContent>
            </Card>

            <Card>
              <CardHeader className="pb-3">
                <CardTitle className="text-sm font-medium text-muted-foreground">
                  Special Wins
                </CardTitle>
              </CardHeader>
              <CardContent>
                <div className="text-sm space-y-1">
                  <div className="flex justify-between">
                    <span className="text-muted-foreground">Gammon:</span>
                    <span className="font-semibold">{profile.stats.gammonWins}</span>
                  </div>
                  <div className="flex justify-between">
                    <span className="text-muted-foreground">Backgammon:</span>
                    <span className="font-semibold">{profile.stats.backgammonWins}</span>
                  </div>
                </div>
              </CardContent>
            </Card>
          </div>
        )}

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
                {profile.stats && !profile.isPrivate ? (
                  <div className="space-y-4">
                    <div>
                      <h3 className="font-semibold mb-2">Career Statistics</h3>
                      <div className="grid grid-cols-2 gap-4">
                        <div className="flex justify-between p-3 bg-muted rounded-lg">
                          <span className="text-sm">Total Games:</span>
                          <span className="font-semibold">{profile.stats.totalGames}</span>
                        </div>
                        <div className="flex justify-between p-3 bg-muted rounded-lg">
                          <span className="text-sm">Win Rate:</span>
                          <span className="font-semibold">
                            {profile.stats.totalGames > 0
                              ? ((profile.stats.wins / profile.stats.totalGames) * 100).toFixed(1)
                              : '0.0'}%
                          </span>
                        </div>
                        <div className="flex justify-between p-3 bg-muted rounded-lg">
                          <span className="text-sm">Total Stakes:</span>
                          <span className="font-semibold">{profile.stats.totalStakes}</span>
                        </div>
                        <div className="flex justify-between p-3 bg-muted rounded-lg">
                          <span className="text-sm">Best Streak:</span>
                          <span className="font-semibold">{profile.stats.bestWinStreak}</span>
                        </div>
                      </div>
                    </div>
                  </div>
                ) : (
                  <p className="text-center text-muted-foreground py-8">Profile statistics are private</p>
                )}
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
                          <div className="flex items-center gap-3">
                            <span className="text-sm text-muted-foreground">
                              {new Date(game.datePlayed).toLocaleDateString()}
                            </span>
                            <Button
                              size="sm"
                              variant="outline"
                              onClick={() => navigate(`/match/${game.gameId}/results`)}
                            >
                              <Eye className="h-4 w-4 mr-1" />
                              View
                            </Button>
                          </div>
                        </div>
                        {profile.recentGames && index < profile.recentGames.length - 1 && <Separator />}
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
                {profile.stats && !profile.isPrivate ? (
                  <div className="space-y-6">
                    <div>
                      <h3 className="font-semibold mb-3">Game Performance</h3>
                      <div className="space-y-2">
                        <div className="flex justify-between p-2">
                          <span className="text-muted-foreground">Total Games:</span>
                          <span className="font-medium">{profile.stats.totalGames}</span>
                        </div>
                        <div className="flex justify-between p-2">
                          <span className="text-muted-foreground">Wins:</span>
                          <span className="font-medium text-green-600">
                            {profile.stats.wins}
                          </span>
                        </div>
                        <div className="flex justify-between p-2">
                          <span className="text-muted-foreground">Losses:</span>
                          <span className="font-medium text-red-600">
                            {profile.stats.losses}
                          </span>
                        </div>
                        <div className="flex justify-between p-2">
                          <span className="text-muted-foreground">Win Rate:</span>
                          <span className="font-medium">
                            {profile.stats.totalGames > 0
                              ? ((profile.stats.wins / profile.stats.totalGames) * 100).toFixed(1)
                              : '0.0'}%
                          </span>
                        </div>
                      </div>
                    </div>

                    <Separator />

                    <div>
                      <h3 className="font-semibold mb-3">Win Types</h3>
                      <div className="space-y-2">
                        <div className="flex justify-between p-2">
                          <span className="text-muted-foreground">Normal Wins:</span>
                          <span className="font-medium">{profile.stats.normalWins}</span>
                        </div>
                        <div className="flex justify-between p-2">
                          <span className="text-muted-foreground">Gammon Wins:</span>
                          <span className="font-medium text-yellow-600">
                            {profile.stats.gammonWins}
                          </span>
                        </div>
                        <div className="flex justify-between p-2">
                          <span className="text-muted-foreground">Backgammon Wins:</span>
                          <span className="font-medium text-orange-600">
                            {profile.stats.backgammonWins}
                          </span>
                        </div>
                      </div>
                    </div>

                    <Separator />

                    <div>
                      <h3 className="font-semibold mb-3">Streaks & Stakes</h3>
                      <div className="space-y-2">
                        <div className="flex justify-between p-2">
                          <span className="text-muted-foreground">Current Win Streak:</span>
                          <span className="font-medium">{profile.stats.winStreak}</span>
                        </div>
                        <div className="flex justify-between p-2">
                          <span className="text-muted-foreground">Best Win Streak:</span>
                          <span className="font-medium text-purple-600">
                            {profile.stats.bestWinStreak}
                          </span>
                        </div>
                        <div className="flex justify-between p-2">
                          <span className="text-muted-foreground">Total Stakes:</span>
                          <span className="font-medium">{profile.stats.totalStakes}</span>
                        </div>
                      </div>
                    </div>
                  </div>
                ) : (
                  <p className="text-center text-muted-foreground py-8">Profile statistics are private</p>
                )}
              </CardContent>
            </Card>
          </TabsContent>
        </Tabs>
      </div>
    </div>
  )
}
