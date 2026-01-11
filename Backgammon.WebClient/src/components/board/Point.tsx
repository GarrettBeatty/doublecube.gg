import { memo } from 'react'
import { BOARD_CONFIG, BOARD_COLORS, POINT_COORDS } from '@/lib/boardConstants'
import { PointTriangle } from './PointTriangle'
import { CheckerStack } from './CheckerStack'
import { HighlightType } from './board.types'

// Highlight colors mapping
const HIGHLIGHT_COLORS: Record<HighlightType, string> = {
  source: BOARD_COLORS.highlightSource,
  selected: BOARD_COLORS.highlightSelected,
  destination: BOARD_COLORS.highlightDest,
  capture: BOARD_COLORS.highlightCapture,
  combined: 'hsla(280 70% 50% / 0.6)', // Purple for combined moves
  analysis: BOARD_COLORS.highlightAnalysis,
}

interface PointProps {
  pointNum: number
  color: 'white' | 'red' | null
  count: number
  highlight?: HighlightType | null
  showNumber?: boolean
  isDraggable?: boolean
  onClick?: () => void
  onCheckerDragStart?: (e: React.MouseEvent | React.TouchEvent) => void
}

export const Point = memo(function Point({
  pointNum,
  color,
  count,
  highlight,
  showNumber = false,
  isDraggable = false,
  onClick,
  onCheckerDragStart,
}: PointProps) {
  const coords = POINT_COORDS[pointNum]
  if (!coords) return null

  const { x, y, direction } = coords
  const isTop = direction === 1

  // Determine fill color
  const baseFillColor =
    pointNum % 2 === 0 ? BOARD_COLORS.pointLight : BOARD_COLORS.pointDark
  const fillColor = highlight ? HIGHLIGHT_COLORS[highlight] : baseFillColor

  // Center X of the point
  const centerX = x + BOARD_CONFIG.pointWidth / 2

  return (
    <g>
      {/* Triangle */}
      <PointTriangle
        x={x}
        y={y}
        direction={direction as 1 | -1}
        fillColor={fillColor}
        onClick={onClick}
      />

      {/* Checkers */}
      {color && count > 0 && (
        <CheckerStack
          baseX={centerX}
          baseY={y}
          direction={direction as 1 | -1}
          color={color}
          count={count}
          draggableTopChecker={isDraggable}
          onTopCheckerDragStart={onCheckerDragStart}
        />
      )}

      {/* Point number */}
      {showNumber && (
        <text
          x={centerX}
          y={isTop ? y - 8 : y + 15}
          textAnchor="middle"
          fill={BOARD_COLORS.textLight}
          fontSize={12}
          style={{ pointerEvents: 'none' }}
        >
          {pointNum}
        </text>
      )}
    </g>
  )
})
