import { memo } from 'react'
import { BOARD_CONFIG } from '@/lib/boardConstants'

interface PointTriangleProps {
  x: number
  y: number
  direction: 1 | -1 // 1 = points down (top row), -1 = points up (bottom row)
  fillColor: string
  onClick?: () => void
}

export const PointTriangle = memo(function PointTriangle({
  x,
  y,
  direction,
  fillColor,
  onClick,
}: PointTriangleProps) {
  const width = BOARD_CONFIG.pointWidth
  const height = BOARD_CONFIG.pointHeight * direction

  const points = `${x},${y} ${x + width / 2},${y + height} ${x + width},${y}`

  return (
    <polygon
      points={points}
      fill={fillColor}
      onClick={onClick}
      style={onClick ? { cursor: 'pointer' } : undefined}
    />
  )
})
