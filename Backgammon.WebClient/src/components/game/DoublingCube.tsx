import { useGameStore } from '@/stores/gameStore'
import { CheckerColor } from '@/types/game.types'
import { useState } from 'react'

interface DoublingCubeProps {
  onOfferDouble?: () => void
}

// Board configuration (matches BoardSVG.tsx)
const CONFIG = {
  viewBox: { width: 1020, height: 500 },
  margin: 20,
  pointWidth: 60,
  barWidth: 70,
  padding: 40,
}

// Calculate bar position
const boardStartX = CONFIG.margin
const barX = boardStartX + 6 * CONFIG.pointWidth

export function DoublingCube({ onOfferDouble }: DoublingCubeProps) {
  const { currentGameState, myColor, doublingCube, isBoardFlipped } = useGameStore()
  const [isHovered, setIsHovered] = useState(false)

  if (!currentGameState) return null

  const { value, owner, canDouble } = doublingCube
  const isCrawfordGame = currentGameState.isCrawfordGame

  const isClickable = canDouble && !isCrawfordGame && onOfferDouble

  const handleClick = () => {
    if (isClickable) {
      onOfferDouble?.()
    }
  }

  // Determine cube position based on ownership
  const getCubePosition = (): { x: number; y: number } => {
    const cubeSize = 50
    const barCenterX = barX + CONFIG.barWidth / 2

    if (owner === null) {
      // Center of the bar
      return {
        x: barCenterX,
        y: CONFIG.viewBox.height / 2,
      }
    }

    // Determine which side based on board flip and ownership
    const isWhiteSide = (owner === CheckerColor.White && !isBoardFlipped) ||
                         (owner === CheckerColor.Red && isBoardFlipped)

    if (isWhiteSide) {
      // Bottom of bar
      return {
        x: barCenterX,
        y: CONFIG.viewBox.height - CONFIG.padding - cubeSize / 2,
      }
    } else {
      // Top of bar
      return {
        x: barCenterX,
        y: CONFIG.padding + cubeSize / 2,
      }
    }
  }

  const position = getCubePosition()
  const cubeSize = 50

  // Determine fill color based on state
  const getFillColor = () => {
    if (isCrawfordGame) return '#9ca3af' // gray-400
    if (canDouble) return '#fbbf24' // yellow-400
    if (owner === null) return '#ffffff' // white
    if (owner === myColor) return '#93c5fd' // blue-300
    return '#d1d5db' // gray-300
  }

  const getStrokeColor = () => {
    if (canDouble) return '#f59e0b' // yellow-600
    if (owner === null) return '#9ca3af' // gray-400
    return 'none'
  }

  const tooltipText = isCrawfordGame
    ? 'Doubling disabled (Crawford game)'
    : canDouble
      ? `Click to offer double to ${value * 2}`
      : owner === null
        ? 'Doubling cube in center'
        : owner === myColor
          ? 'You own the cube'
          : 'Opponent owns the cube'

  return (
    <g
      onClick={handleClick}
      onMouseEnter={() => setIsHovered(true)}
      onMouseLeave={() => setIsHovered(false)}
      style={{
        cursor: isClickable ? 'pointer' : 'default',
        transition: 'all 0.5s ease',
      }}
      role={isClickable ? 'button' : 'status'}
      aria-label={tooltipText}
    >
      {/* Cube shadow */}
      <rect
        x={position.x - cubeSize / 2 + 2}
        y={position.y - cubeSize / 2 + 2}
        width={cubeSize}
        height={cubeSize}
        rx={8}
        fill="rgba(0, 0, 0, 0.2)"
        filter="blur(4px)"
      />

      {/* Cube body */}
      <rect
        x={position.x - cubeSize / 2}
        y={position.y - cubeSize / 2}
        width={cubeSize}
        height={cubeSize}
        rx={8}
        fill={getFillColor()}
        stroke={getStrokeColor()}
        strokeWidth={canDouble ? 3 : owner === null ? 2 : 0}
        opacity={isHovered && isClickable ? 0.9 : 1}
      >
        {canDouble && (
          <animate
            attributeName="opacity"
            values="1;0.7;1"
            dur="1.5s"
            repeatCount="indefinite"
          />
        )}
      </rect>

      {/* Cube value text */}
      <text
        x={position.x}
        y={position.y}
        textAnchor="middle"
        dominantBaseline="central"
        fontSize="28"
        fontWeight="bold"
        fill={isCrawfordGame ? '#4b5563' : '#111827'}
        style={{ userSelect: 'none', pointerEvents: 'none' }}
      >
        {value}
      </text>

      {/* Crawford indicator */}
      {isCrawfordGame && (
        <>
          <circle
            cx={position.x + cubeSize / 2 - 8}
            cy={position.y - cubeSize / 2 + 8}
            r={10}
            fill="#ef4444"
          />
          <text
            x={position.x + cubeSize / 2 - 8}
            y={position.y - cubeSize / 2 + 8}
            textAnchor="middle"
            dominantBaseline="central"
            fontSize="10"
            fontWeight="bold"
            fill="white"
            style={{ userSelect: 'none', pointerEvents: 'none' }}
          >
            C
          </text>
        </>
      )}

      {/* Hover tooltip */}
      {isHovered && (
        <>
          <rect
            x={position.x - 80}
            y={position.y + cubeSize / 2 + 10}
            width={160}
            height={24}
            rx={4}
            fill="#1f2937"
            opacity={0.95}
          />
          <text
            x={position.x}
            y={position.y + cubeSize / 2 + 22}
            textAnchor="middle"
            dominantBaseline="central"
            fontSize="11"
            fill="white"
            style={{ userSelect: 'none', pointerEvents: 'none' }}
          >
            {tooltipText}
          </text>
        </>
      )}
    </g>
  )
}
