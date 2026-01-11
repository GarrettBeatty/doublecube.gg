import { memo, useRef } from 'react'
import { BOARD_CONFIG } from '@/lib/boardConstants'
import { BoardProvider, useBoardContext } from './BoardProvider'
import { BoardBackground } from './BoardBackground'
import { PointsLayer } from './PointsLayer'
import { BarArea } from './BarArea'
import { BearOffArea } from './BearOffArea'
import { GhostChecker } from './GhostChecker'
import { DiceDisplay } from './DiceDisplay'
import { ActionButtons } from './ActionButtons'
import { DoublingCubeDisplay } from './DoublingCubeDisplay'
import {
  UnifiedBoardProps,
  BoardDisplayOptions,
  DiceState,
  ButtonConfig,
  DoublingCubeState,
} from './board.types'

interface BoardContentProps {
  dice?: DiceState
  buttons?: ButtonConfig[]
  doublingCube?: DoublingCubeState
}

// Inner component that uses context
function BoardContent({ dice, buttons, doublingCube }: BoardContentProps) {
  const {
    position,
    display,
    highlights,
    dragState,
    startDrag,
    isHighlighted,
    isDraggable,
  } = useBoardContext()

  // Get bar highlight
  const barHighlight = isHighlighted(0)

  // Get bear-off highlight
  const bearoffHighlight = isHighlighted(25)

  // Check if bar checkers are draggable
  const whiteBarDraggable = isDraggable(0) && position.whiteOnBar > 0
  const redBarDraggable = isDraggable(0) && position.redOnBar > 0

  return (
    <>
      <BoardBackground />
      <PointsLayer
        points={position.points}
        highlights={highlights}
        showNumbers={display.showPointNumbers}
        isDraggable={isDraggable}
        onCheckerDragStart={startDrag}
      />
      <BarArea
        whiteOnBar={position.whiteOnBar}
        redOnBar={position.redOnBar}
        highlight={barHighlight}
        whiteIsDraggable={whiteBarDraggable}
        redIsDraggable={redBarDraggable}
        onWhiteDragStart={(e) => startDrag(e, 0, 'white')}
        onRedDragStart={(e) => startDrag(e, 0, 'red')}
      />
      <BearOffArea
        whiteBornOff={position.whiteBornOff}
        redBornOff={position.redBornOff}
        highlight={bearoffHighlight}
      />
      {/* Ghost checker during drag */}
      {dragState.isDragging && dragState.ghostPosition && dragState.ghostColor && (
        <GhostChecker
          x={dragState.ghostPosition.x}
          y={dragState.ghostPosition.y}
          color={dragState.ghostColor}
        />
      )}
      {/* Overlay components */}
      {dice && <DiceDisplay dice={dice} isFlipped={display.isFlipped} />}
      {buttons && buttons.length > 0 && <ActionButtons buttons={buttons} isFlipped={display.isFlipped} />}
      {doublingCube && <DoublingCubeDisplay cube={doublingCube} isFlipped={display.isFlipped} />}
    </>
  )
}

export const UnifiedBoard = memo(function UnifiedBoard({
  position,
  display = {},
  highlights = [],
  interaction,
  dice,
  buttons,
  doublingCube,
  children,
}: UnifiedBoardProps) {
  const svgRef = useRef<SVGSVGElement>(null)

  // Merge display options with defaults
  const displayWithDefaults: Required<BoardDisplayOptions> = {
    isFlipped: false,
    showPointNumbers: false,
    interactionMode: 'none',
    ...display,
  }

  // Transform class based on flip state
  const transformStyle = displayWithDefaults.isFlipped
    ? { transform: 'rotate(180deg)', transition: 'transform 0.6s' }
    : { transition: 'transform 0.6s' }

  return (
    <svg
      ref={svgRef}
      viewBox={`0 0 ${BOARD_CONFIG.viewBox.width} ${BOARD_CONFIG.viewBox.height}`}
      className="w-full h-auto"
      style={transformStyle}
    >
      <BoardProvider
        position={position}
        display={displayWithDefaults}
        highlights={highlights}
        interaction={interaction}
        svgRef={svgRef}
      >
        <BoardContent dice={dice} buttons={buttons} doublingCube={doublingCube} />
        {children}
      </BoardProvider>
    </svg>
  )
})
