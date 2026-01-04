// Utility functions to map backend types to UI display types

import { CheckerColor } from '@/types/game.types'
import { MatchLobby } from '@/types/match.types'
import { LobbyGame } from '@/types/home.types'

/**
 * Map CheckerColor enum to UI display string
 */
export const mapCheckerColorToUI = (color: CheckerColor): 'White' | 'Red' => {
  return color === CheckerColor.White ? 'White' : 'Red'
}

/**
 * Map MatchLobby (backend) to LobbyGame (UI)
 */
export const mapMatchLobbyToLobbyGame = (lobby: MatchLobby): LobbyGame => {
  return {
    matchId: lobby.matchId,
    creatorUserId: lobby.creatorPlayerId,
    creatorUsername: lobby.creatorUsername || 'Unknown',
    creatorRating: undefined, // TODO: Backend should provide creator rating
    timeControl: undefined, // TODO: Add when backend supports time controls
    isRated: true, // TODO: Backend should provide rated status
    matchLength: lobby.targetScore,
    doublingCube: true, // Default assumption
  }
}

/**
 * Map friend status enum to UI display string
 */
export const mapFriendStatusToUI = (status: string): 'Online' | 'Playing' | 'Offline' => {
  const normalized = status.toLowerCase()
  if (normalized === 'online') return 'Online'
  if (normalized === 'playing' || normalized === 'ingame') return 'Playing'
  return 'Offline'
}

/**
 * Format rating for display (handle null/undefined)
 */
export const formatRating = (rating?: number | null): string => {
  if (rating === null || rating === undefined) return 'Unrated'
  return rating.toString()
}

/**
 * Format time control for display
 */
export const formatTimeControl = (seconds?: number): string => {
  if (!seconds) return 'No timer'
  if (seconds < 60) return `${seconds}s/move`
  return `${Math.floor(seconds / 60)}m/move`
}
