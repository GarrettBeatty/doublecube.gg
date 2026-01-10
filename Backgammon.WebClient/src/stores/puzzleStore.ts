import { create } from 'zustand'
import {
  DailyPuzzle,
  PuzzleResult,
  PuzzleStreakInfo,
  PendingPuzzleMove,
  PuzzleMove,
} from '@/types/puzzle.types'
import { CheckerColor } from '@/types/game.types'

interface PuzzleStore {
  // Puzzle state
  currentPuzzle: DailyPuzzle | null
  isLoading: boolean
  error: string | null

  // User's current solution attempt
  pendingMoves: PendingPuzzleMove[]
  remainingDice: number[]

  // Result state
  result: PuzzleResult | null
  showResultModal: boolean

  // Streak info
  streakInfo: PuzzleStreakInfo | null

  // Board interaction
  selectedPoint: number | null
  validDestinations: number[]

  // Actions
  setPuzzle: (puzzle: DailyPuzzle | null) => void
  setLoading: (loading: boolean) => void
  setError: (error: string | null) => void

  // Move management
  addMove: (move: PendingPuzzleMove) => void
  undoLastMove: () => void
  clearMoves: () => void

  // Result management
  setResult: (result: PuzzleResult | null) => void
  setShowResultModal: (show: boolean) => void

  // Streak management
  setStreakInfo: (info: PuzzleStreakInfo | null) => void

  // Board interaction
  setSelectedPoint: (point: number | null) => void
  setValidDestinations: (destinations: number[]) => void

  // Computed helpers
  getPlayerColor: () => CheckerColor | null
  getCurrentBoardState: () => DailyPuzzle['boardState'] | null
  canSubmit: () => boolean

  // Reset state
  reset: () => void
}

const initialState = {
  currentPuzzle: null,
  isLoading: false,
  error: null,
  pendingMoves: [],
  remainingDice: [],
  result: null,
  showResultModal: false,
  streakInfo: null,
  selectedPoint: null,
  validDestinations: [],
}

export const usePuzzleStore = create<PuzzleStore>((set, get) => ({
  ...initialState,

  setPuzzle: (puzzle) => {
    set({
      currentPuzzle: puzzle,
      pendingMoves: [],
      remainingDice: puzzle ? [...puzzle.dice] : [],
      result: null,
      error: null,
      selectedPoint: null,
      validDestinations: [],
    })
  },

  setLoading: (loading) => set({ isLoading: loading }),
  setError: (error) => set({ error }),

  addMove: (move) => {
    const state = get()
    const newPendingMoves = [...state.pendingMoves, move]

    // Remove the used die from remaining dice
    const newRemainingDice = [...state.remainingDice]
    const dieIndex = newRemainingDice.indexOf(move.dieValue)
    if (dieIndex !== -1) {
      newRemainingDice.splice(dieIndex, 1)
    }

    set({
      pendingMoves: newPendingMoves,
      remainingDice: newRemainingDice,
      selectedPoint: null,
      validDestinations: [],
    })
  },

  undoLastMove: () => {
    const state = get()
    if (state.pendingMoves.length === 0) return

    const newPendingMoves = [...state.pendingMoves]
    newPendingMoves.pop()

    // Rebuild remainingDice from the original puzzle dice to preserve ordering
    let newRemainingDice: number[] = []
    const puzzleDice = state.currentPuzzle?.dice
    if (puzzleDice && puzzleDice.length > 0) {
      const availableDice = [...puzzleDice]

      // Remove one occurrence of each used die from availableDice
      for (const m of newPendingMoves) {
        const idx = availableDice.indexOf(m.dieValue)
        if (idx !== -1) {
          availableDice.splice(idx, 1)
        }
      }

      newRemainingDice = availableDice
    }

    set({
      pendingMoves: newPendingMoves,
      remainingDice: newRemainingDice,
      selectedPoint: null,
      validDestinations: [],
    })
  },

  clearMoves: () => {
    const state = get()
    set({
      pendingMoves: [],
      remainingDice: state.currentPuzzle ? [...state.currentPuzzle.dice] : [],
      selectedPoint: null,
      validDestinations: [],
    })
  },

  setResult: (result) => set({ result }),
  setShowResultModal: (show) => set({ showResultModal: show }),
  setStreakInfo: (info) => set({ streakInfo: info }),
  setSelectedPoint: (point) => set({ selectedPoint: point }),
  setValidDestinations: (destinations) => set({ validDestinations: destinations }),

  getPlayerColor: () => {
    const puzzle = get().currentPuzzle
    if (!puzzle) return null
    return puzzle.currentPlayer === 'White' ? CheckerColor.White : CheckerColor.Red
  },

  getCurrentBoardState: () => {
    const state = get()
    if (!state.currentPuzzle) return null
    // Return the current puzzle board state as-is
    return state.currentPuzzle.boardState
  },

  canSubmit: () => {
    const state = get()
    // Allow submit whenever there is at least one pending move; server will validate dice usage
    return state.pendingMoves.length > 0
  },

  reset: () => set(initialState),
}))

/**
 * Format moves as submission payload
 */
export function formatMovesForSubmission(moves: PendingPuzzleMove[]): PuzzleMove[] {
  return moves.map((m) => ({
    from: m.from,
    to: m.to,
    dieValue: m.dieValue,
    isHit: m.isHit,
  }))
}
