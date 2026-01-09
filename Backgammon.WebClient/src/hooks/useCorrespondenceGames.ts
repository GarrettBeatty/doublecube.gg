import { useState, useEffect, useCallback, useRef } from 'react'
import { useSignalR } from '@/contexts/SignalRContext'
import { HubMethods, HubEvents } from '@/types/signalr.types'
import {
  CorrespondenceGamesResponse,
  CorrespondenceGameDto,
  CorrespondenceMatchInvite,
  CorrespondenceTurnNotification,
} from '@/types/match.types'

export const useCorrespondenceGames = () => {
  const { invoke, isConnected, connection } = useSignalR()
  const [yourTurnGames, setYourTurnGames] = useState<CorrespondenceGameDto[]>([])
  const [waitingGames, setWaitingGames] = useState<CorrespondenceGameDto[]>([])
  const [myLobbies, setMyLobbies] = useState<CorrespondenceGameDto[]>([])
  const [isLoading, setIsLoading] = useState(true)
  const [error, setError] = useState<string | null>(null)

  const fetchCorrespondenceGames = useCallback(async () => {
    if (!isConnected) {
      setIsLoading(false)
      return
    }

    try {
      setIsLoading(true)
      setError(null)

      const response = await invoke<CorrespondenceGamesResponse>(
        HubMethods.GetCorrespondenceGames
      )

      if (response) {
        setYourTurnGames(response.yourTurnGames || [])
        setWaitingGames(response.waitingGames || [])
        setMyLobbies(response.myLobbies || [])
      }
    } catch (err) {
      console.error('Error fetching correspondence games:', err)
      setError('Failed to load correspondence games')
      setYourTurnGames([])
      setWaitingGames([])
      setMyLobbies([])
    } finally {
      setIsLoading(false)
    }
  }, [isConnected, invoke])

  // Use ref to avoid re-registering event handlers
  const fetchRef = useRef(fetchCorrespondenceGames)
  fetchRef.current = fetchCorrespondenceGames

  useEffect(() => {
    fetchCorrespondenceGames()

    // Listen for correspondence match invites
    const handleMatchInvite = (invite: CorrespondenceMatchInvite) => {
      console.log('Received correspondence match invite:', invite)
      // Refetch to include the new match
      fetchRef.current()
    }

    // Listen for turn notifications (when it becomes your turn)
    const handleTurnNotification = (notification: CorrespondenceTurnNotification) => {
      console.log('Correspondence turn notification:', notification)
      // Refetch to update the lists
      fetchRef.current()
    }

    connection?.on(HubEvents.CorrespondenceMatchInvite, handleMatchInvite)
    connection?.on(HubEvents.CorrespondenceTurnNotification, handleTurnNotification)

    return () => {
      connection?.off(HubEvents.CorrespondenceMatchInvite, handleMatchInvite)
      connection?.off(HubEvents.CorrespondenceTurnNotification, handleTurnNotification)
    }
  }, [connection, fetchCorrespondenceGames])

  const createCorrespondenceMatch = useCallback(
    async (config: {
      opponentType: 'Friend' | 'OpenLobby'
      targetScore: number
      timePerMoveDays: number
      friendUserId?: string
      isRated?: boolean
    }) => {
      if (!isConnected) {
        throw new Error('Not connected to server')
      }

      try {
        await invoke(HubMethods.CreateCorrespondenceMatch, {
          OpponentType: config.opponentType,
          TargetScore: config.targetScore,
          TimePerMoveDays: config.timePerMoveDays,
          OpponentId: config.friendUserId,
          IsCorrespondence: true,
          IsRated: config.isRated ?? true,
        })
      } catch (err) {
        console.error('Error creating correspondence match:', err)
        throw err
      }
    },
    [isConnected, invoke]
  )

  const refresh = useCallback(() => {
    fetchCorrespondenceGames()
  }, [fetchCorrespondenceGames])

  return {
    yourTurnGames,
    waitingGames,
    myLobbies,
    totalYourTurn: yourTurnGames.length,
    totalWaiting: waitingGames.length,
    totalMyLobbies: myLobbies.length,
    isLoading,
    error,
    createCorrespondenceMatch,
    refresh,
  }
}
