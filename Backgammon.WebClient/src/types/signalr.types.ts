import { GameState, CheckerColor } from './game.types'

// SignalR Hub method names (client → server)
export const HubMethods = {
  // Game actions
  CreateGame: 'CreateGame',
  CreateAiGame: 'CreateAiGame',
  JoinGame: 'JoinGame',
  GetGameState: 'GetGameState',
  SpectateGame: 'SpectateGame',
  RollDice: 'RollDice',
  MakeMove: 'MakeMove',
  EndTurn: 'EndTurn',
  UndoLastMove: 'UndoLastMove',
  OfferDouble: 'OfferDouble',
  RespondToDouble: 'RespondToDouble',
  AbandonGame: 'AbandonGame',
  LeaveGame: 'LeaveGame',

  // Match actions
  CreateMatch: 'CreateMatch',
  JoinMatch: 'JoinMatch',
  ContinueMatch: 'ContinueMatch',
  GetMatchStatus: 'GetMatchStatus',
  GetMatchLobbies: 'GetMatchLobbies',

  // Correspondence actions
  GetCorrespondenceGames: 'GetCorrespondenceGames',
  CreateCorrespondenceMatch: 'CreateCorrespondenceMatch',
  NotifyCorrespondenceTurnComplete: 'NotifyCorrespondenceTurnComplete',

  // Chat
  SendChatMessage: 'SendChatMessage',

  // Analysis mode
  CreateAnalysisGame: 'CreateAnalysisGame',
  ImportPosition: 'ImportPosition',
  ExportPosition: 'ExportPosition',
  SetDice: 'SetDice',
  MoveCheckerDirectly: 'MoveCheckerDirectly',
  SetCurrentPlayer: 'SetCurrentPlayer',
  AnalyzePosition: 'AnalyzePosition',
  FindBestMoves: 'FindBestMoves',
} as const

// SignalR event names (server → client)
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

// Match event data types
export type OpponentTypeString = 'AI' | 'OpenLobby' | 'Friend'

export interface MatchCreatedEvent {
  matchId: string
  gameId: string
  targetScore: number
  opponentType: OpponentTypeString
  player1Id: string
  player2Id: string | null
  player1Name: string
  player2Name: string | null
}

export interface OpponentJoinedMatchEvent {
  matchId: string
  player2Id: string
  player2Name: string
}

// Time control event data types
export interface TimeUpdateEvent {
  gameId: string
  whiteReserveSeconds: number
  redReserveSeconds: number
  whiteIsInDelay: boolean
  redIsInDelay: boolean
  whiteDelayRemaining: number
  redDelayRemaining: number
}

export interface TimeoutEvent {
  gameId: string
  timedOutPlayer: string
  winner: string
}

// Match event data types
export interface MatchGameStartingEvent {
  matchId: string
  gameId: string
}

export interface MatchUpdateEvent {
  matchId: string
  player1Score: number
  player2Score: number
  targetScore: number
  isCrawfordGame: boolean
  matchComplete: boolean
  matchWinner: string | null
}

export interface MatchContinuedEvent {
  matchId: string
  gameId: string
  player1Score: number
  player2Score: number
  targetScore: number
  isCrawfordGame: boolean
}

export interface MatchGameCompletedEvent {
  matchId: string
  gameId: string
  winner: string
  points: number
}

export interface MatchCompletedEvent {
  matchId: string
  winner: string
  finalScore: {
    player1: number
    player2: number
  }
}
