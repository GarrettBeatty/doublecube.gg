import { memo } from 'react'
import { BOARD_CONFIG, BOARD_COLORS } from '@/lib/boardConstants'

interface GhostCheckerProps {
  x: number
  y: number
  color: 'white' | 'red'
}

export const GhostChecker = memo(function GhostChecker({
  x,
  y,
  color,
}: GhostCheckerProps) {
  const fillColor =
    color === 'white' ? BOARD_COLORS.checkerWhite : BOARD_COLORS.checkerRed
  const strokeColor =
    color === 'white'
      ? BOARD_COLORS.checkerWhiteStroke
      : BOARD_COLORS.checkerRedStroke

  return (
    <circle
      cx={x}
      cy={y}
      r={BOARD_CONFIG.checkerRadius}
      fill={fillColor}
      stroke={strokeColor}
      strokeWidth={2}
      opacity={0.7}
      style={{ pointerEvents: 'none' }}
    />
  )
})
