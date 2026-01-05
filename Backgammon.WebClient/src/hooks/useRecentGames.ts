import { useState, useEffect } from 'react'
import { useSignalR } from '@/contexts/SignalRContext'
import type { RecentGame } from '../types/home.types'

export const useRecentGames = (limit = 10) => {
  const { invoke, isConnected } = useSignalR()
  const [games, setGames] = useState<RecentGame[]>([])
  const [isLoading, setIsLoading] = useState(true)
  const [error, setError] = useState<string | null>(null)

  useEffect(() => {
    const fetchRecentGames = async () => {
      if (!isConnected) {
        setIsLoading(false)
        return
      }

      try {
        setIsLoading(true)
        setError(null)

        const recentGames = await invoke<RecentGame[]>('GetRecentGames', limit)
        setGames(recentGames || [])
      } catch (err) {
        console.error('Failed to fetch recent games:', err)
        setError('Failed to load recent games')
        setGames([])
      } finally {
        setIsLoading(false)
      }
    }

    fetchRecentGames()
  }, [isConnected, invoke, limit])

  return { games, isLoading, error }
}
