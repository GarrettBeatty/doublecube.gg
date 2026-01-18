import { useState } from 'react'
import { useNavigate } from 'react-router-dom'
import { Tabs, TabsContent, TabsList, TabsTrigger } from '@/components/ui/tabs'
import { Badge } from '@/components/ui/badge'
import { Button } from '@/components/ui/button'
import { Toaster } from '@/components/ui/toaster'
import { Plus, Bot, UserPlus, Play } from 'lucide-react'
import { useSignalR } from '@/contexts/SignalRContext'
import { useToast } from '@/hooks/use-toast'
import { useCorrespondenceGames } from '@/hooks/useCorrespondenceGames'
import { useActiveGames } from '@/hooks/useActiveGames'

// Import all home components
import { GameLobby } from '@/components/home/GameLobby'
import { DailyPuzzlePreview } from '@/components/home/DailyPuzzlePreview'
import { CorrespondenceLobbies } from '@/components/home/CorrespondenceLobbies'
import { GamesInPlay } from '@/components/home/GamesInPlay'
import { Footer } from '@/components/home/Footer'

// Import modals
import { CreateMatchModal } from '@/components/modals/CreateMatchModal'
import { FriendsDialog } from '@/components/friends/FriendsDialog'

export function HomePage() {
  const navigate = useNavigate()
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

              {/* Consolidated sidebar - hidden on mobile, shown on lg+ */}
              <div className="hidden lg:block space-y-4">
                {/* Quick Actions */}
                <div className="space-y-2">
                  {/* Primary CTA - Create lobby game */}
                  <button
                    onClick={handleCreateGame}
                    className="w-full flex items-center gap-3 p-4 bg-primary text-primary-foreground hover:bg-primary/90 rounded-lg transition-colors text-left shadow-sm"
                  >
                    <Plus className="h-5 w-5" />
                    <div>
                      <div className="font-semibold">Create lobby game</div>
                      <div className="text-xs opacity-80">Start a match and wait for opponents</div>
                    </div>
                  </button>

                  {/* Secondary actions */}
                  <button
                    onClick={handleChallengeFriend}
                    className="w-full flex items-center gap-3 p-3 bg-card hover:bg-accent border rounded-lg transition-colors text-left"
                  >
                    <UserPlus className="h-5 w-5 text-muted-foreground" />
                    <div>
                      <div className="font-medium">Challenge a friend</div>
                      <div className="text-xs text-muted-foreground">Invite someone you know</div>
                    </div>
                  </button>
                  <button
                    onClick={handlePlayVsComputer}
                    className="w-full flex items-center gap-3 p-3 bg-card hover:bg-accent border rounded-lg transition-colors text-left"
                  >
                    <Bot className="h-5 w-5 text-muted-foreground" />
                    <div>
                      <div className="font-medium">Play against computer</div>
                      <div className="text-xs text-muted-foreground">Practice or play offline</div>
                    </div>
                  </button>
                </div>

                {/* Your Turn Quick Action - shown when games need attention */}
                {totalYourTurn > 0 && (
                  <button
                    onClick={() => {
                      // Navigate to the first game needing attention
                      const firstYourTurnLive = liveGames.find(g => g.isYourTurn)
                      const firstYourTurnCorr = yourTurnGames[0]
                      if (firstYourTurnLive) {
                        navigate(`/match/${firstYourTurnLive.matchId}/game/${firstYourTurnLive.gameId}`)
                      } else if (firstYourTurnCorr) {
                        navigate(`/match/${firstYourTurnCorr.matchId}/game/${firstYourTurnCorr.gameId}`)
                      }
                    }}
                    className="w-full flex items-center gap-3 p-3 bg-green-600 text-white hover:bg-green-700 rounded-lg transition-colors text-left"
                  >
                    <Play className="h-5 w-5" />
                    <div>
                      <div className="font-medium">Play Next</div>
                      <div className="text-xs opacity-80">{totalYourTurn} game{totalYourTurn !== 1 ? 's' : ''} need your move</div>
                    </div>
                  </button>
                )}

                {/* Daily Puzzle Preview */}
                <DailyPuzzlePreview />
              </div>
            </div>
          </Tabs>
        </div>
      </main>

      <Footer />

      {/* Mobile Bottom Action Bar - shown only on mobile (below lg breakpoint) */}
      <div className="fixed bottom-0 left-0 right-0 bg-background/95 backdrop-blur-sm border-t p-3 flex gap-2 lg:hidden z-40">
        <Button
          onClick={handleCreateGame}
          className="flex-1"
          size="lg"
        >
          <Plus className="h-5 w-5 mr-2" />
          Create Game
        </Button>
        <Button
          onClick={handleChallengeFriend}
          variant="outline"
          size="lg"
        >
          <UserPlus className="h-5 w-5" />
        </Button>
        <Button
          onClick={handlePlayVsComputer}
          variant="outline"
          size="lg"
        >
          <Bot className="h-5 w-5" />
        </Button>
        {totalYourTurn > 0 && (
          <Button
            onClick={() => {
              const firstYourTurnLive = liveGames.find(g => g.isYourTurn)
              const firstYourTurnCorr = yourTurnGames[0]
              if (firstYourTurnLive) {
                navigate(`/match/${firstYourTurnLive.matchId}/game/${firstYourTurnLive.gameId}`)
              } else if (firstYourTurnCorr) {
                navigate(`/match/${firstYourTurnCorr.matchId}/game/${firstYourTurnCorr.gameId}`)
              }
            }}
            size="lg"
            className="bg-green-600 hover:bg-green-700 relative"
          >
            <Play className="h-5 w-5" />
            <Badge
              variant="destructive"
              className="absolute -top-2 -right-2 h-5 w-5 p-0 flex items-center justify-center text-xs"
            >
              {totalYourTurn}
            </Badge>
          </Button>
        )}
      </div>

      {/* Add bottom padding on mobile to account for fixed bottom bar */}
      <div className="h-20 lg:hidden" />

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
