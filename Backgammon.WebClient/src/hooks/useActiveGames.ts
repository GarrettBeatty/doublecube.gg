import { useState, useEffect, useCallback } from 'react'
import { useSignalR } from '@/contexts/SignalRContext'
import { ActiveGame } from '@/types/home.types'

export const useActiveGames = () => {
  const { hub, isConnected, connection } = useSignalR()
  const [games, setGames] = useState<ActiveGame[]>([])
  const [isLoading, setIsLoading] = useState(true)
  const [error, setError] = useState<string | null>(null)

  const fetchGames = useCallback(async () => {
    if (!isConnected || !hub) {
      setIsLoading(false)
      return
    }

    try {
      setIsLoading(true)
      setError(null)

      const activeGames = await hub.getActiveGames(10) as ActiveGame[] | undefined
      setGames(activeGames || [])
    } catch (err) {
      console.warn('GetActiveGames failed:', err)
      setError('Could not load active games')
      setGames([])
    } finally {
      setIsLoading(false)
    }
  }, [isConnected, hub])

  useEffect(() => {
    fetchGames()

    // Refresh when a game starts or ends
    const handleGameStart = () => {
      fetchGames()
    }

    const handleGameOver = () => {
      fetchGames()
    }

    const handleMatchGameStarting = () => {
      fetchGames()
    }

    connection?.on('GameStart', handleGameStart)
    connection?.on('GameOver', handleGameOver)
    connection?.on('MatchGameStarting', handleMatchGameStarting)

    // Auto-refresh every 30 seconds
    const interval = setInterval(fetchGames, 30000)

    return () => {
      connection?.off('GameStart', handleGameStart)
      connection?.off('GameOver', handleGameOver)
      connection?.off('MatchGameStarting', handleMatchGameStarting)
      clearInterval(interval)
    }
  }, [connection, fetchGames])

  const yourTurnCount = games.filter(g => g.isYourTurn).length

  return { games, isLoading, error, refresh: fetchGames, yourTurnCount }
}
