import { Move } from './game.types'

/**
 * Position features for analysis
 */
export interface PositionFeatures {
  pipCount: number
  pipDifference: number
  blotCount: number
  blotExposure: number
  checkersOnBar: number
  primeLength: number
  anchorsInOpponentHome: number
  homeboardCoverage: number
  distribution: number
  isContact: boolean
  isRace: boolean
  wastedPips: number
  bearoffEfficiency: number
  checkersBornOff: number
}

/**
 * Position evaluation result
 */
export interface PositionEvaluation {
  equity: number
  winProbability: number
  gammonProbability: number
  backgammonProbability: number
  features: PositionFeatures
}

/**
 * Move sequence with evaluation
 */
export interface MoveSequence {
  moves: Move[]
  notation: string
  equity: number
  equityGain: number
}

/**
 * Best moves analysis result
 */
export interface BestMovesAnalysis {
  initialEvaluation: PositionEvaluation
  topMoves: MoveSequence[]
  totalSequencesExplored: number
}
