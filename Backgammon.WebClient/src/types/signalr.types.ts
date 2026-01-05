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
  onMatchUpdate: (matchData: MatchData) => void
  onMatchContinued: (matchData: MatchData) => void
  onMatchStatus: (matchData: MatchData) => void
  onMatchGameCompleted: (matchData: MatchData) => void
  onMatchCompleted: (matchData: MatchData) => void
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
export interface MatchCreatedEvent {
  matchId: string
  gameId: string
  targetScore: number
  opponentType: 'AI' | 'OpenLobby' | 'Friend'
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

// Generic match event data
export interface MatchData {
  matchId: string
  [key: string]: unknown
}
