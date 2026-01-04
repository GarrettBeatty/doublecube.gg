import { create } from 'zustand'
import {
  GameState,
  CheckerColor,
  SelectedChecker,
  Destination,
  ChatMessage,
} from '@/types/game.types'

interface GameStore {
  // Game state
  currentGameState: GameState | null
  myColor: CheckerColor | null
  currentGameId: string | null
  isSpectator: boolean
  isAnalysisMode: boolean

  // Analysis mode toggles
  isFreeMoveEnabled: boolean
  isCustomDiceEnabled: boolean

  // Board interaction state
  selectedChecker: SelectedChecker | null
  validDestinations: Destination[]
  validSources: number[]
  isBoardFlipped: boolean

  // UI state
  chatMessages: ChatMessage[]
  showChat: boolean

  // Actions
  setGameState: (state: GameState) => void
  updateTimeState: (timeUpdate: {
    whiteReserveSeconds: number
    redReserveSeconds: number
    whiteIsInDelay: boolean
    redIsInDelay: boolean
    whiteDelayRemaining: number
    redDelayRemaining: number
  }) => void
  setMyColor: (color: CheckerColor | null) => void
  setCurrentGameId: (gameId: string | null) => void
  setIsSpectator: (isSpectator: boolean) => void
  setFreeMoveEnabled: (enabled: boolean) => void
  setCustomDiceEnabled: (enabled: boolean) => void
  selectChecker: (checker: SelectedChecker | null) => void
  setValidDestinations: (destinations: Destination[]) => void
  setValidSources: (sources: number[]) => void
  toggleBoardFlip: () => void
  setBoardFlipped: (flipped: boolean) => void
  addChatMessage: (message: ChatMessage) => void
  clearChatMessages: () => void
  toggleChat: () => void
  resetGame: () => void
}

export const useGameStore = create<GameStore>((set) => ({
  // Initial state
  currentGameState: null,
  myColor: null,
  currentGameId: null,
  isSpectator: false,
  isAnalysisMode: false,
  isFreeMoveEnabled: false,
  isCustomDiceEnabled: false,
  selectedChecker: null,
  validDestinations: [],
  validSources: [],
  isBoardFlipped: false,
  chatMessages: [],
  showChat: false,

  // Actions
  setGameState: (state) =>
    set((prevState) => {
      let validSources: number[]

      // Check if free move is enabled in analysis mode
      if (state.isAnalysisMode && prevState.isFreeMoveEnabled) {
        // Free move mode: ALL points with checkers are draggable sources
        validSources = []

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

        // Bear-off (point 25) - allow dragging from bear-off
        if (state.whiteBornOff > 0 || state.redBornOff > 0) {
          validSources.push(25)
        }
      } else {
        // Regular mode or analysis with rules: only valid sources from game rules
        validSources =
          state.isYourTurn && state.validMoves
            ? Array.from(new Set(state.validMoves.map((move) => move.from)))
            : []
      }

      console.log('[GameStore] setGameState:', {
        isAnalysisMode: state.isAnalysisMode,
        isFreeMoveEnabled: prevState.isFreeMoveEnabled,
        isYourTurn: state.isYourTurn,
        validMovesCount: state.validMoves?.length || 0,
        validSources,
        dice: state.dice,
      })

      return {
        currentGameState: state,
        isAnalysisMode: state.isAnalysisMode,
        validSources,
      }
    }),

  updateTimeState: (timeUpdate) =>
    set((state) => ({
      currentGameState: state.currentGameState
        ? {
            ...state.currentGameState,
            whiteReserveSeconds: timeUpdate.whiteReserveSeconds,
            redReserveSeconds: timeUpdate.redReserveSeconds,
            whiteIsInDelay: timeUpdate.whiteIsInDelay,
            redIsInDelay: timeUpdate.redIsInDelay,
            whiteDelayRemaining: timeUpdate.whiteDelayRemaining,
            redDelayRemaining: timeUpdate.redDelayRemaining,
          }
        : null,
    })),

  setMyColor: (color) => set({ myColor: color }),

  setCurrentGameId: (gameId) => set({ currentGameId: gameId }),

  setIsSpectator: (isSpectator) => set({ isSpectator }),

  setFreeMoveEnabled: (enabled) =>
    set((state) => {
      // When toggling, recalculate validSources
      if (state.currentGameState) {
        const validSources = enabled
          ? calculateFreeMoveValidSources(state.currentGameState)
          : calculateRuleBasedValidSources(state.currentGameState)
        return { isFreeMoveEnabled: enabled, validSources }
      }
      return { isFreeMoveEnabled: enabled }
    }),

  setCustomDiceEnabled: (enabled) => set({ isCustomDiceEnabled: enabled }),

  selectChecker: (checker) => set({ selectedChecker: checker }),

  setValidDestinations: (destinations) => set({ validDestinations: destinations }),

  setValidSources: (sources) => set({ validSources: sources }),

  toggleBoardFlip: () => set((state) => ({ isBoardFlipped: !state.isBoardFlipped })),

  setBoardFlipped: (flipped) => set({ isBoardFlipped: flipped }),

  addChatMessage: (message) =>
    set((state) => ({
      chatMessages: [...state.chatMessages, message],
    })),

  clearChatMessages: () => set({ chatMessages: [] }),

  toggleChat: () => set((state) => ({ showChat: !state.showChat })),

  resetGame: () =>
    set({
      currentGameState: null,
      myColor: null,
      currentGameId: null,
      isSpectator: false,
      isAnalysisMode: false,
      isFreeMoveEnabled: false,
      isCustomDiceEnabled: false,
      selectedChecker: null,
      validDestinations: [],
      validSources: [],
      chatMessages: [],
      showChat: false,
      // Keep board flip preference
    }),
}))

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
