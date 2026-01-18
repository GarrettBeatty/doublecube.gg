import { useNavigate } from 'react-router-dom'
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from '@/components/ui/card'
import { Badge } from '@/components/ui/badge'
import { RefreshCw, Play, Gamepad2, ArrowRight } from 'lucide-react'
import { Button } from '@/components/ui/button'
import { useActiveGames } from '@/hooks/useActiveGames'
import { useCorrespondenceGames } from '@/hooks/useCorrespondenceGames'
import { MiniBoardAdapter } from '@/components/board'
import { MiniPoint } from '@/types/home.types'
import { ActiveMatches } from './ActiveMatches'

interface GameCardProps {
  gameId: string
  matchId: string
  opponentName: string
  isYourTurn: boolean
  board?: MiniPoint[]
  whiteOnBar?: number
  redOnBar?: number
  whiteBornOff?: number
  redBornOff?: number
  dice?: number[]
  cubeValue?: number
  cubeOwner?: 'White' | 'Red' | 'Center'
  onPlay: (gameId: string, matchId: string) => void
}

function GameCard({
  gameId,
  matchId,
  opponentName,
  isYourTurn,
  board,
  whiteOnBar,
  redOnBar,
  whiteBornOff,
  redBornOff,
  dice,
  cubeValue,
  cubeOwner,
  onPlay,
}: GameCardProps) {
  return (
    <div
      className={`cursor-pointer transition-all hover:scale-[1.02] ${
        isYourTurn ? 'ring-2 ring-green-500 rounded-lg' : ''
      }`}
      onClick={() => onPlay(gameId, matchId)}
    >
      {/* Mini Board Preview */}
      <MiniBoardAdapter
        board={board}
        whiteOnBar={whiteOnBar}
        redOnBar={redOnBar}
        whiteBornOff={whiteBornOff}
        redBornOff={redBornOff}
        dice={dice}
        cubeValue={cubeValue}
        cubeOwner={cubeOwner}
        size={280}
      />

      {/* Game Info */}
      <div className="mt-2 text-center">
        <span className="text-sm font-medium">{opponentName}</span>
        {isYourTurn && (
          <Badge variant="default" className="ml-2 text-xs bg-green-600">
            Your turn
          </Badge>
        )}
      </div>
    </div>
  )
}

