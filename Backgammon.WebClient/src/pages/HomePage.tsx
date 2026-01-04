import { useState } from 'react'
import { Tabs, TabsContent, TabsList, TabsTrigger } from '@/components/ui/tabs'
import { Button } from '@/components/ui/button'
import { Badge } from '@/components/ui/badge'
import { Toaster } from '@/components/ui/toaster'
import { Plus, UserPlus, Bot } from 'lucide-react'
import { useSignalR } from '@/contexts/SignalRContext'
import { useToast } from '@/hooks/use-toast'

// Import all home components
import { QuickPlayCard } from '@/components/home/QuickPlayCard'
import { ActiveGamesList } from '@/components/home/ActiveGamesList'
import { GameLobby } from '@/components/home/GameLobby'
import { OnlineFriends } from '@/components/home/OnlineFriends'
import { DailyPuzzle } from '@/components/home/DailyPuzzle'
import { CorrespondenceGames } from '@/components/home/CorrespondenceGames'
import { FeaturedTournaments } from '@/components/home/FeaturedTournaments'
import { ActivityFeed } from '@/components/home/ActivityFeed'
import { RecentGames } from '@/components/home/RecentGames'

// Import modals
import { CreateMatchModal } from '@/components/modals/CreateMatchModal'
import { FriendsDialog } from '@/components/friends/FriendsDialog'

export function HomePage() {
  const { isConnected } = useSignalR()
  const { toast } = useToast()

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
        <div className="grid grid-cols-1 lg:grid-cols-12 gap-6">

          {/* Left Column - Actions (col-span-3) */}
          <div className="lg:col-span-3 space-y-6">
            {/* Action Buttons */}
            <div className="space-y-3">
              <Button
                className="w-full flex items-center gap-2"
                size="lg"
                onClick={handleCreateGame}
              >
                <Plus className="h-5 w-5" />
                Create a Game Lobby
              </Button>
              <Button
                className="w-full flex items-center gap-2"
                variant="outline"
                size="lg"
                onClick={handleChallengeFriend}
              >
                <UserPlus className="h-5 w-5" />
                Challenge a Friend
              </Button>
              <Button
                className="w-full flex items-center gap-2"
                variant="outline"
                size="lg"
                onClick={handlePlayVsComputer}
              >
                <Bot className="h-5 w-5" />
                Play Against Computer
              </Button>
            </div>

            <DailyPuzzle />
            <OnlineFriends onChallengeClick={handleChallengeFromFriendsList} />
          </div>

          {/* Center Column - Tabbed Content (col-span-6) */}
          <div className="lg:col-span-6">
            <Tabs defaultValue="quick-pairing" className="w-full">
              <TabsList className="w-full">
                <TabsTrigger value="quick-pairing" className="flex-1">
                  Quick Pairing
                </TabsTrigger>
                <TabsTrigger value="lobby" className="flex-1">
                  Lobby
                </TabsTrigger>
                <TabsTrigger value="correspondence" className="flex-1 relative">
                  Correspondence
                  <Badge variant="destructive" className="ml-2 h-5 w-5 p-0 flex items-center justify-center text-xs">
                    2
                  </Badge>
                </TabsTrigger>
              </TabsList>

              <TabsContent value="quick-pairing" className="mt-6">
                <div className="space-y-6">
                  <QuickPlayCard />
                  <ActiveGamesList />
                  <RecentGames />
                </div>
              </TabsContent>

              <TabsContent value="lobby" className="mt-6">
                <GameLobby />
              </TabsContent>

              <TabsContent value="correspondence" className="mt-6">
                <CorrespondenceGames />
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
