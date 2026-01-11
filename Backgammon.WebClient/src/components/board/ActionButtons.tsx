import { memo } from 'react'
import { BOARD_CONFIG } from '@/lib/boardConstants'
import { ButtonConfig } from './board.types'

interface ActionButtonsProps {
  buttons: ButtonConfig[]
  isFlipped?: boolean
}

// Button colors based on variant
const BUTTON_COLORS: Record<
  NonNullable<ButtonConfig['variant']>,
  { bg: string; text: string }
> = {
  default: { bg: 'hsl(0 0% 98%)', text: 'hsl(0 0% 9%)' },
  primary: { bg: 'hsl(221.2 83.2% 53.3%)', text: 'hsl(0 0% 98%)' },
  warning: { bg: 'hsl(47.9 95.8% 53.1%)', text: 'hsl(0 0% 9%)' },
  danger: { bg: 'hsl(0 84.2% 60.2%)', text: 'hsl(0 0% 98%)' },
}

// Button labels based on type
const BUTTON_LABELS: Record<ButtonConfig['type'], string> = {
  roll: 'Roll',
  undo: 'Undo',
  end: 'End',
  double: 'Double',
  resign: 'Resign',
}

export const ActionButtons = memo(function ActionButtons({
  buttons,
  isFlipped = false,
}: ActionButtonsProps) {
  if (!buttons || buttons.length === 0) return null

  const centerX = BOARD_CONFIG.viewBox.width / 2
  const centerY = BOARD_CONFIG.viewBox.height / 2

  // Button positions
  const leftSideX = BOARD_CONFIG.viewBox.width * 0.2216
  const rightSideX = BOARD_CONFIG.viewBox.width * 0.7265

  // Get radius based on button type (original: Roll/End = 40, Undo/Double = 32)
  const getButtonRadius = (type: ButtonConfig['type']): number => {
    switch (type) {
      case 'roll':
      case 'end':
        return 40
      case 'undo':
      case 'double':
      case 'resign':
        return 32
      default:
        return 25
    }
  }

  // Counter-rotate if board is flipped
  const transform = isFlipped
    ? `rotate(180 ${centerX} ${centerY})`
    : undefined

  // Position buttons based on their type
  // Original layout: Roll/End on RIGHT, Undo/Double on LEFT (mutually exclusive pairs)
  const getButtonPosition = (button: ButtonConfig, index: number) => {
    switch (button.type) {
      case 'roll':
        return { x: rightSideX, y: centerY }
      case 'end':
        return { x: rightSideX, y: centerY }
      case 'double':
        return { x: leftSideX, y: centerY }
      case 'undo':
        return { x: leftSideX, y: centerY }
      case 'resign':
        return { x: leftSideX, y: centerY + 60 }
      default:
        return { x: centerX, y: centerY + index * 60 }
    }
  }

  return (
    <g id="action-buttons" transform={transform}>
      {buttons.map((button, index) => {
        if (button.disabled) return null

        const { x, y } = getButtonPosition(button, index)
        const colors = BUTTON_COLORS[button.variant || 'default']
        const label = BUTTON_LABELS[button.type]
        const radius = getButtonRadius(button.type)
        // Smaller font for smaller buttons
        const fontSize = radius >= 40 ? 14 : 12

        return (
          <g
            key={button.type}
            style={{ cursor: 'pointer' }}
            onClick={button.onClick}
          >
            {/* Button circle */}
            <circle
              cx={x}
              cy={y}
              r={radius}
              fill={colors.bg}
              stroke="rgba(0,0,0,0.1)"
              strokeWidth={2}
              style={{
                filter: 'drop-shadow(0 2px 4px rgba(0,0,0,0.3))',
                transition: 'transform 0.1s',
              }}
            />
            {/* Button label */}
            <text
              x={x}
              y={y}
              textAnchor="middle"
              dominantBaseline="middle"
              fontSize={fontSize}
              fontWeight="bold"
              fill={colors.text}
              style={{ userSelect: 'none', pointerEvents: 'none' }}
            >
              {label}
            </text>
          </g>
        )
      })}
    </g>
  )
})
