// Type definitions for Home Page components

export interface MiniPoint {
  position: number
  color: 'White' | 'Red' | null
  count: number
}

export interface ActiveGame {
  matchId: string
  gameId: string
  player1Name: string
  player2Name: string
  player1Rating?: number
  player2Rating?: number
  currentPlayer: 'White' | 'Red'
  myColor: 'White' | 'Red'
  isYourTurn: boolean
  matchScore?: string
  matchLength?: number
  viewers?: number
  timeControl?: string
  cubeValue?: number
  cubeOwner?: 'White' | 'Red' | 'Center'
  isCrawford?: boolean
  // Board state for mini preview
  board?: MiniPoint[]
  whiteCheckersOnBar?: number
  redCheckersOnBar?: number
  whiteBornOff?: number
  redBornOff?: number
  dice?: number[]
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

export interface RecentOpponent {
  opponentId: string
  opponentName: string
  opponentRating: number
  totalMatches: number
  wins: number
  losses: number
  record: string
  winRate: number
  lastPlayedAt: string
  isAi: boolean
}

export interface ActiveMatch {
  matchId: string
  opponentName: string
  myScore: number
  opponentScore: number
  targetScore: number
  currentGameId?: string
  gamesPlayed: number
  isCrawford: boolean
  isCorrespondence: boolean
  createdAt: string
}
