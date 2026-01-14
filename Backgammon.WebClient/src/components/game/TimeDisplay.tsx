import React, { useState, useEffect } from 'react'
import { CheckerColor } from '@/types/game.types'
import { cn } from '@/lib/utils'

interface TimeDisplayProps {
  reserveSeconds: number | null
  isInDelay: boolean | null
  delayRemaining: number | null
  isActive: boolean
  color: CheckerColor
}

export const TimeDisplay: React.FC<TimeDisplayProps> = ({
  reserveSeconds,
  isInDelay,
  delayRemaining,
  isActive,
  color,
}) => {
  const colorName = color === CheckerColor.White ? 'White' : 'Red'
  const [displaySeconds, setDisplaySeconds] = useState(reserveSeconds || 0)
  const [displayDelaySeconds, setDisplayDelaySeconds] = useState(delayRemaining || 0)

  // Debug: Log props on every render
  console.log(`[TimeDisplay ${colorName}] Render - reserveSeconds: ${reserveSeconds}, isInDelay: ${isInDelay}, delayRemaining: ${delayRemaining}, isActive: ${isActive}`)

  // Sync display time with server updates
  useEffect(() => {
    console.log(`[TimeDisplay ${colorName}] Sync reserve effect - reserveSeconds: ${reserveSeconds}`)
    if (reserveSeconds !== null) {
      console.log(`[TimeDisplay ${colorName}] Setting displaySeconds to ${reserveSeconds}`)
      setDisplaySeconds(reserveSeconds)
    }
  }, [reserveSeconds, colorName])

  // Sync delay display with server updates
  useEffect(() => {
    console.log(`[TimeDisplay ${colorName}] Sync delay effect - delayRemaining: ${delayRemaining}`)
    if (delayRemaining !== null) {
      console.log(`[TimeDisplay ${colorName}] Setting displayDelaySeconds to ${delayRemaining}`)
      setDisplayDelaySeconds(delayRemaining)
    }
  }, [delayRemaining, colorName])

  // Local countdown for reserve time (only when NOT in delay)
  // Note: reserveSeconds intentionally excluded from deps - the sync effect handles server updates,
  // and including it here causes the interval to be recreated every server update (race condition)
  useEffect(() => {
    console.log(`[TimeDisplay ${colorName}] Reserve countdown effect - isActive: ${isActive}, isInDelay: ${isInDelay}, reserveSeconds: ${reserveSeconds}`)
    if (!isActive || isInDelay || reserveSeconds === null) {
      console.log(`[TimeDisplay ${colorName}] Reserve countdown - SKIPPING (isActive=${isActive}, isInDelay=${isInDelay}, reserveSeconds=${reserveSeconds})`)
      return
    }

    console.log(`[TimeDisplay ${colorName}] Reserve countdown - STARTING interval`)
    const interval = setInterval(() => {
      setDisplaySeconds((prev) => {
        const next = Math.max(0, prev - 1)
        console.log(`[TimeDisplay ${colorName}] Reserve countdown tick: ${prev} -> ${next}`)
        return next
      })
    }, 1000)

    return () => {
      console.log(`[TimeDisplay ${colorName}] Reserve countdown - CLEARING interval`)
      clearInterval(interval)
    }
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [isActive, isInDelay, colorName])

  // Local countdown for delay time (only when in delay)
  // Note: delayRemaining intentionally excluded from deps - same race condition issue as above
  useEffect(() => {
    console.log(`[TimeDisplay ${colorName}] Delay countdown effect - isActive: ${isActive}, isInDelay: ${isInDelay}, delayRemaining: ${delayRemaining}`)
    if (!isActive || !isInDelay || delayRemaining === null) {
      console.log(`[TimeDisplay ${colorName}] Delay countdown - SKIPPING (isActive=${isActive}, isInDelay=${isInDelay}, delayRemaining=${delayRemaining})`)
      return
    }

    console.log(`[TimeDisplay ${colorName}] Delay countdown - STARTING interval`)
    const interval = setInterval(() => {
      setDisplayDelaySeconds((prev) => {
        const next = Math.max(0, prev - 1)
        console.log(`[TimeDisplay ${colorName}] Delay countdown tick: ${prev} -> ${next}`)
        return next
      })
    }, 1000)

    return () => {
      console.log(`[TimeDisplay ${colorName}] Delay countdown - CLEARING interval`)
      clearInterval(interval)
    }
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [isActive, isInDelay, colorName])

  // Format time as MM:SS
  const formatTime = (seconds: number): string => {
    const mins = Math.floor(seconds / 60)
    const secs = Math.floor(seconds % 60)
    return `${mins}:${secs.toString().padStart(2, '0')}`
  }

  // Determine visual state
  const isLowTime = displaySeconds < 30 && displaySeconds > 10
  const isCriticalTime = displaySeconds <= 10

  return (
    <div className="flex flex-col items-center gap-1">
      <div
        className={cn(
          'relative flex items-center justify-center rounded-lg px-4 py-2 min-w-[100px] font-mono text-lg font-semibold transition-all',
          // Base colors
          color === CheckerColor.White ? 'bg-slate-100 text-slate-900' : 'bg-slate-800 text-slate-100',
          // Active state
          isActive && 'ring-2 ring-offset-2',
          isActive && color === CheckerColor.White && 'ring-blue-500',
          isActive && color === CheckerColor.Red && 'ring-red-500',
          // Low time warning
          isLowTime && 'bg-yellow-100 text-yellow-900 ring-yellow-500',
          // Critical time warning
          isCriticalTime && 'bg-red-100 text-red-900 ring-red-500 animate-pulse'
        )}
      >
        {formatTime(displaySeconds)}
      </div>

      {/* Delay indicator */}
      {isActive && isInDelay && displayDelaySeconds > 0 && (
        <div className="text-xs text-muted-foreground bg-primary/10 px-2 py-0.5 rounded">
          Delay ({Math.ceil(displayDelaySeconds)}s)
        </div>
      )}
    </div>
  )
}
