// Core components
export { BoardBackground } from './BoardBackground'
export { PointTriangle } from './PointTriangle'
export { Checker } from './Checker'
export { CheckerStack } from './CheckerStack'
export { Point } from './Point'
export { PointsLayer } from './PointsLayer'
export { BarArea } from './BarArea'
export { BearOffArea } from './BearOffArea'
export { GhostChecker } from './GhostChecker'

// Context
export { BoardProvider, useBoardContext } from './BoardProvider'

// Main board component
export { UnifiedBoard } from './UnifiedBoard'

// Overlay components
export { DiceDisplay } from './DiceDisplay'
export { ActionButtons } from './ActionButtons'
export { DoublingCubeDisplay } from './DoublingCubeDisplay'

// Adapters
export {
  GameBoardAdapter,
  PuzzleBoardAdapter,
  MiniBoardAdapter,
} from './adapters'

// Types
export type {
  BoardPosition,
  PointState,
  PointHighlight,
  HighlightType,
  DragState,
  InteractionCallbacks,
  BoardDisplayOptions,
  DiceState,
  ButtonConfig,
  DoublingCubeState,
  UnifiedBoardProps,
  BoardContextValue,
  InteractionMode,
} from './board.types'
