// Enums
export enum CheckerColor {
  White = 0,
  Red = 1,
}

export enum GameStatus {
  WaitingForPlayer = 0,
  InProgress = 1,
  Completed = 2,
}

export enum MatchStatus {
  Waiting = 0,
  InProgress = 1,
  Completed = 2,
}

export enum OpponentType {
  Friend = 0,
  AI = 1,
  OpenLobby = 2,
}

// Core domain types
export interface Point {
  position: number
  color: CheckerColor | null
  count: number
}

export interface Checker {
  color: CheckerColor
  id?: string
}

export interface Move {
  from: number
  to: number
  dieValue: number
}

export interface Dice {
  die1: number
  die2: number
}

// Game state (received from server)
export interface GameState {
  gameId: string
  status: GameStatus
  currentPlayer: CheckerColor
  yourColor: CheckerColor
  isYourTurn: boolean
  dice: number[]
  remainingMoves: Move[]
  validMoves: Move[]
  board: Point[]
  whiteCheckersOnBar: number
  redCheckersOnBar: number
  whiteBornOff: number
  redBornOff: number
  doublingCubeValue: number
  doublingCubeOwner: CheckerColor | null
  isAnalysisMode: boolean
  whitePlayerName: string
  redPlayerName: string
  whiteUsername: string | null
  redUsername: string | null
  whitePlayerId: string
  redPlayerId: string
  winner: CheckerColor | null
  createdAt: string
  updatedAt: string
  matchId?: string
  isMatchGame?: boolean
  targetScore?: number
  player1Score?: number
  player2Score?: number
  isCrawfordGame?: boolean
}

// Match types
export interface Match {
  matchId: string
  status: MatchStatus
  opponentType: OpponentType
  targetScore: number
  whiteScore: number
  redScore: number
  isCrawfordGame: boolean
  wasInCrawford: boolean
  createdAt: string
  updatedAt: string
  currentGameId: string | null
  games: Game[]
  whitePlayerId: string | null
  redPlayerId: string | null
  whitePlayerName: string | null
  redPlayerName: string | null
}

export interface Game {
  gameId: string
  winner: CheckerColor | null
  points: number
  isGamemon: boolean
  isBackgammon: boolean
  completedAt: string | null
}

// Player types
export interface Player {
  playerId: string
  username: string | null
  displayName: string
  color: CheckerColor
  isConnected: boolean
  pipCount?: number
}

export interface User {
  userId: string
  username: string
  email: string | null
  createdAt: string
}

export interface Profile {
  userId: string
  username: string
  email: string | null
  gamesPlayed: number
  gamesWon: number
  winRate: number
  createdAt: string
  recentGames: GameSummary[]
}

export interface GameSummary {
  gameId: string
  opponentName: string
  opponentUsername: string | null
  yourColor: CheckerColor
  winner: CheckerColor | null
  result: string // "Won" | "Lost" | "In Progress"
  points: number
  completedAt: string | null
}

// Friend types
export interface Friend {
  userId: string
  username: string
  status: FriendStatus
  createdAt: string
}

export enum FriendStatus {
  Pending = 0,
  Accepted = 1,
  Blocked = 2,
}

// UI state types
export interface Destination {
  point: number
  isCapture: boolean
}

export interface SelectedChecker {
  point: number
  x?: number
  y?: number
}

export interface DragState {
  isDragging: boolean
  draggedChecker: SVGElement | null
  sourcePoint: number
  ghostChecker: SVGElement | null
  offset: { x: number; y: number }
}

// API response types
export interface ApiResponse<T = any> {
  success: boolean
  data?: T
  error?: string
  message?: string
}

export interface ServerConfig {
  signalrUrl: string
}

export interface AuthResponse {
  token: string
  userId: string
  username: string
}

export interface LoginRequest {
  username: string
  password: string
}

export interface RegisterRequest {
  username: string
  email?: string
  password: string
}

// Chat types
export interface ChatMessage {
  senderName: string
  message: string
  timestamp: Date
  isOwn: boolean
}

// Analysis types
export interface PositionExport {
  gameId: string
  board: Point[]
  whiteCheckersOnBar: number
  redCheckersOnBar: number
  whiteBornOff: number
  redBornOff: number
  currentPlayer: CheckerColor
  doublingCubeValue: number
  doublingCubeOwner: CheckerColor | null
}
