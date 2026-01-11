import { useState, useEffect, useCallback } from 'react'
import { useSignalR } from '@/contexts/SignalRContext'
import { MatchLobby } from '@/types/match.types'
import { HubEvents } from '@/types/signalr.types'

export const useMatchLobbies = () => {
  const { hub, isConnected, connection } = useSignalR()
  const [lobbies, setLobbies] = useState<MatchLobby[]>([])
  const [isLoading, setIsLoading] = useState(true)
  const [error, setError] = useState<string | null>(null)

  const fetchLobbies = useCallback(async () => {
    if (!isConnected || !hub) {
      setIsLoading(false)
      return
    }

    try {
      setIsLoading(true)
      setError(null)

      // Fetch match lobbies from backend (regular lobbies only)
      const matchLobbies = await hub.getMatchLobbies('regular')
      // Map MatchLobbyDto to local MatchLobby type
      setLobbies(
        (matchLobbies || []).map((m) => ({
          matchId: m.matchId,
          creatorPlayerId: m.creatorPlayerId,
          creatorUsername: m.creatorUsername,
          opponentType: m.opponentType,
          targetScore: m.targetScore,
          status: m.status,
          opponentPlayerId: m.opponentPlayerId ?? null,
          opponentPlayerName: m.opponentUsername ?? null,
          createdAt: m.createdAt,
          isOpenLobby: m.isOpenLobby,
          isCorrespondence: m.isCorrespondence,
          timePerMoveDays: m.timePerMoveDays,
        }))
      )
    } catch (err) {
      console.warn('GetMatchLobbies not implemented yet:', err)
      setError('Lobbies temporarily unavailable')
      setLobbies([])
    } finally {
      setIsLoading(false)
    }
  }, [isConnected, hub])

  useEffect(() => {
    fetchLobbies()

    // Listen for real-time lobby updates (if backend supports it)
    const handleLobbyUpdate = (updatedLobbies: MatchLobby[]) => {
      setLobbies(updatedLobbies)
    }

    // Listen for new lobbies being created
    const handleLobbyCreated = () => {
      console.log('New lobby created - refreshing list')
      fetchLobbies()
    }

    connection?.on('LobbyListUpdate', handleLobbyUpdate)
    connection?.on(HubEvents.LobbyCreated, handleLobbyCreated)

    return () => {
      connection?.off('LobbyListUpdate', handleLobbyUpdate)
      connection?.off(HubEvents.LobbyCreated, handleLobbyCreated)
    }
  }, [connection, fetchLobbies])

  return { lobbies, isLoading, error }
}
