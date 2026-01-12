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

// Connection state
export enum ConnectionState {
  Disconnected = 'Disconnected',
  Connecting = 'Connecting',
  Connected = 'Connected',
  Reconnecting = 'Reconnecting',
  Failed = 'Failed',
}
