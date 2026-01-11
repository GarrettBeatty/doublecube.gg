import { useState, useEffect } from 'react'
import { useSignalR } from '@/contexts/SignalRContext'
import type { RecentGame } from '../types/home.types'

export const useRecentGames = (limit = 10) => {
  const { hub, isConnected } = useSignalR()
  const [games, setGames] = useState<RecentGame[]>([])
  const [isLoading, setIsLoading] = useState(true)
  const [error, setError] = useState<string | null>(null)

  useEffect(() => {
    const fetchRecentGames = async () => {
      if (!isConnected || !hub) {
        setIsLoading(false)
        return
      }

      try {
        setIsLoading(true)
        setError(null)

        const recentGames = await hub.getRecentGames(limit)
        // Map RecentGameDto to local RecentGame type
        setGames(
          (recentGames || []).map((g) => ({
            matchId: g.matchId,
            opponentId: g.opponentId ?? '',
            opponentName: g.opponentName,
            opponentRating: g.opponentRating,
            result: g.result as RecentGame['result'],
            myScore: g.myScore,
            opponentScore: g.opponentScore,
            matchScore: g.matchScore,
            targetScore: g.targetScore,
            matchLength: g.matchLength,
            timeControl: g.timeControl,
            ratingChange: g.ratingChange,
            completedAt: typeof g.completedAt === 'string' ? g.completedAt : g.completedAt?.toISOString() ?? '',
            createdAt: typeof g.createdAt === 'string' ? g.createdAt : g.createdAt.toISOString(),
          }))
        )
      } catch (err) {
        console.error('Failed to fetch recent games:', err)
        setError('Failed to load recent games')
        setGames([])
      } finally {
        setIsLoading(false)
      }
    }

    fetchRecentGames()
  }, [isConnected, hub, limit])

  return { games, isLoading, error }
}
