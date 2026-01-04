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
  CreateMatchWithConfig: 'CreateMatchWithConfig',
  CreateMatchLobby: 'CreateMatchLobby',
  JoinMatchLobby: 'JoinMatchLobby',
  StartMatchFromLobby: 'StartMatchFromLobby',
  LeaveMatchLobby: 'LeaveMatchLobby',

  // Chat
  SendChatMessage: 'SendChatMessage',

  // Analysis mode
  CreateAnalysisGame: 'CreateAnalysisGame',
  ImportPosition: 'ImportPosition',
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
  MatchLobbyCreated: 'MatchLobbyCreated',
  MatchGameStarting: 'MatchGameStarting',
  MatchLobbyUpdate: 'MatchLobbyUpdate',
  MatchStarted: 'MatchStarted',
  MatchGameCompleted: 'MatchGameCompleted',
  MatchCompleted: 'MatchCompleted',
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
  onMatchLobbyUpdate: (matchLobby: any) => void
  onMatchStarted: (matchId: string) => void
  onMatchGameCompleted: (matchData: any) => void
  onMatchCompleted: (matchData: any) => void
}

// Connection state
export enum ConnectionState {
  Disconnected = 'Disconnected',
  Connecting = 'Connecting',
  Connected = 'Connected',
  Reconnecting = 'Reconnecting',
  Failed = 'Failed',
}
