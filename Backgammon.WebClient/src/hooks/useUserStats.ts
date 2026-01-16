import { useState, useEffect } from 'react'
import { useAuth } from '@/contexts/AuthContext'
import { useSignalR } from '@/contexts/SignalRContext'
import type { UserStats } from '../types/home.types'

export const useUserStats = () => {
  const { user } = useAuth()
  const { hub, isConnected } = useSignalR()
  const [stats, setStats] = useState<UserStats | null>(null)
  const [isLoading, setIsLoading] = useState(true)
  const [error, setError] = useState<string | null>(null)

  useEffect(() => {
    const fetchStats = async () => {
      if (!isConnected || !user || !hub) {
        setIsLoading(false)
        setStats(null)
        return
      }

      try {
        setIsLoading(true)
        setError(null)

        // Fetch user profile stats from backend
        const profile = await hub.getPlayerProfile(user.username)
        if (profile && profile.stats) {
          const s = profile.stats
          setStats({
            rating: profile.rating,
            peakRating: profile.peakRating,
            wins: s.wins,
            losses: s.losses,
            winRate: s.wins + s.losses > 0
              ? (s.wins / (s.wins + s.losses)) * 100
              : 0,
            currentStreak: s.winStreak,
            streakType: s.winStreak >= 0 ? 'win' : 'loss',
            gamesToday: 0,
            gamesThisWeek: 0,
          })
        }
      } catch (err) {
        console.warn('GetPlayerProfile not implemented yet, using placeholder data:', err)
        setError('Stats temporarily unavailable')

        // Use mock data as fallback
        setStats({
          rating: 1500,
          peakRating: 1500,
          wins: 0,
          losses: 0,
          winRate: 0,
          currentStreak: 0,
          streakType: 'win',
          gamesToday: 0,
          gamesThisWeek: 0,
        })
      } finally {
        setIsLoading(false)
      }
    }

    fetchStats()
  }, [isConnected, user, hub])

  return { stats, isLoading, error }
}
