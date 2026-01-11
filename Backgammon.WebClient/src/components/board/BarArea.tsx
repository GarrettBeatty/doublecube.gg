import { memo } from 'react'
import { BOARD_CONFIG, BOARD_COLORS } from '@/lib/boardConstants'
import { CheckerStack } from './CheckerStack'
import { HighlightType } from './board.types'

// Highlight colors mapping
const HIGHLIGHT_COLORS: Record<HighlightType, string> = {
  source: BOARD_COLORS.highlightSource,
  selected: BOARD_COLORS.highlightSelected,
  destination: BOARD_COLORS.highlightDest,
  capture: BOARD_COLORS.highlightCapture,
  combined: 'hsla(280 70% 50% / 0.6)',
  analysis: BOARD_COLORS.highlightAnalysis,
}

interface BarAreaProps {
  whiteOnBar: number
  redOnBar: number
  highlight?: HighlightType | null
  whiteIsDraggable?: boolean
  redIsDraggable?: boolean
  onWhiteDragStart?: (e: React.MouseEvent | React.TouchEvent) => void
  onRedDragStart?: (e: React.MouseEvent | React.TouchEvent) => void
  onClick?: () => void
}

export const BarArea = memo(function BarArea({
  whiteOnBar,
  redOnBar,
  highlight,
  whiteIsDraggable = false,
  redIsDraggable = false,
  onWhiteDragStart,
  onRedDragStart,
  onClick,
}: BarAreaProps) {
  const barX = BOARD_CONFIG.barX
  const barWidth = BOARD_CONFIG.barWidth
  const barCenterX = barX + barWidth / 2
  const padding = BOARD_CONFIG.padding
  const viewBoxHeight = BOARD_CONFIG.viewBox.height

  // Bar fill color
  const fillColor = highlight ? HIGHLIGHT_COLORS[highlight] : BOARD_COLORS.bar

  // White checkers start from top, Red from bottom
  const whiteStartY = padding
  const redStartY = viewBoxHeight - padding

  return (
    <g id="bar">
      {/* Bar background */}
      <rect
        x={barX}
        y={padding}
        width={barWidth}
        height={viewBoxHeight - 2 * padding}
        fill={fillColor}
        onClick={onClick}
        style={onClick ? { cursor: 'pointer' } : undefined}
      />

      {/* White checkers on bar (top half) */}
      {whiteOnBar > 0 && (
        <CheckerStack
          baseX={barCenterX}
          baseY={whiteStartY}
          direction={1}
          color="white"
          count={whiteOnBar}
          draggableTopChecker={whiteIsDraggable}
          onTopCheckerDragStart={onWhiteDragStart}
        />
      )}

      {/* Red checkers on bar (bottom half) */}
      {redOnBar > 0 && (
        <CheckerStack
          baseX={barCenterX}
          baseY={redStartY}
          direction={-1}
          color="red"
          count={redOnBar}
          draggableTopChecker={redIsDraggable}
          onTopCheckerDragStart={onRedDragStart}
        />
      )}
    </g>
  )
})
