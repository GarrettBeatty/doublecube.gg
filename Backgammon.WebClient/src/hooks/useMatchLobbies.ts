import { useState, useEffect } from 'react'
import { useSignalR } from '@/contexts/SignalRContext'
import { MatchLobby } from '@/types/match.types'

export const useMatchLobbies = () => {
  const { invoke, isConnected, connection } = useSignalR()
  const [lobbies, setLobbies] = useState<MatchLobby[]>([])
  const [isLoading, setIsLoading] = useState(true)
  const [error, setError] = useState<string | null>(null)

  useEffect(() => {
    const fetchLobbies = async () => {
      if (!isConnected) {
        setIsLoading(false)
        return
      }

      try {
        setIsLoading(true)
        setError(null)

        // Try to fetch match lobbies from backend
        const matchLobbies = await invoke<MatchLobby[]>('GetMatchLobbies')
        setLobbies(matchLobbies || [])
      } catch (err) {
        console.warn('GetMatchLobbies not implemented yet:', err)
        setError('Lobbies temporarily unavailable')
        // Empty array as fallback
        setLobbies([])
      } finally {
        setIsLoading(false)
      }
    }

    fetchLobbies()

    // Listen for real-time lobby updates (if backend supports it)
    const handleLobbyUpdate = (updatedLobbies: MatchLobby[]) => {
      setLobbies(updatedLobbies)
    }

    connection?.on('LobbyListUpdate', handleLobbyUpdate)

    return () => {
      connection?.off('LobbyListUpdate', handleLobbyUpdate)
    }
  }, [isConnected, invoke, connection])

  return { lobbies, isLoading, error }
}
