import { create } from 'zustand'
import type { GameState } from '@/types/generated/Backgammon.Server.Models'
import { CheckerColor } from '@/types/generated/Backgammon.Core'

export interface DoublingCubeState {
  value: number
  owner: CheckerColor | null
  canDouble: boolean
  pendingOffer: boolean
  pendingResponse: boolean
  offerFrom: CheckerColor | null
  newValue: number | null
}

interface GameStateStore {
  // Core game state from server
  currentGameState: GameState | null
  myColor: CheckerColor | null
  currentGameId: string | null
  isSpectator: boolean
  isAnalysisMode: boolean

  // Doubling cube state
  doublingCube: DoublingCubeState

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
  setDoublingCubeState: (state: Partial<DoublingCubeState>) => void
  setPendingDoubleOffer: (from: CheckerColor, newValue: number) => void
  clearPendingDoubleOffer: () => void
  resetGameState: () => void
}

const initialDoublingCube: DoublingCubeState = {
  value: 1,
  owner: null,
  canDouble: false,
  pendingOffer: false,
  pendingResponse: false,
  offerFrom: null,
  newValue: null,
}

export const useGameStateStore = create<GameStateStore>((set) => ({
  // Initial state
  currentGameState: null,
  myColor: null,
  currentGameId: null,
  isSpectator: false,
  isAnalysisMode: false,
  doublingCube: { ...initialDoublingCube },

  // Actions
  setGameState: (state) =>
    set((prevState) => {
      // Parse doublingCubeOwner from string to CheckerColor enum
      let cubeOwner: CheckerColor | null = null
      if (state.doublingCubeOwner === 'White') {
        cubeOwner = CheckerColor.White
      } else if (state.doublingCubeOwner === 'Red') {
        cubeOwner = CheckerColor.Red
      }

      return {
        currentGameState: state,
        isAnalysisMode: state.isAnalysisMode,
        doublingCube: {
          value: state.doublingCubeValue,
          owner: cubeOwner,
          canDouble: state.canDouble,
          pendingOffer: prevState.doublingCube.pendingOffer,
          pendingResponse: prevState.doublingCube.pendingResponse,
          offerFrom: prevState.doublingCube.offerFrom,
          newValue: prevState.doublingCube.newValue,
        },
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

  setDoublingCubeState: (state) =>
    set((prev) => ({
      doublingCube: { ...prev.doublingCube, ...state },
    })),

  setPendingDoubleOffer: (from, newValue) =>
    set((prev) => ({
      doublingCube: {
        ...prev.doublingCube,
        pendingOffer: false,
        pendingResponse: true,
        offerFrom: from,
        newValue,
      },
    })),

  clearPendingDoubleOffer: () =>
    set((prev) => ({
      doublingCube: {
        ...prev.doublingCube,
        pendingOffer: false,
        pendingResponse: false,
        offerFrom: null,
        newValue: null,
      },
    })),

  resetGameState: () =>
    set({
      currentGameState: null,
      myColor: null,
      currentGameId: null,
      isSpectator: false,
      isAnalysisMode: false,
      doublingCube: { ...initialDoublingCube },
    }),
}))
