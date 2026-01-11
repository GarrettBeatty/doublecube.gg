import React, { useCallback, useEffect, useState } from 'react'
import { useNavigate } from 'react-router-dom'
import { useSignalR } from '@/contexts/SignalRContext'
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from '@/components/ui/card'
import { Tabs, TabsContent, TabsList, TabsTrigger } from '@/components/ui/tabs'
import { Button } from '@/components/ui/button'
import { Users, Trophy, BarChart3, Bot, ArrowLeft } from 'lucide-react'
import { OnlinePlayersList } from '@/components/players/OnlinePlayersList'
import { Leaderboard } from '@/components/players/Leaderboard'
import { RatingDistribution } from '@/components/players/RatingDistribution'
import { OnlineBotsList } from '@/components/players/OnlineBotsList'
import type {
  LeaderboardEntryDto,
  OnlinePlayerDto,
  RatingDistributionDto,
  BotInfoDto
} from '@/types/players'

export const PlayersPage: React.FC = () => {
  const navigate = useNavigate()
  const { hub, isConnected } = useSignalR()
  const [activeTab, setActiveTab] = useState('online')
  const [isLoading, setIsLoading] = useState(true)

  const [onlinePlayers, setOnlinePlayers] = useState<OnlinePlayerDto[]>([])
  const [leaderboard, setLeaderboard] = useState<LeaderboardEntryDto[]>([])
  const [ratingDistribution, setRatingDistribution] = useState<RatingDistributionDto | null>(null)
  const [bots, setBots] = useState<BotInfoDto[]>([])

  const loadAllData = useCallback(async () => {
    setIsLoading(true)
    try {
      const [online, leaders, distribution, botList] = await Promise.all([
        hub?.getOnlinePlayers() as Promise<OnlinePlayerDto[] | null>,
        hub?.getLeaderboard(50) as Promise<LeaderboardEntryDto[] | null>,
        hub?.getRatingDistribution() as Promise<RatingDistributionDto | null>,
        hub?.getAvailableBots() as Promise<BotInfoDto[] | null>
      ])
      setOnlinePlayers(online || [])
      setLeaderboard(leaders || [])
      setRatingDistribution(distribution || null)
      setBots(botList || [])
    } catch (error) {
      console.error('Failed to load players data:', error)
    } finally {
      setIsLoading(false)
    }
  }, [hub])

  useEffect(() => {
    if (isConnected) {
      loadAllData()
    }
  }, [isConnected, loadAllData])

  const handleRefresh = () => {
    loadAllData()
  }

  return (
    <div className="min-h-screen bg-background">
      <div className="max-w-6xl mx-auto px-4 py-8">
        <Button variant="outline" onClick={() => navigate('/')} className="mb-6">
          <ArrowLeft className="h-4 w-4 mr-2" />
          Back to Home
        </Button>

        <div className="flex items-center justify-between mb-6">
          <div>
            <h1 className="text-3xl font-bold flex items-center gap-3">
              <Users className="h-8 w-8" />
              Players
            </h1>
            <p className="text-muted-foreground mt-1">
              Find opponents, check rankings, and explore the community
            </p>
          </div>
          <Button variant="outline" onClick={handleRefresh} disabled={isLoading}>
            {isLoading ? 'Loading...' : 'Refresh'}
          </Button>
        </div>

        <Tabs value={activeTab} onValueChange={setActiveTab}>
          <TabsList className="grid w-full grid-cols-4 mb-6">
            <TabsTrigger value="online" className="flex items-center gap-2">
              <Users className="h-4 w-4" />
              <span className="hidden sm:inline">Online</span>
              {onlinePlayers.length > 0 && (
                <span className="ml-1 text-xs bg-primary/20 px-1.5 py-0.5 rounded-full">
                  {onlinePlayers.length}
                </span>
              )}
            </TabsTrigger>
            <TabsTrigger value="leaderboard" className="flex items-center gap-2">
              <Trophy className="h-4 w-4" />
              <span className="hidden sm:inline">Leaderboard</span>
            </TabsTrigger>
            <TabsTrigger value="stats" className="flex items-center gap-2">
              <BarChart3 className="h-4 w-4" />
              <span className="hidden sm:inline">Rating Stats</span>
            </TabsTrigger>
            <TabsTrigger value="bots" className="flex items-center gap-2">
              <Bot className="h-4 w-4" />
              <span className="hidden sm:inline">Bots</span>
            </TabsTrigger>
          </TabsList>

          <TabsContent value="online">
            <Card>
              <CardHeader>
                <CardTitle className="flex items-center gap-2">
                  <Users className="h-5 w-5 text-green-500" />
                  Online Players
                </CardTitle>
                <CardDescription>
                  Players currently online and available to play
                </CardDescription>
              </CardHeader>
              <CardContent>
                <OnlinePlayersList
                  players={onlinePlayers}
                  isLoading={isLoading}
                  onRefresh={handleRefresh}
                />
              </CardContent>
            </Card>
          </TabsContent>

          <TabsContent value="leaderboard">
            <Card>
              <CardHeader>
                <CardTitle className="flex items-center gap-2">
                  <Trophy className="h-5 w-5 text-yellow-500" />
                  Leaderboard
                </CardTitle>
                <CardDescription>
                  Top players ranked by ELO rating
                </CardDescription>
              </CardHeader>
              <CardContent>
                <Leaderboard
                  entries={leaderboard}
                  isLoading={isLoading}
                />
              </CardContent>
            </Card>
          </TabsContent>

          <TabsContent value="stats">
            <Card>
              <CardHeader>
                <CardTitle className="flex items-center gap-2">
                  <BarChart3 className="h-5 w-5 text-blue-500" />
                  Rating Distribution
                </CardTitle>
                <CardDescription>
                  See where you stand compared to other players
                </CardDescription>
              </CardHeader>
              <CardContent>
                <RatingDistribution
                  data={ratingDistribution}
                  isLoading={isLoading}
                />
              </CardContent>
            </Card>
          </TabsContent>

          <TabsContent value="bots">
            <Card>
              <CardHeader>
                <CardTitle className="flex items-center gap-2">
                  <Bot className="h-5 w-5 text-purple-500" />
                  AI Opponents
                </CardTitle>
                <CardDescription>
                  Practice against computer opponents of varying skill levels
                </CardDescription>
              </CardHeader>
              <CardContent>
                <OnlineBotsList
                  bots={bots}
                  isLoading={isLoading}
                />
              </CardContent>
            </Card>
          </TabsContent>
        </Tabs>
      </div>
    </div>
  )
}
