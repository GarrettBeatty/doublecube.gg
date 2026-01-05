// Type definitions for Home Page components

export interface ActiveGame {
  gameId: string
  player1Name: string
  player2Name: string
  player1Rating?: number
  player2Rating?: number
  currentPlayer: 'White' | 'Red'
  matchScore?: string
  matchLength?: string
  viewers?: number
  timeControl?: string
  cubeValue?: number
  cubeOwner?: 'White' | 'Red' | 'Center'
  isCrawford?: boolean
}

export interface UserStats {
  rating: number
  wins: number
  losses: number
  winRate: number
  currentStreak: number
  streakType: 'win' | 'loss'
  gamesToday: number
  gamesThisWeek: number
}

export interface Friend {
  userId: string
  username: string
  displayName: string
  rating?: number
  status: 'Online' | 'Playing' | 'Offline'
  currentOpponent?: string
}

export interface LobbyGame {
  matchId: string
  creatorUserId: string
  creatorUsername: string
  creatorRating?: number
  timeControl?: string
  isRated: boolean
  matchLength: number
  doublingCube: boolean
}

export interface RecentGame {
  matchId: string
  opponentId: string
  opponentName: string
  opponentRating: number
  result: 'win' | 'loss'
  myScore: number
  opponentScore: number
  matchScore: string
  targetScore: number
  matchLength: string
  timeControl: string
  ratingChange: number
  completedAt?: string
  createdAt: string
}
