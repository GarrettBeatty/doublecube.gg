/**
 * SignalR event hooks - split by domain concern
 *
 * Individual hooks can be imported directly for granular control:
 * - useGameStateEvents - GameUpdate, GameStart, GameOver, SpectatorJoined
 * - useMatchEvents - MatchCreated, MatchUpdate, OpponentJoinedMatch
 * - useDoubleEvents - DoubleOffered, DoubleAccepted
 * - useTimeEvents - TimeUpdate, PlayerTimedOut
 * - useChatEvents - ReceiveChatMessage
 * - useConnectionEvents - WaitingForOpponent, OpponentJoined, OpponentLeft, Error, Info
 * - useGameAudio - Sound effect utilities
 */

export { useGameStateEvents } from './useGameStateEvents'
export { useMatchEvents } from './useMatchEvents'
export { useDoubleEvents } from './useDoubleEvents'
export { useTimeEvents } from './useTimeEvents'
export { useChatEvents } from './useChatEvents'
export { useConnectionEvents } from './useConnectionEvents'
export { useGameAudio } from './useGameAudio'
