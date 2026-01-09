import { useState, useEffect } from 'react'
import { useSignalR } from '@/contexts/SignalRContext'
import type { RecentOpponent } from '../types/home.types'

export const useRecentOpponents = (limit = 10, includeAi = false) => {
  const { invoke, isConnected } = useSignalR()
  const [opponents, setOpponents] = useState<RecentOpponent[]>([])
  const [isLoading, setIsLoading] = useState(true)
  const [error, setError] = useState<string | null>(null)

  useEffect(() => {
    const fetchRecentOpponents = async () => {
      if (!isConnected) {
        setIsLoading(false)
        return
      }

      try {
        setIsLoading(true)
        setError(null)

        const recentOpponents = await invoke<RecentOpponent[]>('GetRecentOpponents', limit, includeAi)
        setOpponents(recentOpponents || [])
      } catch (err) {
        console.error('Failed to fetch recent opponents:', err)
        setError('Failed to load recent opponents')
        setOpponents([])
      } finally {
        setIsLoading(false)
      }
    }

    fetchRecentOpponents()
  }, [isConnected, invoke, limit, includeAi])

  return { opponents, isLoading, error }
}
