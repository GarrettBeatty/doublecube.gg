import { create } from 'zustand'
import type { GameState } from '@/types/generated/Backgammon.Server.Models'
import { SelectedChecker, Destination } from '@/types/game.types'

interface BoardInteractionStore {
  // Board interaction state
  selectedChecker: SelectedChecker | null
  validDestinations: Destination[]
  validSources: number[]
  isBoardFlipped: boolean

  // Actions
  selectChecker: (checker: SelectedChecker | null) => void
  setValidDestinations: (destinations: Destination[]) => void
  setValidSources: (sources: number[]) => void
  toggleBoardFlip: () => void
  setBoardFlipped: (flipped: boolean) => void
  updateValidSources: (gameState: GameState, isFreeMoveEnabled: boolean) => void
  resetBoardInteraction: () => void
}

// Helper functions for calculating valid sources
function calculateFreeMoveValidSources(state: GameState): number[] {
  const validSources: number[] = []

  // Bar (point 0)
  if (state.whiteCheckersOnBar > 0 || state.redCheckersOnBar > 0) {
    validSources.push(0)
  }

  // Board points (1-24)
  state.board.forEach((point, index) => {
    if (point.count > 0) {
      validSources.push(index + 1)
    }
  })

  // Bear-off (point 25)
  if (state.whiteBornOff > 0 || state.redBornOff > 0) {
    validSources.push(25)
  }

  return validSources
}

function calculateRuleBasedValidSources(state: GameState): number[] {
  return state.isYourTurn && state.validMoves
    ? Array.from(new Set(state.validMoves.map((move) => move.from)))
    : []
}

export const useBoardInteractionStore = create<BoardInteractionStore>((set) => ({
  // Initial state
  selectedChecker: null,
  validDestinations: [],
  validSources: [],
  isBoardFlipped: false,

  // Actions
  selectChecker: (checker) => set({ selectedChecker: checker }),

  setValidDestinations: (destinations) => set({ validDestinations: destinations }),

  setValidSources: (sources) => set({ validSources: sources }),

  toggleBoardFlip: () => set((state) => ({ isBoardFlipped: !state.isBoardFlipped })),

  setBoardFlipped: (flipped) => set({ isBoardFlipped: flipped }),

  updateValidSources: (gameState, isFreeMoveEnabled) => {
    const validSources = isFreeMoveEnabled && gameState.isAnalysisMode
      ? calculateFreeMoveValidSources(gameState)
      : calculateRuleBasedValidSources(gameState)
    set({ validSources })
  },

  resetBoardInteraction: () =>
    set({
      selectedChecker: null,
      validDestinations: [],
      validSources: [],
      // Keep board flip preference
    }),
}))

// Export helper functions for use elsewhere
export { calculateFreeMoveValidSources, calculateRuleBasedValidSources }
