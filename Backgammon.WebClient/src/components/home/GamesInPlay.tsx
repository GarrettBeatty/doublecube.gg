import { useNavigate } from 'react-router-dom'
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from '@/components/ui/card'
import { Badge } from '@/components/ui/badge'
import { RefreshCw } from 'lucide-react'
import { Button } from '@/components/ui/button'
import { useActiveGames } from '@/hooks/useActiveGames'
import { useCorrespondenceGames } from '@/hooks/useCorrespondenceGames'
import { MiniBoardAdapter } from '@/components/board'
import { MiniPoint } from '@/types/home.types'

interface GameCardProps {
  gameId: string
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
  onPlay: (gameId: string) => void
}

function GameCard({
  gameId,
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
      onClick={() => onPlay(gameId)}
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

  const handlePlayGame = (gameId: string) => {
    navigate(`/game/${gameId}`)
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

  return (
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
          <div className="text-center py-8">
            <p className="text-muted-foreground">No active games</p>
            <p className="text-sm text-muted-foreground mt-2">
              Join a game from the Lobby or Correspondence tabs
            </p>
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
  )
}
