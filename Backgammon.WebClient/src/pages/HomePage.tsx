import { useState } from 'react'
import { Tabs, TabsContent, TabsList, TabsTrigger } from '@/components/ui/tabs'
import { Badge } from '@/components/ui/badge'
import { Toaster } from '@/components/ui/toaster'
import { useSignalR } from '@/contexts/SignalRContext'
import { useToast } from '@/hooks/use-toast'
import { useCorrespondenceGames } from '@/hooks/useCorrespondenceGames'
import { useActiveGames } from '@/hooks/useActiveGames'

// Import all home components
import { GameLobby } from '@/components/home/GameLobby'
import { OnlineFriends } from '@/components/home/OnlineFriends'
import { DailyPuzzle } from '@/components/home/DailyPuzzle'
import { CorrespondenceLobbies } from '@/components/home/CorrespondenceLobbies'
import { GamesInPlay } from '@/components/home/GamesInPlay'
import { FeaturedTournaments } from '@/components/home/FeaturedTournaments'
import { ActivityFeed } from '@/components/home/ActivityFeed'
import { RecentOpponents } from '@/components/home/RecentOpponents'
import { QuickPlayHero } from '@/components/home/QuickPlayHero'

// Import modals
import { CreateMatchModal } from '@/components/modals/CreateMatchModal'
import { FriendsDialog } from '@/components/friends/FriendsDialog'

export function HomePage() {
  const { isConnected } = useSignalR()
  const { toast } = useToast()
  const { yourTurnGames, waitingGames, totalYourTurn: corrYourTurn } = useCorrespondenceGames()
  const { games: liveGames, yourTurnCount: liveYourTurn } = useActiveGames()

  // Calculate total games in play
  const totalGamesInPlay = liveGames.length + yourTurnGames.length + waitingGames.length
  const totalYourTurn = corrYourTurn + liveYourTurn

  // Modal state
  const [showCreateMatchModal, setShowCreateMatchModal] = useState(false)
  const [matchModalType, setMatchModalType] = useState<'AI' | 'OpenLobby'>('OpenLobby')
  const [showFriendsDialog, setShowFriendsDialog] = useState(false)

  // Action handlers
  const handleCreateGame = () => {
    if (!isConnected) {
      toast({
        title: 'Not connected',
        description: 'Please wait for connection to server...',
        variant: 'destructive',
      })
      return
    }
    setMatchModalType('OpenLobby')
    setShowCreateMatchModal(true)
  }

  const handleChallengeFriend = () => {
    setShowFriendsDialog(true)
  }

  const handlePlayVsComputer = () => {
    if (!isConnected) {
      toast({
        title: 'Not connected',
        description: 'Please wait for connection to server...',
        variant: 'destructive',
      })
      return
    }
    setMatchModalType('AI')
    setShowCreateMatchModal(true)
  }

  const handleChallengeFromFriendsList = () => {
    // For now, open the FriendsDialog which has challenge functionality
    // TODO: Pre-select the friend in FriendsDialog or add direct challenge support
    setShowFriendsDialog(true)
  }

  return (
    <div className="min-h-screen bg-background">
      <main className="container mx-auto px-4 py-8">
        {/* Hero Quick-Play Section */}
        <QuickPlayHero
          onCreateGame={handleCreateGame}
          onPlayComputer={handlePlayVsComputer}
          onChallengeFriend={handleChallengeFriend}
        />

        <div className="grid grid-cols-1 lg:grid-cols-12 gap-6">

          {/* Left Column - Social & Engagement (col-span-3) */}
          <div className="lg:col-span-3 space-y-6">
            <DailyPuzzle />
            <OnlineFriends onChallengeClick={handleChallengeFromFriendsList} />
            <RecentOpponents onChallengeClick={() => {
              // Open friends dialog to initiate challenge
              // TODO: Add direct challenge support for recent opponents
              setShowFriendsDialog(true)
            }} />
          </div>

          {/* Center Column - Tabbed Content (col-span-6) */}
          <div className="lg:col-span-6">
            <Tabs defaultValue="lobby" className="w-full">
              <TabsList className="w-full">
                <TabsTrigger value="lobby" className="flex-1">
                  Lobby
                </TabsTrigger>
                <TabsTrigger value="correspondence" className="flex-1">
                  Correspondence
                </TabsTrigger>
                <TabsTrigger value="in-play" className="flex-1 gap-2">
                  {totalGamesInPlay > 0 ? `${totalGamesInPlay} Games` : 'My Games'}
                  {totalYourTurn > 0 && (
                    <Badge variant="destructive" className="animate-pulse">
                      {totalYourTurn}
                    </Badge>
                  )}
                </TabsTrigger>
              </TabsList>

              <TabsContent value="lobby" className="mt-6">
                <GameLobby onCreateGame={handleCreateGame} />
              </TabsContent>

              <TabsContent value="correspondence" className="mt-6">
                <CorrespondenceLobbies />
              </TabsContent>

              <TabsContent value="in-play" className="mt-6">
                <GamesInPlay />
              </TabsContent>
            </Tabs>
          </div>

          {/* Right Column - Discovery & Engagement (col-span-3) */}
          <div className="lg:col-span-3 space-y-6">
            <FeaturedTournaments />
            <ActivityFeed />
          </div>

        </div>
      </main>

      {/* Modals */}
      <CreateMatchModal
        isOpen={showCreateMatchModal}
        onClose={() => setShowCreateMatchModal(false)}
        defaultOpponentType={matchModalType}
      />

      <FriendsDialog
        isOpen={showFriendsDialog}
        onClose={() => setShowFriendsDialog(false)}
      />

      <Toaster />
    </div>
  )
}
