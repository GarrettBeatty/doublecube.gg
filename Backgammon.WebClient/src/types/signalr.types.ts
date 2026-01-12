import { GameState, CheckerColor } from './game.types'
import {
  MatchGameStartingDto,
  MatchCreatedDto,
  OpponentJoinedMatchDto,
  MatchUpdateDto,
  MatchContinuedDto,
  MatchGameCompletedDto,
  MatchCompletedDto,
  TimeUpdateDto,
  PlayerTimedOutDto,
} from './generated/Backgammon.Server.Models.SignalR'

// SignalR event names (server → client)
// Note: Hub methods are now accessed via the typed hub proxy (see SignalRContext.tsx)
export const HubEvents = {
  GameUpdate: 'GameUpdate',
  GameStart: 'GameStart',
  GameOver: 'GameOver',
  WaitingForOpponent: 'WaitingForOpponent',
  OpponentJoined: 'OpponentJoined',
  OpponentLeft: 'OpponentLeft',
  DoubleOffered: 'DoubleOffered',
  DoubleAccepted: 'DoubleAccepted',
  ReceiveChatMessage: 'ReceiveChatMessage',
  SpectatorJoined: 'SpectatorJoined',
  Error: 'Error',
  Info: 'Info',

  // Match events
  MatchCreated: 'MatchCreated',
  OpponentJoinedMatch: 'OpponentJoinedMatch',
  MatchGameStarting: 'MatchGameStarting',
  MatchUpdate: 'MatchUpdate',
  MatchContinued: 'MatchContinued',
  MatchStatus: 'MatchStatus',
  MatchGameCompleted: 'MatchGameCompleted',
  MatchCompleted: 'MatchCompleted',

  // Time control events
  TimeUpdate: 'TimeUpdate',
  PlayerTimedOut: 'PlayerTimedOut',

  // Correspondence events
  CorrespondenceMatchInvite: 'CorrespondenceMatchInvite',
  CorrespondenceTurnNotification: 'CorrespondenceTurnNotification',
  CorrespondenceLobbyCreated: 'CorrespondenceLobbyCreated',

  // Lobby events
  LobbyCreated: 'LobbyCreated',
} as const

// Event handler types
export interface SignalREventHandlers {
  onGameUpdate: (gameState: GameState) => void
  onGameStart: (gameState: GameState) => void
  onGameOver: (winner: CheckerColor, points: number, gameState: GameState) => void
  onWaitingForOpponent: (gameId: string) => void
  onOpponentJoined: (opponentId: string) => void
  onOpponentLeft: () => void
  onDoubleOffered: (currentStakes: number, newStakes: number) => void
  onDoubleAccepted: (gameState: GameState) => void
  onReceiveChatMessage: (senderName: string, message: string, senderConnectionId: string) => void
  onSpectatorJoined: (gameState: GameState) => void
  onError: (errorMessage: string) => void
  onInfo: (infoMessage: string) => void

  // Match events
  onMatchCreated: (matchData: MatchCreatedEvent) => void
  onOpponentJoinedMatch: (matchData: OpponentJoinedMatchEvent) => void
  onMatchGameStarting: (matchData: MatchGameStartingEvent) => void
  onMatchUpdate: (matchData: MatchUpdateEvent) => void
  onMatchContinued: (matchData: MatchContinuedEvent) => void
  onMatchStatus: (matchData: MatchUpdateEvent) => void
  onMatchGameCompleted: (matchData: MatchGameCompletedEvent) => void
  onMatchCompleted: (matchData: MatchCompletedEvent) => void
}

// Connection state
export enum ConnectionState {
  Disconnected = 'Disconnected',
  Connecting = 'Connecting',
  Connected = 'Connected',
  Reconnecting = 'Reconnecting',
  Failed = 'Failed',
}

// Event data types - Aliases to auto-generated DTOs
// Note: All event data types are now imported from auto-generated types
// We keep the "Event" suffix for backwards compatibility in the codebase

export type MatchCreatedEvent = MatchCreatedDto
export type OpponentJoinedMatchEvent = OpponentJoinedMatchDto
export type MatchGameStartingEvent = MatchGameStartingDto
export type MatchUpdateEvent = MatchUpdateDto
export type MatchContinuedEvent = MatchContinuedDto
export type MatchGameCompletedEvent = MatchGameCompletedDto
export type MatchCompletedEvent = MatchCompletedDto
export type TimeUpdateEvent = TimeUpdateDto
export type TimeoutEvent = PlayerTimedOutDto

// Legacy type for backwards compatibility
export type OpponentTypeString = 'AI' | 'OpenLobby' | 'Friend'
