import { create } from 'zustand'

interface MatchStore {
  currentMatchId: string | null
  setCurrentMatchId: (matchId: string | null) => void
}

export const useMatchStore = create<MatchStore>((set) => ({
  currentMatchId: null,
  setCurrentMatchId: (matchId) => set({ currentMatchId: matchId }),
}))
