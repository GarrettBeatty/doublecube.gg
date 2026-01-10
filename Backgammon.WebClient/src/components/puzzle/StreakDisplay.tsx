import { Trophy, Flame } from 'lucide-react'
import { Badge } from '@/components/ui/badge'
import { PuzzleStreakInfo } from '@/types/puzzle.types'

interface StreakDisplayProps {
  streak: PuzzleStreakInfo | null
}

export function StreakDisplay({ streak }: StreakDisplayProps) {
  if (!streak) {
    return null
  }

  return (
    <div className="flex items-center gap-4">
      {/* Current streak */}
      <div className="flex items-center gap-2">
        <Flame className="h-5 w-5 text-orange-500" />
        <div className="text-right">
          <p className="text-xs text-muted-foreground">Streak</p>
          <p className="font-bold">{streak.currentStreak}</p>
        </div>
      </div>

      {/* Best streak */}
      {streak.bestStreak > 0 && (
        <div className="flex items-center gap-2">
          <Trophy className="h-5 w-5 text-yellow-500" />
          <div className="text-right">
            <p className="text-xs text-muted-foreground">Best</p>
            <p className="font-bold">{streak.bestStreak}</p>
          </div>
        </div>
      )}

      {/* Total solved badge */}
      {streak.totalSolved > 0 && (
        <Badge variant="secondary" className="ml-2">
          {streak.totalSolved} solved
        </Badge>
      )}
    </div>
  )
}
