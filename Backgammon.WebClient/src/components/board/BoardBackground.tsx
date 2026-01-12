import { memo } from 'react'
import { BOARD_CONFIG } from '@/lib/boardConstants'
import { useThemeColors } from '@/stores/themeStore'

export const BoardBackground = memo(function BoardBackground() {
  const colors = useThemeColors()
  return (
    <rect
      x={0}
      y={0}
      width={BOARD_CONFIG.viewBox.width}
      height={BOARD_CONFIG.viewBox.height}
      fill={colors.boardBackground}
      stroke={colors.boardBorder}
      strokeWidth={4}
    />
  )
})
