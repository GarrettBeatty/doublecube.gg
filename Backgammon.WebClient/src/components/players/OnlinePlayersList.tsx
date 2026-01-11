import React from 'react'
import { useNavigate } from 'react-router-dom'
import { Badge } from '@/components/ui/badge'
import { Button } from '@/components/ui/button'
import { Avatar, AvatarFallback } from '@/components/ui/avatar'
import { Skeleton } from '@/components/ui/skeleton'
import { Separator } from '@/components/ui/separator'
import { User, Gamepad2, UserPlus, Eye } from 'lucide-react'
import type { OnlinePlayerDto, OnlinePlayerStatus } from '@/types/players'

interface OnlinePlayersListProps {
  players: OnlinePlayerDto[]
  isLoading: boolean
  onRefresh: () => void
}

export const OnlinePlayersList: React.FC<OnlinePlayersListProps> = ({
  players,
  isLoading
}) => {
  const navigate = useNavigate()

  const getStatusBadge = (status: OnlinePlayerStatus) => {
    switch (status) {
      case 1: // InGame
        return (
          <Badge variant="secondary" className="flex items-center gap-1">
            <Gamepad2 className="h-3 w-3" />
            In Game
          </Badge>
        )
      case 2: // LookingForMatch
        return (
          <Badge variant="default" className="flex items-center gap-1 bg-green-600">
            <Eye className="h-3 w-3" />
            Looking
          </Badge>
        )
      default: // Available
        return (
          <Badge variant="outline" className="flex items-center gap-1 text-green-600 border-green-600">
            <div className="h-2 w-2 bg-green-500 rounded-full animate-pulse" />
            Online
          </Badge>
        )
    }
  }

  if (isLoading) {
    return (
      <div className="space-y-3">
        {[1, 2, 3, 4, 5].map((i) => (
          <div key={i} className="flex items-center justify-between p-3">
            <div className="flex items-center gap-3">
              <Skeleton className="h-10 w-10 rounded-full" />
              <div className="space-y-2">
                <Skeleton className="h-4 w-32" />
                <Skeleton className="h-3 w-20" />
              </div>
            </div>
            <Skeleton className="h-8 w-20" />
          </div>
        ))}
      </div>
    )
  }

  if (players.length === 0) {
    return (
      <div className="text-center py-12">
        <User className="h-12 w-12 mx-auto text-muted-foreground mb-4" />
        <p className="text-muted-foreground">No other players online right now</p>
        <p className="text-sm text-muted-foreground mt-1">
          Check back later or play against a bot!
        </p>
      </div>
    )
  }

  return (
    <div className="space-y-1">
      {players.map((player, index) => (
        <div key={player.userId}>
          <div className="flex items-center justify-between p-3 rounded-lg hover:bg-muted/50 transition-colors">
            <div className="flex items-center gap-3">
              <Avatar className="h-10 w-10">
                <AvatarFallback>
                  {player.displayName.charAt(0).toUpperCase()}
                </AvatarFallback>
              </Avatar>
              <div>
                <div className="flex items-center gap-2">
                  <span className="font-medium">{player.displayName}</span>
                  {player.isFriend && (
                    <Badge variant="secondary" className="text-xs">
                      <UserPlus className="h-3 w-3 mr-1" />
                      Friend
                    </Badge>
                  )}
                </div>
                <div className="flex items-center gap-2 text-sm text-muted-foreground">
                  <span>@{player.username}</span>
                  <span>-</span>
                  <span className="font-mono">{player.rating} ELO</span>
                </div>
              </div>
            </div>

            <div className="flex items-center gap-3">
              {getStatusBadge(player.status)}
              <Button
                variant="ghost"
                size="sm"
                onClick={() => navigate(`/profile/${player.username}`)}
              >
                View Profile
              </Button>
            </div>
          </div>
          {index < players.length - 1 && <Separator />}
        </div>
      ))}
    </div>
  )
}
