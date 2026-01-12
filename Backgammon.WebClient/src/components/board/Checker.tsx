import { memo } from 'react'
import { BOARD_CONFIG } from '@/lib/boardConstants'
import { useThemeColors } from '@/stores/themeStore'

interface CheckerProps {
  cx: number
  cy: number
  color: 'white' | 'red'
  isSelected?: boolean
  isDraggable?: boolean
  onDragStart?: (e: React.MouseEvent | React.TouchEvent) => void
}

export const Checker = memo(function Checker({
  cx,
  cy,
  color,
  isSelected = false,
  isDraggable = false,
  onDragStart,
}: CheckerProps) {
  const colors = useThemeColors()
  const fillColor =
    color === 'white' ? colors.checkerWhite : colors.checkerRed
  const strokeColor =
    color === 'white' ? colors.checkerWhiteStroke : colors.checkerRedStroke

  return (
    <circle
      cx={cx}
      cy={cy}
      r={BOARD_CONFIG.checkerRadius}
      fill={fillColor}
      stroke={isSelected ? colors.highlightSelected : strokeColor}
      strokeWidth={isSelected ? 4 : 2}
      style={{ cursor: isDraggable ? 'grab' : 'default' }}
      onMouseDown={isDraggable ? onDragStart : undefined}
      onTouchStart={isDraggable ? onDragStart : undefined}
    />
  )
})
