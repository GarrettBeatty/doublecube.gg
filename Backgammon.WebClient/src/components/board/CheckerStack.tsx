import { memo } from 'react'
import { BOARD_CONFIG } from '@/lib/boardConstants'
import { Checker } from './Checker'

interface CheckerStackProps {
  baseX: number // Center X of the stack
  baseY: number // Edge Y of the point (top or bottom)
  direction: 1 | -1 // 1 = stack grows down, -1 = stack grows up
  color: 'white' | 'red'
  count: number
  selectedIndex?: number // Which checker is selected (-1 or undefined = none)
  draggableTopChecker?: boolean
  onTopCheckerDragStart?: (e: React.MouseEvent | React.TouchEvent) => void
}

export const CheckerStack = memo(function CheckerStack({
  baseX,
  baseY,
  direction,
  color,
  count,
  selectedIndex,
  draggableTopChecker = false,
  onTopCheckerDragStart,
}: CheckerStackProps) {
  if (count === 0) return null

  const maxVisible = Math.min(count, 5)
  const checkerRadius = BOARD_CONFIG.checkerRadius
  const spacing = BOARD_CONFIG.checkerSpacing

  // Calculate checker positions
  // First checker is offset by radius from the edge, then each subsequent checker is spaced
  const getCheckerY = (index: number) => {
    if (direction === 1) {
      // Top row: checkers go downward
      return baseY + checkerRadius + index * spacing
    } else {
      // Bottom row: checkers go upward
      return baseY - checkerRadius - index * spacing
    }
  }

  return (
    <g>
      {Array.from({ length: maxVisible }).map((_, i) => {
        const isTop = i === maxVisible - 1
        const isSelected = selectedIndex === i

        return (
          <Checker
            key={i}
            cx={baseX}
            cy={getCheckerY(i)}
            color={color}
            isSelected={isSelected}
            isDraggable={isTop && draggableTopChecker}
            onDragStart={isTop ? onTopCheckerDragStart : undefined}
          />
        )
      })}

      {/* Show count badge if more than 5 checkers */}
      {count > 5 && (
        <text
          x={baseX}
          y={getCheckerY(maxVisible - 1)}
          textAnchor="middle"
          dominantBaseline="middle"
          fill={color === 'white' ? '#333' : '#fff'}
          fontSize={14}
          fontWeight="bold"
          style={{ pointerEvents: 'none' }}
        >
          {count}
        </text>
      )}
    </g>
  )
})
