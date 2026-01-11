import { Point } from './game.types'

/**
 * A move in a puzzle solution
 */
export interface PuzzleMove {
  from: number
  to: number
  dieValue: number
  isHit: boolean
  isCombinedMove: boolean
  diceUsed?: number[]
  intermediatePoints?: number[]
}

/**
 * Board state for displaying a puzzle position
 */
export interface PuzzlePointState {
  position: number
  color?: string
  count: number
}

/**
 * Daily puzzle as received from the server
 */
export interface DailyPuzzle {
  puzzleId: string
  puzzleDate: string
  currentPlayer: string
  dice: number[]
  boardState: PuzzlePointState[]
  whiteCheckersOnBar: number
  redCheckersOnBar: number
  whiteBornOff: number
  redBornOff: number
  alreadySolved: boolean
  attemptsToday: number
  bestMoves?: PuzzleMove[]
  bestMovesNotation?: string
}

/**
 * Result after submitting a puzzle answer
 */
export interface PuzzleResult {
  isCorrect: boolean
  equityLoss: number
  feedback: string
  bestMoves?: PuzzleMove[]
  bestMovesNotation?: string
  currentStreak: number
  streakBroken: boolean
  attemptCount: number
}

/**
 * User's puzzle streak information
 */
export interface PuzzleStreakInfo {
  userId?: string
  currentStreak: number
  bestStreak: number
  lastSolvedDate?: string
  totalSolved: number
  totalAttempts: number
}

/**
 * State of a move being made in puzzle mode
 */
export interface PendingPuzzleMove {
  from: number
  to: number
  dieValue: number
  isHit: boolean
  isCombinedMove?: boolean
}

/**
 * Convert puzzle board state to game Point format
 */
export function puzzleBoardToPoints(puzzleBoard: PuzzlePointState[]): Point[] {
  return puzzleBoard.map((p) => ({
    position: p.position,
    color: p.color === 'White' ? 0 : p.color === 'Red' ? 1 : null,
    count: p.count,
  }))
}

/**
 * Helper to convert PuzzleMove[] to MoveDto[] for API calls
 */
export function puzzleMovesToMoveDto(moves: PuzzleMove[]): Array<{
  from: number
  to: number
  dieValue: number
  isHit: boolean
  isCombinedMove: boolean
  diceUsed?: number[]
  intermediatePoints?: number[]
}> {
  return moves.map((m) => ({
    from: m.from,
    to: m.to,
    dieValue: m.dieValue,
    isHit: m.isHit,
    isCombinedMove: m.isCombinedMove ?? false,
    diceUsed: m.diceUsed,
    intermediatePoints: m.intermediatePoints,
  }))
}
