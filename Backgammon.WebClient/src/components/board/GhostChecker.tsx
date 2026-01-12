import { memo } from 'react'
import { BOARD_CONFIG } from '@/lib/boardConstants'
import { useThemeColors } from '@/stores/themeStore'

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
  const colors = useThemeColors()
  const fillColor =
    color === 'white' ? colors.checkerWhite : colors.checkerRed
  const strokeColor =
    color === 'white' ? colors.checkerWhiteStroke : colors.checkerRedStroke

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
