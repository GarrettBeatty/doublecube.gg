import { create } from 'zustand'
import { CheckerColor } from '@/types/generated/Backgammon.Core'

export interface MatchState {
  matchId: string
  player1Score: number
  player2Score: number
  targetScore: number
  isCrawfordGame: boolean
  matchComplete: boolean
  matchWinner: string | null
  lastUpdatedAt?: string // ISO timestamp for staleness detection
}

interface MatchStore {
  // Match state
  matchState: MatchState | null
  showGameResultModal: boolean
  lastGameWinner: CheckerColor | null
  lastGamePoints: number

  // Legacy: kept for backward compatibility during migration
  currentMatchId: string | null

  // Actions
  setMatchState: (
    matchState: MatchState | null | ((prev: MatchState | null) => MatchState | null)
  ) => void
  setShowGameResultModal: (show: boolean) => void
  setLastGameResult: (winner: CheckerColor | null, points: number) => void
  setCurrentMatchId: (matchId: string | null) => void
  resetMatchState: () => void
}

export const useMatchStore = create<MatchStore>((set) => ({
  // Initial state
  matchState: null,
  showGameResultModal: false,
  lastGameWinner: null,
  lastGamePoints: 0,
  currentMatchId: null,

  // Actions
  setMatchState: (matchStateOrUpdater) =>
    set((state) => ({
      matchState:
        typeof matchStateOrUpdater === 'function'
          ? matchStateOrUpdater(state.matchState)
          : matchStateOrUpdater,
    })),

  setShowGameResultModal: (show) => set({ showGameResultModal: show }),

  setLastGameResult: (winner, points) =>
    set({ lastGameWinner: winner, lastGamePoints: points }),

  setCurrentMatchId: (matchId) => set({ currentMatchId: matchId }),

  resetMatchState: () =>
    set({
      matchState: null,
      showGameResultModal: false,
      lastGameWinner: null,
      lastGamePoints: 0,
      currentMatchId: null,
    }),
}))
