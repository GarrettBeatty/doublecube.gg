import { memo } from 'react'
import { BOARD_CONFIG } from '@/lib/boardConstants'
import { useThemeColors } from '@/stores/themeStore'
import { DiceState } from './board.types'

interface DiceDisplayProps {
  dice: DiceState
  isFlipped?: boolean
  statusText?: string
}

export const DiceDisplay = memo(function DiceDisplay({
  dice,
  isFlipped = false,
  statusText,
}: DiceDisplayProps) {
  const colors = useThemeColors()
  const barCenterX = BOARD_CONFIG.barX + BOARD_CONFIG.barWidth / 2
  const centerY = BOARD_CONFIG.viewBox.height / 2
  const diceSize = 36
  const diceGap = 6

  // Only render if dice are rolled
  if (!dice.values || dice.values.length === 0 || !dice.values.some((d) => d > 0)) {
    return null
  }

  const totalHeight =
    dice.values.length * diceSize + (dice.values.length - 1) * diceGap
  const startY = centerY - totalHeight / 2

  // Counter-rotate if board is flipped
  const transform = isFlipped
    ? `rotate(180 ${barCenterX} ${centerY})`
    : undefined

  const remainingCount = dice.remainingMoves?.length ?? 0
  const totalMoves = dice.values.length === 2 && dice.values[0] === dice.values[1]
    ? 4 // doubles
    : dice.values.length

  return (
    <g id="dice" transform={transform}>
      {dice.values.map((die, index) => {
        const y = startY + index * (diceSize + diceGap)

        return (
          <g key={index}>
            {/* Dice background */}
            <rect
              x={barCenterX - diceSize / 2}
              y={y}
              width={diceSize}
              height={diceSize}
              rx={4}
              fill={colors.diceBackground}
              style={{ filter: 'drop-shadow(0 4px 6px rgba(0,0,0,0.3))' }}
            />
            {/* Dice value */}
            <text
              x={barCenterX}
              y={y + diceSize / 2}
              textAnchor="middle"
              dominantBaseline="middle"
              fill={colors.diceDots}
              fontSize={20}
              fontWeight="bold"
            >
              {die}
            </text>
          </g>
        )
      })}

      {/* Remaining moves indicator */}
      {totalMoves > 0 && (
        <text
          x={barCenterX}
          y={startY + totalHeight + 12}
          textAnchor="middle"
          dominantBaseline="hanging"
          fill={colors.textLight}
          fontSize={11}
          fontWeight="bold"
        >
          {remainingCount} / {totalMoves} moves
        </text>
      )}

      {/* Status text */}
      {statusText && (
        <text
          x={barCenterX}
          y={startY + totalHeight + 26}
          textAnchor="middle"
          dominantBaseline="hanging"
          fill={colors.textLight}
          fontSize={10}
          fontWeight="bold"
        >
          {statusText}
        </text>
      )}
    </g>
  )
})
