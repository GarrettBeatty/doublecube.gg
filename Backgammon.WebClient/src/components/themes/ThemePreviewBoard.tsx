import { memo } from 'react'
import type { ThemeColors } from '@/types/theme.types'

interface ThemePreviewBoardProps {
  colors: ThemeColors
  width?: number
  height?: number
}

/**
 * A miniature board preview showing theme colors.
 */
export const ThemePreviewBoard = memo(function ThemePreviewBoard({
  colors,
  width = 200,
  height = 100,
}: ThemePreviewBoardProps) {
  const pointWidth = width / 14 // 12 points + bar + bearoff
  const pointHeight = height * 0.4

  return (
    <svg width={width} height={height} viewBox={`0 0 ${width} ${height}`}>
      {/* Board background */}
      <rect
        x={0}
        y={0}
        width={width}
        height={height}
        fill={colors.boardBackground}
        stroke={colors.boardBorder}
        strokeWidth={2}
      />

      {/* Top points */}
      {[0, 1, 2, 3, 4, 5].map((i) => (
        <polygon
          key={`top-${i}`}
          points={`${i * pointWidth + 2},2 ${i * pointWidth + pointWidth / 2 + 2},${pointHeight} ${(i + 1) * pointWidth + 2},2`}
          fill={i % 2 === 0 ? colors.pointLight : colors.pointDark}
        />
      ))}

      {/* Bar */}
      <rect
        x={6 * pointWidth + 2}
        y={2}
        width={pointWidth}
        height={height - 4}
        fill={colors.bar}
      />

      {/* Top points after bar */}
      {[0, 1, 2, 3, 4, 5].map((i) => (
        <polygon
          key={`top-right-${i}`}
          points={`${(i + 7) * pointWidth + 2},2 ${(i + 7) * pointWidth + pointWidth / 2 + 2},${pointHeight} ${(i + 8) * pointWidth + 2},2`}
          fill={i % 2 === 0 ? colors.pointDark : colors.pointLight}
        />
      ))}

      {/* Bottom points */}
      {[0, 1, 2, 3, 4, 5].map((i) => (
        <polygon
          key={`bottom-${i}`}
          points={`${i * pointWidth + 2},${height - 2} ${i * pointWidth + pointWidth / 2 + 2},${height - pointHeight} ${(i + 1) * pointWidth + 2},${height - 2}`}
          fill={i % 2 === 0 ? colors.pointDark : colors.pointLight}
        />
      ))}

      {/* Bottom points after bar */}
      {[0, 1, 2, 3, 4, 5].map((i) => (
        <polygon
          key={`bottom-right-${i}`}
          points={`${(i + 7) * pointWidth + 2},${height - 2} ${(i + 7) * pointWidth + pointWidth / 2 + 2},${height - pointHeight} ${(i + 8) * pointWidth + 2},${height - 2}`}
          fill={i % 2 === 0 ? colors.pointLight : colors.pointDark}
        />
      ))}

      {/* Bear-off area */}
      <rect
        x={13 * pointWidth + 2}
        y={2}
        width={pointWidth - 4}
        height={height - 4}
        fill={colors.bearoff}
      />

      {/* Sample checkers - white on bottom left */}
      {[0, 1, 2].map((i) => (
        <circle
          key={`white-${i}`}
          cx={0.5 * pointWidth + 2}
          cy={height - 10 - i * 12}
          r={5}
          fill={colors.checkerWhite}
          stroke={colors.checkerWhiteStroke}
          strokeWidth={1}
        />
      ))}

      {/* Sample checkers - red on top right */}
      {[0, 1, 2].map((i) => (
        <circle
          key={`red-${i}`}
          cx={12.5 * pointWidth + 2}
          cy={10 + i * 12}
          r={5}
          fill={colors.checkerRed}
          stroke={colors.checkerRedStroke}
          strokeWidth={1}
        />
      ))}

      {/* Sample dice */}
      <rect
        x={6.5 * pointWidth - 4}
        y={height / 2 - 8}
        width={16}
        height={16}
        rx={2}
        fill={colors.diceBackground}
      />
      <circle
        cx={6.5 * pointWidth + 4}
        cy={height / 2}
        r={2}
        fill={colors.diceDots}
      />
    </svg>
  )
})
