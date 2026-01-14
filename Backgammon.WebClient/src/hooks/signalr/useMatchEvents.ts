import { useEffect, useRef } from 'react'
import { useNavigate } from 'react-router-dom'
import type { HubConnection } from '@microsoft/signalr'
import { HubEvents } from '@/types/signalr.types'
import { useGameStore } from '@/stores/gameStore'
import {
  MatchCreatedDto,
  MatchUpdateDto,
  OpponentJoinedMatchDto,
} from '@/types/generated/Backgammon.Server.Models.SignalR'

/**
 * Hook to handle match events: MatchCreated, MatchUpdate, OpponentJoinedMatch
 */
export function useMatchEvents(connection: HubConnection | null) {
  const navigate = useNavigate()
  const navigateRef = useRef(navigate)

  const { setCurrentGameId, setMatchState } = useGameStore()

  // Keep navigateRef in sync with navigate
  useEffect(() => {
    navigateRef.current = navigate
  }, [navigate])

  useEffect(() => {
    if (!connection) return

    // MatchCreated - Match and first game created, navigate to game page
    const handleMatchCreated = (data: MatchCreatedDto) => {
      console.log('[SignalR] MatchCreated', { matchId: data.matchId, gameId: data.gameId, opponentType: data.opponentType })
      setCurrentGameId(data.gameId)
      // Navigate to game page immediately - game handles waiting state
      navigateRef.current(`/match/${data.matchId}/game/${data.gameId}`, { replace: true })
    }

    // OpponentJoinedMatch - Opponent joined the match
    const handleOpponentJoinedMatch = (data: OpponentJoinedMatchDto) => {
      console.log('[SignalR] OpponentJoinedMatch', { matchId: data.matchId, player2Name: data.player2Name })
      // Game page will receive WaitingForOpponent → GameStart flow
    }

    // MatchUpdate - Match score/state updated
    const handleMatchUpdate = (data: MatchUpdateDto) => {
      console.log('[SignalR] MatchUpdate', data)

      setMatchState({
        matchId: data.matchId,
        player1Score: data.player1Score,
        player2Score: data.player2Score,
        targetScore: data.targetScore,
        isCrawfordGame: data.isCrawfordGame,
        matchComplete: data.matchComplete,
        matchWinner: data.matchWinner ?? null,
        lastUpdatedAt: new Date().toISOString(),
      })
    }

    // Register handlers
    connection.on(HubEvents.MatchCreated, handleMatchCreated)
    connection.on(HubEvents.OpponentJoinedMatch, handleOpponentJoinedMatch)
    connection.on(HubEvents.MatchUpdate, handleMatchUpdate)

    return () => {
      connection.off(HubEvents.MatchCreated)
      connection.off(HubEvents.OpponentJoinedMatch)
      connection.off(HubEvents.MatchUpdate)
    }
  }, [connection, setCurrentGameId, setMatchState])
}
