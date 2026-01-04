import { CheckerColor, OpponentType, MatchStatus } from './game.types'

export interface MatchLobby {
  matchId: string
  creatorPlayerId: string
  creatorUsername: string | null
  opponentType: OpponentType
  targetScore: number
  status: MatchStatus
  opponentPlayerId: string | null
  opponentUsername: string | null
  createdAt: string
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
}
