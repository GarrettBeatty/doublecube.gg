import { useState, useEffect } from 'react'
import { useAuth } from '@/contexts/AuthContext'
import { useSignalR } from '@/contexts/SignalRContext'
import type { UserStats } from '../types/home.types'

export const useUserStats = () => {
  const { user } = useAuth()
  const { invoke, isConnected } = useSignalR()
  const [stats, setStats] = useState<UserStats | null>(null)
  const [isLoading, setIsLoading] = useState(true)
  const [error, setError] = useState<string | null>(null)

  useEffect(() => {
    const fetchStats = async () => {
      if (!isConnected || !user) {
        setIsLoading(false)
        setStats(null)
        return
      }

      try {
        setIsLoading(true)
        setError(null)

        // Try to fetch user profile stats from backend
        const userStats = await invoke<UserStats>('GetUserProfile', user.userId)
        setStats(userStats)
      } catch (err) {
        console.warn('GetUserProfile not implemented yet, using placeholder data:', err)
        setError('Stats temporarily unavailable')

        // Use mock data as fallback
        setStats({
          rating: 1500,
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
  }, [isConnected, user, invoke])

  return { stats, isLoading, error }
}
