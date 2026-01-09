import { useGameStore } from '@/stores/gameStore'
import { CheckerColor } from '@/types/game.types'
import { cn } from '@/lib/utils'
import { useState } from 'react'

interface DoublingCubeProps {
  onOfferDouble?: () => void
}

export function DoublingCube({ onOfferDouble }: DoublingCubeProps) {
  const { currentGameState, myColor, doublingCube } = useGameStore()
  const [isHovered, setIsHovered] = useState(false)

  if (!currentGameState) return null

  const { value, owner, canDouble } = doublingCube
  const isCrawfordGame = currentGameState.isCrawfordGame

  // Determine cube position based on ownership
  const getCubePosition = () => {
    if (owner === null) return 'center'
    if (owner === CheckerColor.White) return 'white'
    return 'red'
  }

  const position = getCubePosition()
  const isClickable = canDouble && !isCrawfordGame && onOfferDouble

  const handleClick = () => {
    if (isClickable) {
      onOfferDouble?.()
    }
  }

  // Position classes based on owner
  const positionClasses = {
    center: 'top-1/2 left-1/2 -translate-x-1/2 -translate-y-1/2',
    white: myColor === CheckerColor.White ? 'bottom-4 right-4' : 'top-4 right-4',
    red: myColor === CheckerColor.Red ? 'bottom-4 right-4' : 'top-4 right-4',
  }

  // Styling based on state
  const cubeClasses = cn(
    'absolute w-16 h-16 transition-all duration-500',
    positionClasses[position],
    'flex items-center justify-center',
    'rounded-lg shadow-lg',
    'font-bold text-3xl',
    {
      // Crawford game - disabled
      'bg-gray-400 text-gray-600 cursor-not-allowed': isCrawfordGame,
      // Can double - glowing effect
      'bg-yellow-400 text-gray-900 cursor-pointer hover:bg-yellow-300 hover:shadow-xl animate-pulse':
        canDouble && !isCrawfordGame,
      // Opponent owns - dimmed
      'bg-gray-300 text-gray-700':
        !canDouble && !isCrawfordGame && owner !== null && owner !== myColor,
      // Center (no owner) - neutral
      'bg-white text-gray-900 border-2 border-gray-400':
        !canDouble && !isCrawfordGame && owner === null,
      // My ownership but can't double yet (dice rolled)
      'bg-blue-300 text-gray-900':
        !canDouble && !isCrawfordGame && owner === myColor,
    }
  )

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
    <div
      className={cubeClasses}
      onClick={handleClick}
      onMouseEnter={() => setIsHovered(true)}
      onMouseLeave={() => setIsHovered(false)}
      title={tooltipText}
      role={isClickable ? 'button' : 'status'}
      aria-label={tooltipText}
    >
      <span className="select-none">{value}</span>

      {/* Crawford indicator */}
      {isCrawfordGame && (
        <div className="absolute -top-2 -right-2 bg-red-500 text-white text-xs px-1.5 py-0.5 rounded-full">
          C
        </div>
      )}

      {/* Hover tooltip */}
      {isHovered && (
        <div className="absolute -bottom-8 left-1/2 -translate-x-1/2 bg-gray-900 text-white text-xs px-2 py-1 rounded whitespace-nowrap z-50">
          {tooltipText}
        </div>
      )}
    </div>
  )
}
