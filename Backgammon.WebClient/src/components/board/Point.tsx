import { memo, useMemo } from 'react'
import { BOARD_CONFIG, POINT_COORDS } from '@/lib/boardConstants'
import { useThemeColors } from '@/stores/themeStore'
import { PointTriangle } from './PointTriangle'
import { CheckerStack } from './CheckerStack'
import { HighlightType } from './board.types'

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
  const themeColors = useThemeColors()

  // Build highlight colors from theme - must be called before any conditional returns
  const highlightColors: Record<HighlightType, string> = useMemo(
    () => ({
      source: themeColors.highlightSource,
      selected: themeColors.highlightSelected,
      destination: themeColors.highlightDest,
      capture: themeColors.highlightCapture,
      combined: 'hsla(280 70% 50% / 0.6)', // Purple for combined moves
      analysis: themeColors.highlightAnalysis,
    }),
    [themeColors]
  )

  const coords = POINT_COORDS[pointNum]
  if (!coords) return null

  const { x, y, direction } = coords
  const isTop = direction === 1

  // Determine fill color
  const baseFillColor =
    pointNum % 2 === 0 ? themeColors.pointLight : themeColors.pointDark
  const fillColor = highlight ? highlightColors[highlight] : baseFillColor

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
          fill={themeColors.textLight}
          fontSize={12}
          style={{ pointerEvents: 'none' }}
        >
          {pointNum}
        </text>
      )}
    </g>
  )
})
