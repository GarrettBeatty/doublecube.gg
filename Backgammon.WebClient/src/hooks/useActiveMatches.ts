import { useState, useEffect, useCallback } from 'react'
import { useSignalR } from '@/contexts/SignalRContext'
import { ActiveMatch } from '@/types/home.types'

export const useActiveMatches = () => {
  const { hub, isConnected, connection } = useSignalR()
  const [matches, setMatches] = useState<ActiveMatch[]>([])
  const [isLoading, setIsLoading] = useState(true)
  const [error, setError] = useState<string | null>(null)

  const fetchMatches = useCallback(async () => {
    if (!isConnected || !hub) {
      setIsLoading(false)
      return
    }

    try {
      setIsLoading(true)
      setError(null)

      const activeMatches = (await hub.getActiveMatches(10)) as ActiveMatch[] | undefined
      setMatches(activeMatches || [])
    } catch (err) {
      console.warn('GetActiveMatches failed:', err)
      setError('Could not load active matches')
      setMatches([])
    } finally {
      setIsLoading(false)
    }
  }, [isConnected, hub])

  useEffect(() => {
    fetchMatches()

    // Refresh when match events occur
    const handleMatchCreated = () => {
      fetchMatches()
    }

    const handleMatchCompleted = () => {
      fetchMatches()
    }

    const handleMatchGameStarting = () => {
      fetchMatches()
    }

    connection?.on('MatchCreated', handleMatchCreated)
    connection?.on('MatchCompleted', handleMatchCompleted)
    connection?.on('MatchGameStarting', handleMatchGameStarting)

    // Auto-refresh every 30 seconds
    const interval = setInterval(fetchMatches, 30000)

    return () => {
      connection?.off('MatchCreated', handleMatchCreated)
      connection?.off('MatchCompleted', handleMatchCompleted)
      connection?.off('MatchGameStarting', handleMatchGameStarting)
      clearInterval(interval)
    }
  }, [connection, fetchMatches])

  return { matches, isLoading, error, refresh: fetchMatches }
}
