import { CheckerColor, OpponentType } from './game.types'

export interface MatchLobby {
  matchId: string
  creatorPlayerId: string
  creatorUsername: string
  opponentType: string
  targetScore: number
  status: string
  opponentPlayerId: string | null
  opponentPlayerName: string | null
  createdAt: string
  isOpenLobby: boolean
  isCorrespondence: boolean
  timePerMoveDays?: number
}

export interface MatchGame {
  gameId: string
  gameNumber: number
  winner: CheckerColor | null
  points: number
  isGamemon: boolean
  isBackgammon: boolean
  isCrawfordGame: boolean
  completedAt: string | null
}

export interface MatchResults {
  matchId: string
  winner: CheckerColor | null
  winnerUsername: string | null
  loserUsername: string | null
  finalScore: {
    white: number
    red: number
  }
  targetScore: number
  games: MatchGame[]
  totalGames: number
  duration: string
  completedAt: string
}

export interface CreateMatchRequest {
  opponentType: OpponentType
  targetScore: number
  friendUserId?: string
  timeControlType?: 'None' | 'ChicagoPoint'
  isCorrespondence?: boolean
  timePerMoveDays?: number
}

// Correspondence game types
export interface CorrespondenceGameDto {
  matchId: string
  gameId: string
  opponentId: string
  opponentName: string
  opponentRating: number
  isYourTurn: boolean
  timePerMoveDays: number
  turnDeadline: string | null
  timeRemaining: string | null // ISO duration string
  moveCount: number
  matchScore: string
  targetScore: number
  isRated: boolean
  lastUpdatedAt: string
}

export interface CorrespondenceGamesResponse {
  yourTurnGames: CorrespondenceGameDto[]
  waitingGames: CorrespondenceGameDto[]
  myLobbies: CorrespondenceGameDto[]
  totalYourTurn: number
  totalWaiting: number
  totalMyLobbies: number
}

export interface CorrespondenceMatchInvite {
  matchId: string
  gameId: string
  targetScore: number
  challengerName: string
  challengerId: string
  timePerMoveDays: number
}

export interface CorrespondenceTurnNotification {
  matchId: string
  message: string
}
