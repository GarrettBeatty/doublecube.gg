import { memo } from 'react'
import { BOARD_CONFIG, BOARD_COLORS } from '@/lib/boardConstants'

export const BoardBackground = memo(function BoardBackground() {
  return (
    <rect
      x={0}
      y={0}
      width={BOARD_CONFIG.viewBox.width}
      height={BOARD_CONFIG.viewBox.height}
      fill={BOARD_COLORS.boardBackground}
      stroke={BOARD_COLORS.boardBorder}
      strokeWidth={4}
    />
  )
})
