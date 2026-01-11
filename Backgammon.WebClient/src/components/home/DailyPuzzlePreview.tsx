import { useEffect, useState } from 'react'
import { useNavigate } from 'react-router-dom'
import { Card, CardContent } from '@/components/ui/card'
import { Badge } from '@/components/ui/badge'
import { Flame, Check, Loader2 } from 'lucide-react'
import { useSignalR } from '@/contexts/SignalRContext'
import { HubMethods } from '@/types/signalr.types'
import { DailyPuzzle, PuzzleStreakInfo } from '@/types/puzzle.types'
import { MiniBoardPreview } from './MiniBoardPreview'
import { MiniPoint } from '@/types/home.types'

export function DailyPuzzlePreview() {
  const navigate = useNavigate()
  const { invoke, isConnected } = useSignalR()
  const [puzzle, setPuzzle] = useState<DailyPuzzle | null>(null)
  const [streak, setStreak] = useState<PuzzleStreakInfo | null>(null)
  const [isLoading, setIsLoading] = useState(true)

  useEffect(() => {
    const loadPuzzlePreview = async () => {
      if (!isConnected) return

      try {
        const [puzzleData, streakData] = await Promise.all([
          invoke<DailyPuzzle>(HubMethods.GetDailyPuzzle),
          invoke<PuzzleStreakInfo>(HubMethods.GetPuzzleStreak),
        ])
        setPuzzle(puzzleData)
        setStreak(streakData)
      } catch (err) {
        console.error('Failed to load puzzle preview:', err)
      } finally {
        setIsLoading(false)
      }
    }

    loadPuzzlePreview()
  }, [isConnected, invoke])

  const handleClick = () => {
    navigate('/puzzle')
  }

  // Convert puzzle board state to MiniPoint format
  const boardToMiniPoints = (puzzle: DailyPuzzle): MiniPoint[] => {
    return puzzle.boardState.map(p => ({
      position: p.position,
      color: p.color as 'White' | 'Red' | null,
      count: p.count,
    }))
  }

  if (isLoading) {
    return (
      <Card className="cursor-pointer hover:bg-accent/50 transition-colors" onClick={handleClick}>
        <CardContent className="p-4">
          <div className="flex items-center justify-center py-8">
            <Loader2 className="h-6 w-6 animate-spin text-muted-foreground" />
          </div>
        </CardContent>
      </Card>
    )
  }

  if (!puzzle) {
    return (
      <Card className="cursor-pointer hover:bg-accent/50 transition-colors" onClick={handleClick}>
        <CardContent className="p-4 text-center">
          <p className="text-sm text-muted-foreground">No puzzle available</p>
        </CardContent>
      </Card>
    )
  }

  return (
    <Card
      className="cursor-pointer hover:bg-accent/50 transition-colors overflow-hidden"
      onClick={handleClick}
    >
      <CardContent className="p-0">
        {/* Mini Board Preview */}
        <div className="flex justify-center bg-muted/30 p-3">
          <MiniBoardPreview
            board={boardToMiniPoints(puzzle)}
            whiteOnBar={puzzle.whiteCheckersOnBar}
            redOnBar={puzzle.redCheckersOnBar}
            whiteBornOff={puzzle.whiteBornOff}
            redBornOff={puzzle.redBornOff}
            dice={puzzle.dice}
            size={240}
          />
        </div>

        {/* Info */}
        <div className="p-3 space-y-2">
          <div className="flex items-center justify-between">
            <span className="font-semibold text-sm">Daily Puzzle</span>
            <div className="flex items-center gap-1.5">
              {puzzle.alreadySolved && (
                <Badge variant="default" className="bg-green-600 text-xs">
                  <Check className="h-3 w-3 mr-0.5" />
                  Solved
                </Badge>
              )}
              {streak && streak.currentStreak > 0 && (
                <div className="flex items-center gap-0.5 text-orange-500">
                  <Flame className="h-3.5 w-3.5" />
                  <span className="text-xs font-medium">{streak.currentStreak}</span>
                </div>
              )}
            </div>
          </div>
          <p className="text-xs text-muted-foreground">
            {puzzle.currentPlayer} to play {puzzle.dice[0]}-{puzzle.dice[1]}
          </p>
        </div>
      </CardContent>
    </Card>
  )
}
