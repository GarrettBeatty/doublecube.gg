import { memo } from 'react'
import { BOARD_CONFIG, BOARD_COLORS } from '@/lib/boardConstants'
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
  const bearoffWidth = BOARD_CONFIG.bearoffWidth
  const bearoffX = BOARD_CONFIG.barX + BOARD_CONFIG.barWidth + 6 * BOARD_CONFIG.pointWidth
  const padding = BOARD_CONFIG.padding
  const viewBoxHeight = BOARD_CONFIG.viewBox.height
  const centerX = bearoffX + bearoffWidth / 2

  // Fill color
  const fillColor = highlight ? HIGHLIGHT_COLORS[highlight] : BOARD_COLORS.bearoff

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
    const fillColor =
      color === 'white' ? BOARD_COLORS.checkerWhite : BOARD_COLORS.checkerRed
    const strokeColor =
      color === 'white'
        ? BOARD_COLORS.checkerWhiteStroke
        : BOARD_COLORS.checkerRedStroke

    return (
      <g>
        {Array.from({ length: visibleCount }).map((_, i) => (
          <rect
            key={i}
            x={centerX - checkerWidth / 2}
            y={startY + i * (checkerHeight + 2) * direction}
            width={checkerWidth}
            height={checkerHeight}
            fill={fillColor}
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
