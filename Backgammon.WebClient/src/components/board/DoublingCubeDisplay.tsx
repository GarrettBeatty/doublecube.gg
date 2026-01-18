import { memo } from 'react'
import { BOARD_CONFIG } from '@/lib/boardConstants'
import { useThemeColors } from '@/stores/themeStore'
import { DoublingCubeState } from './board.types'

interface DoublingCubeDisplayProps {
  cube: DoublingCubeState
  isFlipped?: boolean
}

export const DoublingCubeDisplay = memo(function DoublingCubeDisplay({
  cube,
  isFlipped = false,
}: DoublingCubeDisplayProps) {
  const colors = useThemeColors()
  const cubeSize = 36
  // Center cube in the bearoff area
  const bearoffStartX =
    BOARD_CONFIG.barX + BOARD_CONFIG.barWidth + 6 * BOARD_CONFIG.pointWidth
  const cubeX = bearoffStartX + BOARD_CONFIG.bearoffWidth / 2
  const centerY = BOARD_CONFIG.viewBox.height / 2

  // Determine Y position based on ownership
  let cubeY = centerY // center by default

  if (cube.owner !== 'center') {
    // Position at top or bottom based on ownership and board flip
    const isWhiteSide =
      (cube.owner === 'white' && !isFlipped) ||
      (cube.owner === 'red' && isFlipped)

    if (isWhiteSide) {
      cubeY = BOARD_CONFIG.viewBox.height - BOARD_CONFIG.padding - cubeSize / 2
    } else {
      cubeY = BOARD_CONFIG.padding + cubeSize / 2
    }
  }

  // Counter-rotate if board is flipped
  const transform = isFlipped
    ? `rotate(180 ${BOARD_CONFIG.viewBox.width / 2} ${centerY})`
    : undefined

  return (
    <g id="doubling-cube" transform={transform}>
      {/* Shadow */}
      <rect
        x={cubeX - cubeSize / 2 + 2}
        y={cubeY - cubeSize / 2 + 2}
        width={cubeSize}
        height={cubeSize}
        rx={6}
        fill="rgba(0, 0, 0, 0.2)"
        style={{ filter: 'blur(4px)' }}
      />

      {/* Cube body */}
      <rect
        x={cubeX - cubeSize / 2}
        y={cubeY - cubeSize / 2}
        width={cubeSize}
        height={cubeSize}
        rx={6}
        fill={colors.doublingCubeBackground}
        stroke={colors.doublingCubeStroke}
        strokeWidth={2}
      />

      {/* Cube value */}
      <text
        x={cubeX}
        y={cubeY}
        textAnchor="middle"
        dominantBaseline="middle"
        fontSize={20}
        fontWeight="bold"
        fill={colors.doublingCubeText}
        style={{ userSelect: 'none', pointerEvents: 'none' }}
      >
        {cube.value}
      </text>

      {/* Crawford indicator */}
      {cube.isCrawford && (
        <>
          <circle
            cx={cubeX + cubeSize / 2 - 8}
            cy={cubeY - cubeSize / 2 + 8}
            r={7}
            fill="#ef4444"
          />
          <text
            x={cubeX + cubeSize / 2 - 8}
            y={cubeY - cubeSize / 2 + 8}
            textAnchor="middle"
            dominantBaseline="middle"
            fontSize={8}
            fontWeight="bold"
            fill="white"
            style={{ userSelect: 'none', pointerEvents: 'none' }}
          >
            C
          </text>
        </>
      )}
    </g>
  )
})
