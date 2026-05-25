import { useNavigate } from 'react-router-dom'
import { Tabs, TabsContent, TabsList, TabsTrigger } from '@/components/ui/tabs'
import { Badge } from '@/components/ui/badge'
import { Button } from '@/components/ui/button'
import { Toaster } from '@/components/ui/toaster'
import {
  DropdownMenu,
  DropdownMenuContent,
  DropdownMenuTrigger,
} from '@/components/ui/dropdown-menu'
import { Play, ChevronDown } from 'lucide-react'
import { useCorrespondenceGames } from '@/hooks/useCorrespondenceGames'
import { useActiveGames } from '@/hooks/useActiveGames'
import { usePlayActions } from '@/hooks/usePlayActions'
import { PlayMenuItems } from '@/components/PlayMenuItems'

import { GameLobby } from '@/components/home/GameLobby'
import { DailyPuzzlePreview } from '@/components/home/DailyPuzzlePreview'
import { GamesInPlay } from '@/components/home/GamesInPlay'
import { Footer } from '@/components/home/Footer'

export function HomePage() {
  const navigate = useNavigate()
  const { yourTurnGames, waitingGames, totalYourTurn: corrYourTurn } = useCorrespondenceGames()
  const { games: liveGames, yourTurnCount: liveYourTurn } = useActiveGames()

  const totalGamesInPlay = liveGames.length + yourTurnGames.length + waitingGames.length
  const totalYourTurn = corrYourTurn + liveYourTurn

  const handlePlayNext = () => {
    const firstYourTurnLive = liveGames.find(g => g.isYourTurn)
    const firstYourTurnCorr = yourTurnGames[0]
    if (firstYourTurnLive) {
      navigate(`/match/${firstYourTurnLive.matchId}/game/${firstYourTurnLive.gameId}`)
    } else if (firstYourTurnCorr) {
      navigate(`/match/${firstYourTurnCorr.matchId}/game/${firstYourTurnCorr.gameId}`)
    }
  }

  return (
    <div className="min-h-screen bg-background">
      <main className="container mx-auto px-4 py-8">
        <div className="max-w-5xl mx-auto">
          <Tabs defaultValue="lobby" className="w-full">
            <TabsList className="w-full justify-start mb-6">
              <TabsTrigger value="lobby">Lobby</TabsTrigger>
              <TabsTrigger value="your-games" className="gap-2">
                {totalGamesInPlay > 0 ? `Your games (${totalGamesInPlay})` : 'Your games'}
                {totalYourTurn > 0 && (
                  <Badge variant="destructive" className="animate-pulse">
                    {totalYourTurn}
                  </Badge>
                )}
              </TabsTrigger>
            </TabsList>

            <div className="grid grid-cols-1 lg:grid-cols-[1fr_300px] gap-6">
              <div>
                <TabsContent value="lobby" className="mt-0">
                  <GameLobby />
                </TabsContent>

                <TabsContent value="your-games" className="mt-0">
                  <GamesInPlay />
                </TabsContent>
              </div>

              <div className="hidden lg:block space-y-4">
                <PlayMenu>
                  <button className="w-full flex items-center justify-between gap-3 p-4 bg-primary text-primary-foreground hover:bg-primary/90 rounded-lg transition-colors text-left shadow-sm">
                    <div className="flex items-center gap-3">
                      <Play className="h-5 w-5" />
                      <div>
                        <div className="font-semibold">Play</div>
                        <div className="text-xs opacity-80">Start a new game</div>
                      </div>
                    </div>
                    <ChevronDown className="h-4 w-4 opacity-80" />
                  </button>
                </PlayMenu>

                {totalYourTurn > 0 && (
                  <button
                    onClick={handlePlayNext}
                    className="w-full flex items-center gap-3 p-3 bg-green-600 text-white hover:bg-green-700 rounded-lg transition-colors text-left"
                  >
                    <Play className="h-5 w-5" />
                    <div>
                      <div className="font-medium">Play Next</div>
                      <div className="text-xs opacity-80">
                        {totalYourTurn} game{totalYourTurn !== 1 ? 's' : ''} need your move
                      </div>
                    </div>
                  </button>
                )}

                <DailyPuzzlePreview />
              </div>
            </div>
          </Tabs>
        </div>
      </main>

      <Footer />

      <div className="fixed bottom-0 left-0 right-0 bg-background/95 backdrop-blur-sm border-t p-3 flex gap-2 lg:hidden z-40">
        <PlayMenu>
          <Button className="flex-1 w-full" size="lg">
            <Play className="h-5 w-5 mr-2" />
            Play
            <ChevronDown className="h-4 w-4 ml-2 opacity-80" />
          </Button>
        </PlayMenu>
        {totalYourTurn > 0 && (
          <Button
            onClick={handlePlayNext}
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

      <div className="h-20 lg:hidden" />

      <Toaster />
    </div>
  )
}

function PlayMenu({ children }: { children: React.ReactNode }) {
  const { onPlayChoice } = usePlayActions()
  return (
    <DropdownMenu>
      <DropdownMenuTrigger asChild>{children}</DropdownMenuTrigger>
      <DropdownMenuContent align="end" className="w-64">
        <PlayMenuItems onChoose={onPlayChoice} />
      </DropdownMenuContent>
    </DropdownMenu>
  )
}
