import { useState, useEffect, useCallback } from 'react'
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
      }
    } catch (err) {
      console.error('Error fetching correspondence games:', err)
      setError('Failed to load correspondence games')
      setYourTurnGames([])
      setWaitingGames([])
    } finally {
      setIsLoading(false)
    }
  }, [isConnected, invoke])

  useEffect(() => {
    fetchCorrespondenceGames()

    // Listen for correspondence match invites
    const handleMatchInvite = (invite: CorrespondenceMatchInvite) => {
      console.log('Received correspondence match invite:', invite)
      // Refetch to include the new match
      fetchCorrespondenceGames()
    }

    // Listen for turn notifications (when it becomes your turn)
    const handleTurnNotification = (notification: CorrespondenceTurnNotification) => {
      console.log('Correspondence turn notification:', notification)
      // Refetch to update the lists
      fetchCorrespondenceGames()
    }

    connection?.on(HubEvents.CorrespondenceMatchInvite, handleMatchInvite)
    connection?.on(HubEvents.CorrespondenceTurnNotification, handleTurnNotification)

    return () => {
      connection?.off(HubEvents.CorrespondenceMatchInvite, handleMatchInvite)
      connection?.off(HubEvents.CorrespondenceTurnNotification, handleTurnNotification)
    }
  }, [fetchCorrespondenceGames, connection])

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
    totalYourTurn: yourTurnGames.length,
    totalWaiting: waitingGames.length,
    isLoading,
    error,
    createCorrespondenceMatch,
    refresh,
  }
}
