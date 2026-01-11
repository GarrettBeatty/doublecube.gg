// Board position types
export interface PointState {
  position: number // 1-24
  color: 'white' | 'red' | null
  count: number
}

export interface BoardPosition {
  points: PointState[] // 24 points
  whiteOnBar: number
  redOnBar: number
  whiteBornOff: number
  redBornOff: number
}

// Highlight system
export type HighlightType =
  | 'source' // Yellow - moveable pieces
  | 'selected' // Green - currently selected
  | 'destination' // Blue - valid landing spots
  | 'capture' // Red - blot hit
  | 'combined' // Purple - multi-die moves
  | 'analysis' // Light green - suggested moves

export interface PointHighlight {
  point: number // 0 = bar, 1-24 = points, 25 = bear-off
  type: HighlightType
}

// Interaction
export type InteractionMode = 'none' | 'click' | 'drag' | 'both'

export interface DragState {
  isDragging: boolean
  sourcePoint: number | null
  ghostPosition: { x: number; y: number } | null
  ghostColor: 'white' | 'red' | null
}

export interface InteractionCallbacks {
  onCheckerSelect?: (point: number) => void
  onMoveAttempt?: (from: number, to: number) => void
  onPointClick?: (point: number) => void
  getValidDestinations?: (from: number) => number[]
  isDraggable?: (point: number) => boolean
}

// Display options
export interface BoardDisplayOptions {
  isFlipped?: boolean
  showPointNumbers?: boolean
  interactionMode?: InteractionMode
}

// Dice
export interface DiceState {
  values: number[]
  remainingMoves: number[]
}

// Buttons
export interface ButtonConfig {
  type: 'roll' | 'undo' | 'end' | 'double' | 'resign'
  label: string
  onClick: () => void
  disabled?: boolean
  variant?: 'default' | 'primary' | 'warning' | 'danger'
}

// Doubling cube
export interface DoublingCubeState {
  value: number
  owner: 'white' | 'red' | 'center'
  isCrawford?: boolean
}

// Main UnifiedBoard props
export interface UnifiedBoardProps {
  position: BoardPosition
  display?: BoardDisplayOptions
  highlights?: PointHighlight[]
  interaction?: InteractionCallbacks
  dice?: DiceState
  buttons?: ButtonConfig[]
  doublingCube?: DoublingCubeState
  children?: React.ReactNode
}

// Context value
export interface BoardContextValue {
  position: BoardPosition
  display: Required<BoardDisplayOptions>
  highlights: PointHighlight[]
  dragState: DragState
  startDrag: (
    e: React.MouseEvent | React.TouchEvent,
    point: number,
    color: 'white' | 'red'
  ) => void
  isHighlighted: (point: number) => HighlightType | null
  isDraggable: (point: number) => boolean
}
