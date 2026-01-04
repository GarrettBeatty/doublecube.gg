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
  setMyColor: (color: CheckerColor | null) => void
  setCurrentGameId: (gameId: string | null) => void
  setIsSpectator: (isSpectator: boolean) => void
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
  selectedChecker: null,
  validDestinations: [],
  validSources: [],
  isBoardFlipped: false,
  chatMessages: [],
  showChat: false,

  // Actions
  setGameState: (state) => {
    // Compute valid sources from valid moves - ONLY on player's turn
    const validSources =
      state.isYourTurn && state.validMoves
        ? Array.from(new Set(state.validMoves.map((move) => move.from)))
        : []

    console.log('[GameStore] setGameState:', {
      isYourTurn: state.isYourTurn,
      validMovesCount: state.validMoves?.length || 0,
      validSources,
      dice: state.dice,
    })

    set({
      currentGameState: state,
      isAnalysisMode: state.isAnalysisMode,
      validSources,
    })
  },

  setMyColor: (color) => set({ myColor: color }),

  setCurrentGameId: (gameId) => set({ currentGameId: gameId }),

  setIsSpectator: (isSpectator) => set({ isSpectator }),

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
      selectedChecker: null,
      validDestinations: [],
      validSources: [],
      chatMessages: [],
      showChat: false,
      // Keep board flip preference
    }),
}))
