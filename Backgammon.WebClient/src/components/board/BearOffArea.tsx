import { memo, useMemo } from 'react'
import { BOARD_CONFIG } from '@/lib/boardConstants'
import { useThemeColors } from '@/stores/themeStore'
import { HighlightType } from './board.types'

interface BearOffAreaProps {
  whiteBornOff: number
  redBornOff: number
  highlight?: HighlightType | null
  onClick?: () => void
}

export const BearOffArea = memo(function BearOffArea({
  whiteBornOff,
  redBornOff,
  highlight,
  onClick,
}: BearOffAreaProps) {
  const themeColors = useThemeColors()
  const bearoffWidth = BOARD_CONFIG.bearoffWidth
  const bearoffX = BOARD_CONFIG.barX + BOARD_CONFIG.barWidth + 6 * BOARD_CONFIG.pointWidth
  const padding = BOARD_CONFIG.padding
  const viewBoxHeight = BOARD_CONFIG.viewBox.height
  const centerX = bearoffX + bearoffWidth / 2

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

  // Fill color
  const fillColor = highlight ? highlightColors[highlight] : themeColors.bearoff

  // Checker dimensions for born-off display
  const checkerWidth = bearoffWidth - 10
  const checkerHeight = 8
  const maxVisible = 5

  // Render stacked borne-off checkers as thin rectangles
  const renderBornOffCheckers = (
    count: number,
    color: 'white' | 'red',
    startY: number,
    direction: 1 | -1
  ) => {
    if (count === 0) return null

    const visibleCount = Math.min(count, maxVisible)
    const checkerFillColor =
      color === 'white' ? themeColors.checkerWhite : themeColors.checkerRed
    const strokeColor =
      color === 'white' ? themeColors.checkerWhiteStroke : themeColors.checkerRedStroke

    return (
      <g>
        {Array.from({ length: visibleCount }).map((_, i) => (
          <rect
            key={i}
            x={centerX - checkerWidth / 2}
            y={startY + i * (checkerHeight + 2) * direction}
            width={checkerWidth}
            height={checkerHeight}
            fill={checkerFillColor}
            stroke={strokeColor}
            strokeWidth={1}
            rx={2}
          />
        ))}
        {count > maxVisible && (
          <text
            x={centerX}
            y={startY + (maxVisible + 1) * (checkerHeight + 2) * direction}
            textAnchor="middle"
            dominantBaseline="middle"
            fill={color === 'white' ? '#333' : '#fff'}
            fontSize={10}
            fontWeight="bold"
          >
            {count}
          </text>
        )}
      </g>
    )
  }

  return (
    <g id="bearoff">
      {/* Bear-off area background */}
      <rect
        x={bearoffX}
        y={padding}
        width={bearoffWidth}
        height={viewBoxHeight - 2 * padding}
        fill={fillColor}
        onClick={onClick}
        style={onClick ? { cursor: 'pointer' } : undefined}
      />

      {/* White born-off (bottom) */}
      {renderBornOffCheckers(whiteBornOff, 'white', viewBoxHeight - padding - 10, -1)}

      {/* Red born-off (top) */}
      {renderBornOffCheckers(redBornOff, 'red', padding + 10, 1)}
    </g>
  )
})
