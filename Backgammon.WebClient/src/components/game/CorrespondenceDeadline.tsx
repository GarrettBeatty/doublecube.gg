import React, { useState, useEffect } from 'react'
import { Clock, AlertTriangle } from 'lucide-react'
import { CheckerColor } from '@/types/game.types'
import { cn } from '@/lib/utils'

interface CorrespondenceDeadlineProps {
  turnDeadline: Date | string | null | undefined
  timePerMoveDays: number | null | undefined
  isActive: boolean
  color: CheckerColor
}

export const CorrespondenceDeadline: React.FC<CorrespondenceDeadlineProps> = ({
  turnDeadline,
  timePerMoveDays,
  isActive,
  color,
}) => {
  const [timeRemaining, setTimeRemaining] = useState<string>('')
  const [isUrgent, setIsUrgent] = useState(false)
  const [isCritical, setIsCritical] = useState(false)

  useEffect(() => {
    const calculateTimeRemaining = () => {
      if (!turnDeadline) {
        setTimeRemaining('')
        return
      }

      const deadline = typeof turnDeadline === 'string' ? new Date(turnDeadline) : turnDeadline
      const now = new Date()
      const diffMs = deadline.getTime() - now.getTime()

      if (diffMs <= 0) {
        setTimeRemaining('Expired')
        setIsCritical(true)
        setIsUrgent(false)
        return
      }

      const diffDays = Math.floor(diffMs / (1000 * 60 * 60 * 24))
      const diffHours = Math.floor((diffMs % (1000 * 60 * 60 * 24)) / (1000 * 60 * 60))
      const diffMinutes = Math.floor((diffMs % (1000 * 60 * 60)) / (1000 * 60))

      // Set urgency levels
      const critical = diffDays === 0 && diffHours < 6
      setIsCritical(critical)
      setIsUrgent(diffDays <= 1 && !critical)

      if (diffDays > 0) {
        setTimeRemaining(`${diffDays}d ${diffHours}h`)
      } else if (diffHours > 0) {
        setTimeRemaining(`${diffHours}h ${diffMinutes}m`)
      } else {
        setTimeRemaining(`${diffMinutes}m`)
      }
    }

    calculateTimeRemaining()
    const interval = setInterval(calculateTimeRemaining, 60000) // Update every minute

    return () => clearInterval(interval)
  }, [turnDeadline])

  if (!turnDeadline && !timePerMoveDays) {
    return null
  }

  return (
    <div className="flex flex-col items-center gap-1">
      <div
        className={cn(
          'flex items-center justify-center gap-2 rounded-lg px-4 py-2 min-w-[100px] text-sm font-semibold transition-all',
          // Base colors matching TimeDisplay
          color === CheckerColor.White ? 'bg-slate-100 text-slate-900' : 'bg-slate-800 text-slate-100',
          // Active state
          isActive && 'ring-2 ring-offset-2',
          isActive && color === CheckerColor.White && 'ring-blue-500',
          isActive && color === CheckerColor.Red && 'ring-red-500',
          // Urgency overrides (only when active)
          isCritical && isActive && 'bg-red-100 text-red-900 ring-red-500 animate-pulse',
          isUrgent && isActive && 'bg-orange-100 text-orange-900 ring-orange-500'
        )}
      >
        {isCritical && isActive ? (
          <AlertTriangle className="h-4 w-4" />
        ) : (
          <Clock className="h-4 w-4 opacity-60" />
        )}
        <span>
          {timeRemaining || `${timePerMoveDays}d/move`}
        </span>
      </div>
    </div>
  )
}
