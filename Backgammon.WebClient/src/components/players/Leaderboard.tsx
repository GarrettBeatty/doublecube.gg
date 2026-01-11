import React from 'react'
import { useNavigate } from 'react-router-dom'
import { useAuth } from '@/contexts/AuthContext'
import { Badge } from '@/components/ui/badge'
import { Avatar, AvatarFallback } from '@/components/ui/avatar'
import { Skeleton } from '@/components/ui/skeleton'
import {
  Table,
  TableBody,
  TableCell,
  TableHead,
  TableHeader,
  TableRow
} from '@/components/ui/table'
import { Trophy, Medal, Award, TrendingUp } from 'lucide-react'
import type { LeaderboardEntryDto } from '@/types/players'

interface LeaderboardProps {
  entries: LeaderboardEntryDto[]
  isLoading: boolean
}

export const Leaderboard: React.FC<LeaderboardProps> = ({ entries, isLoading }) => {
  const navigate = useNavigate()
  const { user } = useAuth()

  const getRankIcon = (rank: number) => {
    switch (rank) {
      case 1:
        return <Trophy className="h-5 w-5 text-yellow-500" />
      case 2:
        return <Medal className="h-5 w-5 text-gray-400" />
      case 3:
        return <Award className="h-5 w-5 text-amber-600" />
      default:
        return <span className="text-muted-foreground font-mono">{rank}</span>
    }
  }

  const getRankBackground = (rank: number) => {
    switch (rank) {
      case 1:
        return 'bg-yellow-500/10'
      case 2:
        return 'bg-gray-400/10'
      case 3:
        return 'bg-amber-600/10'
      default:
        return ''
    }
  }

  if (isLoading) {
    return (
      <div className="space-y-3">
        {[1, 2, 3, 4, 5, 6, 7, 8, 9, 10].map((i) => (
          <div key={i} className="flex items-center gap-4 p-3">
            <Skeleton className="h-6 w-6" />
            <Skeleton className="h-10 w-10 rounded-full" />
            <div className="flex-1 space-y-2">
              <Skeleton className="h-4 w-32" />
              <Skeleton className="h-3 w-24" />
            </div>
            <Skeleton className="h-4 w-16" />
          </div>
        ))}
      </div>
    )
  }

  if (entries.length === 0) {
    return (
      <div className="text-center py-12">
        <Trophy className="h-12 w-12 mx-auto text-muted-foreground mb-4" />
        <p className="text-muted-foreground">No leaderboard data available</p>
        <p className="text-sm text-muted-foreground mt-1">
          Play some rated games to appear on the leaderboard!
        </p>
      </div>
    )
  }

  return (
    <div className="overflow-x-auto">
      <Table>
        <TableHeader>
          <TableRow>
            <TableHead className="w-16">Rank</TableHead>
            <TableHead>Player</TableHead>
            <TableHead className="text-right">Rating</TableHead>
            <TableHead className="text-right hidden sm:table-cell">Games</TableHead>
            <TableHead className="text-right hidden md:table-cell">W/L</TableHead>
            <TableHead className="text-right hidden lg:table-cell">Win Rate</TableHead>
          </TableRow>
        </TableHeader>
        <TableBody>
          {entries.map((entry) => {
            const isCurrentUser = user?.userId === entry.userId

            return (
              <TableRow
                key={entry.userId}
                className={`cursor-pointer hover:bg-muted/50 ${getRankBackground(entry.rank)} ${isCurrentUser ? 'ring-2 ring-primary ring-inset' : ''}`}
                onClick={() => navigate(`/profile/${entry.username}`)}
              >
                <TableCell className="font-medium">
                  <div className="flex items-center justify-center w-8 h-8">
                    {getRankIcon(entry.rank)}
                  </div>
                </TableCell>
                <TableCell>
                  <div className="flex items-center gap-3">
                    <Avatar className="h-8 w-8">
                      <AvatarFallback className="text-sm">
                        {entry.displayName.charAt(0).toUpperCase()}
                      </AvatarFallback>
                    </Avatar>
                    <div>
                      <div className="flex items-center gap-2">
                        <span className="font-medium">{entry.displayName}</span>
                        {entry.isOnline && (
                          <div className="h-2 w-2 bg-green-500 rounded-full" title="Online" />
                        )}
                        {isCurrentUser && (
                          <Badge variant="outline" className="text-xs">You</Badge>
                        )}
                      </div>
                      <span className="text-sm text-muted-foreground">@{entry.username}</span>
                    </div>
                  </div>
                </TableCell>
                <TableCell className="text-right">
                  <div className="flex items-center justify-end gap-1">
                    <TrendingUp className="h-4 w-4 text-muted-foreground" />
                    <span className="font-mono font-semibold">{entry.rating}</span>
                  </div>
                </TableCell>
                <TableCell className="text-right hidden sm:table-cell">
                  <span className="font-mono">{entry.totalGames}</span>
                </TableCell>
                <TableCell className="text-right hidden md:table-cell">
                  <span className="text-green-600 font-mono">{entry.wins}</span>
                  <span className="text-muted-foreground mx-1">/</span>
                  <span className="text-red-600 font-mono">{entry.losses}</span>
                </TableCell>
                <TableCell className="text-right hidden lg:table-cell">
                  <span className="font-mono">{entry.winRate}%</span>
                </TableCell>
              </TableRow>
            )
          })}
        </TableBody>
      </Table>
    </div>
  )
}
