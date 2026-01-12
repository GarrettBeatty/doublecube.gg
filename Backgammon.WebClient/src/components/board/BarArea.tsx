import { memo, useMemo } from 'react'
import { BOARD_CONFIG } from '@/lib/boardConstants'
import { useThemeColors } from '@/stores/themeStore'
import { CheckerStack } from './CheckerStack'
import { HighlightType } from './board.types'

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
  const themeColors = useThemeColors()
  const barX = BOARD_CONFIG.barX
  const barWidth = BOARD_CONFIG.barWidth
  const barCenterX = barX + barWidth / 2
  const padding = BOARD_CONFIG.padding
  const viewBoxHeight = BOARD_CONFIG.viewBox.height

  // Build highlight colors from theme
  const highlightColors: Record<HighlightType, string> = useMemo(
    () => ({
      source: themeColors.highlightSource,
      selected: themeColors.highlightSelected,
      destination: themeColors.highlightDest,
      capture: themeColors.highlightCapture,
      combined: 'hsla(280 70% 50% / 0.6)',
      analysis: themeColors.highlightAnalysis,
    }),
    [themeColors]
  )

  // Bar fill color
  const fillColor = highlight ? highlightColors[highlight] : themeColors.bar

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
