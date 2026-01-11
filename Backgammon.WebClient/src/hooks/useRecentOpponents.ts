import { useState, useEffect } from 'react'
import { useSignalR } from '@/contexts/SignalRContext'
import type { RecentOpponent } from '../types/home.types'

export const useRecentOpponents = (limit = 10, includeAi = false) => {
  const { hub, isConnected } = useSignalR()
  const [opponents, setOpponents] = useState<RecentOpponent[]>([])
  const [isLoading, setIsLoading] = useState(true)
  const [error, setError] = useState<string | null>(null)

  useEffect(() => {
    const fetchRecentOpponents = async () => {
      if (!isConnected || !hub) {
        setIsLoading(false)
        return
      }

      try {
        setIsLoading(true)
        setError(null)

        const recentOpponents = await hub.getRecentOpponents(limit, includeAi)
        // Map RecentOpponentDto to local RecentOpponent type
        setOpponents(
          (recentOpponents || []).map((o) => ({
            opponentId: o.opponentId,
            opponentName: o.opponentName,
            opponentRating: o.opponentRating,
            totalMatches: o.totalMatches,
            wins: o.wins,
            losses: o.losses,
            record: o.record,
            winRate: o.winRate,
            lastPlayedAt: typeof o.lastPlayedAt === 'string' ? o.lastPlayedAt : o.lastPlayedAt.toISOString(),
            isAi: o.isAi,
          }))
        )
      } catch (err) {
        console.error('Failed to fetch recent opponents:', err)
        setError('Failed to load recent opponents')
        setOpponents([])
      } finally {
        setIsLoading(false)
      }
    }

    fetchRecentOpponents()
  }, [isConnected, hub, limit, includeAi])

  return { opponents, isLoading, error }
}
