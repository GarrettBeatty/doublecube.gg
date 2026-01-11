import { useState } from 'react'
import { Tabs, TabsContent, TabsList, TabsTrigger } from '@/components/ui/tabs'
import { Badge } from '@/components/ui/badge'
import { Toaster } from '@/components/ui/toaster'
import { Plus, Bot, UserPlus } from 'lucide-react'
import { useSignalR } from '@/contexts/SignalRContext'
import { useToast } from '@/hooks/use-toast'
import { useCorrespondenceGames } from '@/hooks/useCorrespondenceGames'
import { useActiveGames } from '@/hooks/useActiveGames'

// Import all home components
import { GameLobby } from '@/components/home/GameLobby'
import { DailyPuzzlePreview } from '@/components/home/DailyPuzzlePreview'
import { CorrespondenceLobbies } from '@/components/home/CorrespondenceLobbies'
import { GamesInPlay } from '@/components/home/GamesInPlay'
import { FeaturedTournaments } from '@/components/home/FeaturedTournaments'
import { ActivityFeed } from '@/components/home/ActivityFeed'
import { RecentOpponents } from '@/components/home/RecentOpponents'
import { QuickPlayHero } from '@/components/home/QuickPlayHero'
import { Footer } from '@/components/home/Footer'

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

  return (
    <div className="min-h-screen bg-background">
      <main className="container mx-auto px-4 py-8">
        <div className="max-w-5xl mx-auto">
          <Tabs defaultValue="lobby" className="w-full">
            {/* Full-width tabs at top */}
            <TabsList className="w-full justify-start mb-6">
              <TabsTrigger value="lobby">
                Lobby
              </TabsTrigger>
              <TabsTrigger value="correspondence">
                Correspondence
              </TabsTrigger>
              <TabsTrigger value="in-play" className="gap-2">
                {totalGamesInPlay > 0 ? `${totalGamesInPlay} games in play` : 'My Games'}
                {totalYourTurn > 0 && (
                  <Badge variant="destructive" className="animate-pulse">
                    {totalYourTurn}
                  </Badge>
                )}
              </TabsTrigger>
            </TabsList>

            {/* Two column layout: Content + Action Sidebar */}
            <div className="grid grid-cols-1 lg:grid-cols-[1fr_300px] gap-6">
              {/* Main content area */}
              <div>
                <TabsContent value="lobby" className="mt-0">
                  <GameLobby onCreateGame={handleCreateGame} />
                </TabsContent>

                <TabsContent value="correspondence" className="mt-0">
                  <CorrespondenceLobbies />
                </TabsContent>

                <TabsContent value="in-play" className="mt-0">
                  <GamesInPlay />
                </TabsContent>
              </div>

              {/* Consolidated sidebar */}
              <div className="space-y-4">
                {/* Quick Actions */}
                <div className="space-y-2">
                  <button
                    onClick={handleCreateGame}
                    className="w-full flex items-center gap-3 p-3 bg-card hover:bg-accent border rounded-lg transition-colors text-left"
                  >
                    <Plus className="h-5 w-5 text-muted-foreground" />
                    <span className="font-medium">Create lobby game</span>
                  </button>
                  <button
                    onClick={handleChallengeFriend}
                    className="w-full flex items-center gap-3 p-3 bg-card hover:bg-accent border rounded-lg transition-colors text-left"
                  >
                    <UserPlus className="h-5 w-5 text-muted-foreground" />
                    <span className="font-medium">Challenge a friend</span>
                  </button>
                  <button
                    onClick={handlePlayVsComputer}
                    className="w-full flex items-center gap-3 p-3 bg-card hover:bg-accent border rounded-lg transition-colors text-left"
                  >
                    <Bot className="h-5 w-5 text-muted-foreground" />
                    <span className="font-medium">Play against computer</span>
                  </button>
                </div>

                {/* Daily Puzzle Preview */}
                <DailyPuzzlePreview />
              </div>
            </div>
          </Tabs>
        </div>
      </main>

      <Footer />

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