export function GamesInPlay() {
  const navigate = useNavigate()
  const { games: liveGames, isLoading: liveLoading, refresh: refreshLive, yourTurnCount: liveYourTurn } = useActiveGames()
  const { yourTurnGames, waitingGames, isLoading: corrLoading, refresh: refreshCorr, totalYourTurn: corrYourTurn } = useCorrespondenceGames()

  const handlePlayGame = (gameId: string, matchId: string) => {
    navigate(`/match/${matchId}/game/${gameId}`)
  }

  const handleRefresh = () => {
    refreshLive()
    refreshCorr()
  }

  const isLoading = liveLoading || corrLoading
  const totalYourTurn = liveYourTurn + corrYourTurn

  // Combine all games
  const allCorrespondenceGames = [...yourTurnGames, ...waitingGames]

  // Build unified list with board data
  type UnifiedGame = {
    gameId: string
    matchId: string
    opponentName: string
    isYourTurn: boolean
    board?: MiniPoint[]
    whiteOnBar?: number
    redOnBar?: number
    whiteBornOff?: number
    redBornOff?: number
    dice?: number[]
    cubeValue?: number
    cubeOwner?: 'White' | 'Red' | 'Center'
  }

  const unifiedGames: UnifiedGame[] = [
    // Live games (with board state)
    ...liveGames.map(g => ({
      gameId: g.gameId,
      matchId: g.matchId,
      opponentName: g.myColor === 'White' ? g.player2Name : g.player1Name,
      isYourTurn: g.isYourTurn,
      board: g.board,
      whiteOnBar: g.whiteCheckersOnBar,
      redOnBar: g.redCheckersOnBar,
      whiteBornOff: g.whiteBornOff,
      redBornOff: g.redBornOff,
      dice: g.dice,
      cubeValue: g.cubeValue,
      cubeOwner: g.cubeOwner,
    })),
    // Correspondence games
    ...allCorrespondenceGames.map(g => ({
      gameId: g.gameId,
      matchId: g.matchId,
      opponentName: g.opponentName,
      isYourTurn: g.isYourTurn,
    })),
  ]

  // Sort: your turn first
  const sortedGames = unifiedGames.sort((a, b) => {
    if (a.isYourTurn && !b.isYourTurn) return -1
    if (!a.isYourTurn && b.isYourTurn) return 1
    return 0
  })

  if (isLoading) {
    return (
      <Card>
        <CardHeader className="flex flex-row items-center justify-between space-y-0 pb-2">
          <div>
            <CardTitle>Active Games</CardTitle>
            <CardDescription>Your ongoing matches and games</CardDescription>
          </div>
        </CardHeader>
        <CardContent className="flex items-center justify-center py-12">
          <div className="text-muted-foreground">Loading games...</div>
        </CardContent>
      </Card>
    )
  }

  // Get the first game needing attention for "Play Next" button
  const firstYourTurnGame = sortedGames.find(g => g.isYourTurn)

  return (
    <div className="space-y-6">
      {/* Your Turn Summary Card - shown when games need attention */}
      {totalYourTurn > 0 && firstYourTurnGame && (
        <Card className="bg-green-50 dark:bg-green-950/30 border-green-200 dark:border-green-900">
          <CardContent className="p-4">
            <div className="flex items-center justify-between">
              <div className="flex items-center gap-3">
                <div className="h-10 w-10 rounded-full bg-green-600 flex items-center justify-center">
                  <Play className="h-5 w-5 text-white" />
                </div>
                <div>
                  <p className="font-semibold text-green-900 dark:text-green-100">
                    {totalYourTurn} game{totalYourTurn !== 1 ? 's' : ''} need your attention
                  </p>
                  <p className="text-sm text-green-700 dark:text-green-300">
                    vs {firstYourTurnGame.opponentName} is your oldest waiting game
                  </p>
                </div>
              </div>
              <Button
                onClick={() => handlePlayGame(firstYourTurnGame.gameId, firstYourTurnGame.matchId)}
                className="bg-green-600 hover:bg-green-700"
              >
                Play Next
                <ArrowRight className="h-4 w-4 ml-2" />
              </Button>
            </div>
          </CardContent>
        </Card>
      )}

      {/* Active Games (individual in-progress games) */}
      <Card>
        <CardHeader className="flex flex-row items-center justify-between space-y-0 pb-2">
          <div className="flex items-center gap-2">
            <CardTitle>Active Games</CardTitle>
            {totalYourTurn > 0 && (
              <Badge variant="destructive" className="animate-pulse">
                {totalYourTurn} your turn
              </Badge>
            )}
          </div>
          <Button variant="ghost" size="icon" onClick={handleRefresh} title="Refresh">
            <RefreshCw className="h-4 w-4" />
          </Button>
        </CardHeader>
        <CardContent className="space-y-4">
          {sortedGames.length === 0 ? (
            <div className="text-center py-12 space-y-4">
              <div className="mx-auto w-16 h-16 rounded-full bg-muted flex items-center justify-center">
                <Gamepad2 className="h-8 w-8 text-muted-foreground" />
              </div>
              <div>
                <p className="font-medium text-lg">No active games</p>
                <p className="text-sm text-muted-foreground mt-1">
                  Join a game from the Lobby tab or challenge a friend to get started!
                </p>
              </div>
            </div>
          ) : (
            <div className="grid grid-cols-2 gap-6">
              {sortedGames.map((game) => (
                <GameCard
                  key={game.gameId}
                  {...game}
                  onPlay={handlePlayGame}
                />
              ))}
            </div>
          )}
        </CardContent>
      </Card>

      {/* Active Matches (multi-game matches in progress) */}
      <ActiveMatches />
    </div>
  )
}
