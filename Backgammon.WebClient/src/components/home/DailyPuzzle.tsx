import { useEffect, useState } from 'react'
import { useNavigate } from 'react-router-dom'
import {
  Card,
  CardContent,
  CardDescription,
  CardHeader,
  CardTitle,
} from '@/components/ui/card'
import { Button } from '@/components/ui/button'
import { Badge } from '@/components/ui/badge'
import { Brain, Trophy, Flame, Check, Loader2 } from 'lucide-react'
import { useSignalR } from '@/contexts/SignalRContext'
import { HubMethods } from '@/types/signalr.types'
import { DailyPuzzle as DailyPuzzleType, PuzzleStreakInfo } from '@/types/puzzle.types'

export function DailyPuzzle() {
  const navigate = useNavigate()
  const { invoke, isConnected } = useSignalR()
  const [puzzle, setPuzzle] = useState<DailyPuzzleType | null>(null)
  const [streak, setStreak] = useState<PuzzleStreakInfo | null>(null)
  const [isLoading, setIsLoading] = useState(true)

  useEffect(() => {
    const loadPuzzlePreview = async () => {
      if (!isConnected) return

      try {
        const [puzzleData, streakData] = await Promise.all([
          invoke<DailyPuzzleType>(HubMethods.GetDailyPuzzle),
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

  const handleSolvePuzzle = () => {
    navigate('/puzzle')
  }

  return (
    <Card>
      <CardHeader>
        <CardTitle className="flex items-center gap-2">
          <Brain className="h-5 w-5 text-purple-500" />
          Daily Puzzle
        </CardTitle>
        <CardDescription>Improve your skills with today's position</CardDescription>
      </CardHeader>
      <CardContent className="space-y-4">
        <div className="relative bg-gradient-to-br from-purple-500/10 to-blue-500/10 rounded-lg p-6 border-2 border-dashed border-purple-500/30">
          {isLoading ? (
            <div className="flex items-center justify-center py-4">
              <Loader2 className="h-6 w-6 animate-spin text-purple-500" />
            </div>
          ) : puzzle ? (
            <div className="text-center space-y-2">
              <div className="flex items-center justify-center gap-2 mb-2">
                <Badge variant="secondary">Checker Play</Badge>
                {puzzle.alreadySolved && (
                  <Badge variant="default" className="bg-green-600">
                    <Check className="h-3 w-3 mr-1" />
                    Solved
                  </Badge>
                )}
              </div>
              <p className="text-sm text-muted-foreground">
                {puzzle.currentPlayer} rolled {puzzle.dice[0]}-{puzzle.dice[1]}.
                What's the best play?
              </p>
              <p className="text-xs text-muted-foreground mt-2">
                {puzzle.alreadySolved
                  ? `Solved in ${puzzle.attemptsToday} attempt${puzzle.attemptsToday !== 1 ? 's' : ''}`
                  : puzzle.attemptsToday > 0
                  ? `${puzzle.attemptsToday} attempt${puzzle.attemptsToday !== 1 ? 's' : ''} today`
                  : 'Not attempted yet'}
              </p>
            </div>
          ) : (
            <div className="text-center space-y-2">
              <p className="text-sm text-muted-foreground">
                No puzzle available today
              </p>
              <p className="text-xs text-muted-foreground">
                Check back later!
              </p>
            </div>
          )}
        </div>

        <div className="flex items-center justify-between">
          <div className="text-sm">
            <p className="text-muted-foreground">Your streak</p>
            <div className="flex items-center gap-3">
              {streak && streak.currentStreak > 0 ? (
                <>
                  <div className="flex items-center gap-1">
                    <Flame className="h-4 w-4 text-orange-500" />
                    <span className="font-semibold">{streak.currentStreak} days</span>
                  </div>
                  {streak.bestStreak > streak.currentStreak && (
                    <div className="flex items-center gap-1 text-xs text-muted-foreground">
                      <Trophy className="h-3 w-3 text-yellow-500" />
                      <span>Best: {streak.bestStreak}</span>
                    </div>
                  )}
                </>
              ) : (
                <span className="text-muted-foreground">Start your streak!</span>
              )}
            </div>
          </div>
          <Button onClick={handleSolvePuzzle} disabled={!puzzle}>
            {puzzle?.alreadySolved ? 'View Solution' : 'Solve Puzzle'}
          </Button>
        </div>
      </CardContent>
    </Card>
  )
}
