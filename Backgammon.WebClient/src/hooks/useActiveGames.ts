import { useState, useEffect } from 'react'
import { useSignalR } from '@/contexts/SignalRContext'
import type { ActiveGame } from '../types/home.types'

export const useActiveGames = () => {
  const { invoke, isConnected } = useSignalR()
  const [games, setGames] = useState<ActiveGame[]>([])
  const [isLoading, setIsLoading] = useState(true)
  const [error, setError] = useState<string | null>(null)

  useEffect(() => {
    const fetchActiveGames = async () => {
      if (!isConnected) {
        setIsLoading(false)
        return
      }

      try {
        setIsLoading(true)
        setError(null)

        // Try to fetch active games from backend
        const activeGames = await invoke<ActiveGame[]>('GetActiveGames')
        setGames(activeGames || [])
      } catch (err) {
        console.warn('GetActiveGames not implemented yet:', err)
        setError('Active games temporarily unavailable')
        // Empty array as fallback
        setGames([])
      } finally {
        setIsLoading(false)
      }
    }

    fetchActiveGames()
  }, [isConnected, invoke])

  return { games, isLoading, error }
}
