import React, { useState, useEffect } from 'react'
import { Clock, AlertTriangle } from 'lucide-react'
import { cn } from '@/lib/utils'

interface CorrespondenceDeadlineProps {
  turnDeadline: Date | string | null | undefined
  timePerMoveDays: number | null | undefined
  isYourTurn: boolean
}

export const CorrespondenceDeadline: React.FC<CorrespondenceDeadlineProps> = ({
  turnDeadline,
  timePerMoveDays,
  isYourTurn,
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
    <div
      className={cn(
        'flex items-center gap-2 px-3 py-1.5 rounded-lg text-sm font-medium',
        isYourTurn ? 'bg-primary/10' : 'bg-muted/50',
        isCritical && isYourTurn && 'bg-red-100 dark:bg-red-900/30 text-red-700 dark:text-red-400 animate-pulse',
        isUrgent && isYourTurn && 'bg-orange-100 dark:bg-orange-900/30 text-orange-700 dark:text-orange-400'
      )}
    >
      {isCritical && isYourTurn ? (
        <AlertTriangle className="h-4 w-4" />
      ) : (
        <Clock className="h-4 w-4 text-muted-foreground" />
      )}
      <span>
        {timeRemaining || `${timePerMoveDays}d/move`}
      </span>
      {isYourTurn && timeRemaining && (
        <span className="text-xs text-muted-foreground">
          (your turn)
        </span>
      )}
    </div>
  )
}
