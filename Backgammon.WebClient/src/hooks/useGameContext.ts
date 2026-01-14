import { useParams, useLocation } from 'react-router-dom'
import { useMemo } from 'react'

interface GameContext {
  matchId: string | null
  gameId: string | null
  isOnGamePage: boolean
  isOnMatchPage: boolean
  isOnAnalysisPage: boolean
}

/**
 * Hook to get the current game/match context from React Router.
 * Replaces manual URL parsing (window.location.pathname) for more reliable routing.
 */
export function useGameContext(): GameContext {
  const params = useParams<{ matchId?: string; gameId?: string }>()
  const location = useLocation()

  return useMemo(() => {
    const pathname = location.pathname

    return {
      matchId: params.matchId ?? null,
      gameId: params.gameId ?? null,
      isOnGamePage: pathname.includes('/game/'),
      isOnMatchPage: pathname.includes('/match/'),
      isOnAnalysisPage: pathname.startsWith('/analysis'),
    }
  }, [params.matchId, params.gameId, location.pathname])
}

/**
 * Helper to parse game context from a pathname string.
 * Use this when you can't use the hook (e.g., in callbacks with stale closures).
 */
export function parseGameContextFromPath(pathname: string): GameContext {
  const isOnGamePage = pathname.includes('/game/')
  const isOnMatchPage = pathname.includes('/match/')
  const isOnAnalysisPage = pathname.startsWith('/analysis')

  let matchId: string | null = null
  let gameId: string | null = null

  if (isOnMatchPage) {
    const matchMatch = pathname.match(/\/match\/([^/]+)/)
    if (matchMatch) {
      matchId = matchMatch[1]
    }
  }

  if (isOnGamePage) {
    const gameMatch = pathname.match(/\/game\/([^/]+)/)
    if (gameMatch) {
      gameId = gameMatch[1]
    }
  }

  return {
    matchId,
    gameId,
    isOnGamePage,
    isOnMatchPage,
    isOnAnalysisPage,
  }
}
