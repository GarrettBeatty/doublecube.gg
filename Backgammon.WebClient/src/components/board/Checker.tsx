import { memo } from 'react'
import { BOARD_CONFIG, BOARD_COLORS } from '@/lib/boardConstants'

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
  const fillColor =
    color === 'white' ? BOARD_COLORS.checkerWhite : BOARD_COLORS.checkerRed
  const strokeColor =
    color === 'white'
      ? BOARD_COLORS.checkerWhiteStroke
      : BOARD_COLORS.checkerRedStroke

  return (
    <circle
      cx={cx}
      cy={cy}
      r={BOARD_CONFIG.checkerRadius}
      fill={fillColor}
      stroke={isSelected ? BOARD_COLORS.highlightSelected : strokeColor}
      strokeWidth={isSelected ? 4 : 2}
      style={{ cursor: isDraggable ? 'grab' : 'default' }}
      onMouseDown={isDraggable ? onDragStart : undefined}
      onTouchStart={isDraggable ? onDragStart : undefined}
    />
  )
})
