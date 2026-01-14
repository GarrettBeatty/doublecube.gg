import { create } from 'zustand'
import { PositionEvaluation, BestMovesAnalysis } from '@/types/analysis.types'
import { Move } from '@/types/game.types'

interface AnalysisStore {
  // Analysis mode toggles
  isFreeMoveEnabled: boolean
  isCustomDiceEnabled: boolean

  // Analysis state
  currentEvaluation: PositionEvaluation | null
  bestMoves: BestMovesAnalysis | null
  isAnalyzing: boolean
  highlightedMoves: Move[]

  // Actions
  setFreeMoveEnabled: (enabled: boolean) => void
  setCustomDiceEnabled: (enabled: boolean) => void
  setCurrentEvaluation: (evaluation: PositionEvaluation | null) => void
  setBestMoves: (analysis: BestMovesAnalysis | null) => void
  setIsAnalyzing: (analyzing: boolean) => void
  setHighlightedMoves: (moves: Move[]) => void
  clearAnalysis: () => void
  resetAnalysisState: () => void
}

export const useAnalysisStore = create<AnalysisStore>((set) => ({
  // Initial state
  isFreeMoveEnabled: false,
  isCustomDiceEnabled: false,
  currentEvaluation: null,
  bestMoves: null,
  isAnalyzing: false,
  highlightedMoves: [],

  // Actions
  setFreeMoveEnabled: (enabled) => set({ isFreeMoveEnabled: enabled }),

  setCustomDiceEnabled: (enabled) => set({ isCustomDiceEnabled: enabled }),

  setCurrentEvaluation: (evaluation) => set({ currentEvaluation: evaluation }),

  setBestMoves: (analysis) => set({ bestMoves: analysis }),

  setIsAnalyzing: (analyzing) => set({ isAnalyzing: analyzing }),

  setHighlightedMoves: (moves) => set({ highlightedMoves: moves }),

  clearAnalysis: () =>
    set({
      currentEvaluation: null,
      bestMoves: null,
      isAnalyzing: false,
      highlightedMoves: [],
    }),

  resetAnalysisState: () =>
    set({
      isFreeMoveEnabled: false,
      isCustomDiceEnabled: false,
      currentEvaluation: null,
      bestMoves: null,
      isAnalyzing: false,
      highlightedMoves: [],
    }),
}))
