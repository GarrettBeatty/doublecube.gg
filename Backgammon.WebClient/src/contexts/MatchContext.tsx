/* eslint-disable react-refresh/only-export-components */
import React, { createContext, useContext, useState, ReactNode } from 'react'
import { Match } from '@/types/game.types'
import { MatchLobby } from '@/types/match.types'

interface MatchContextType {
  currentMatch: Match | null
  currentMatchLobby: MatchLobby | null
  setCurrentMatch: (match: Match | null) => void
  setCurrentMatchLobby: (lobby: MatchLobby | null) => void
}

const MatchContext = createContext<MatchContextType | undefined>(undefined)

export const useMatch = () => {
  const context = useContext(MatchContext)
  if (!context) {
    throw new Error('useMatch must be used within a MatchProvider')
  }
  return context
}

interface MatchProviderProps {
  children: ReactNode
}

export const MatchProvider: React.FC<MatchProviderProps> = ({ children }) => {
  const [currentMatch, setCurrentMatch] = useState<Match | null>(null)
  const [currentMatchLobby, setCurrentMatchLobby] = useState<MatchLobby | null>(null)

  const value: MatchContextType = {
    currentMatch,
    currentMatchLobby,
    setCurrentMatch,
    setCurrentMatchLobby,
  }

  return <MatchContext.Provider value={value}>{children}</MatchContext.Provider>
}
