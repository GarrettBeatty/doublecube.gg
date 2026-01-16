import React, { useState, useEffect, useRef, useMemo } from 'react'
import { useNavigate } from 'react-router-dom'
import { Badge } from '@/components/ui/badge'
import { Button } from '@/components/ui/button'
import { Input } from '@/components/ui/input'
import { Avatar, AvatarFallback } from '@/components/ui/avatar'
import { Skeleton } from '@/components/ui/skeleton'
import { Separator } from '@/components/ui/separator'
import { User, Search, X, Circle, Users, RefreshCw } from 'lucide-react'
import type { PlayerSearchResultDto } from '@/types/players'
import type { IGameHub } from '@/types/generated/TypedSignalR.Client/Backgammon.Server.Hubs.Interfaces'

interface AllPlayersListProps {
  hub: IGameHub | null
}

export const AllPlayersList: React.FC<AllPlayersListProps> = ({ hub }) => {
  const navigate = useNavigate()
  const [searchQuery, setSearchQuery] = useState('')
  const [allPlayers, setAllPlayers] = useState<PlayerSearchResultDto[]>([])
  const [isLoading, setIsLoading] = useState(true)
  const [error, setError] = useState<string | null>(null)
  const hasLoadedRef = useRef(false)

  // Load all players on mount
  useEffect(() => {
    if (!hub || hasLoadedRef.current) return

    const loadPlayers = async () => {
      setIsLoading(true)
      setError(null)
      try {
        const players = await hub.getAllPlayers(50)
        setAllPlayers(players)
        hasLoadedRef.current = true
      } catch (err) {
        console.error('Failed to load players:', err)
        setError('Failed to load players')
      } finally {
        setIsLoading(false)
      }
    }

    loadPlayers()
  }, [hub])

  // Refresh function
  const handleRefresh = async () => {
    if (!hub) return

    setIsLoading(true)
    setError(null)
    try {
      const players = await hub.getAllPlayers(50)
      setAllPlayers(players)
    } catch (err) {
      console.error('Failed to refresh players:', err)
      setError('Failed to refresh players')
    } finally {
      setIsLoading(false)
    }
  }

  // Filter players based on search query
  const filteredPlayers = useMemo(() => {
    if (!searchQuery.trim()) {
      return allPlayers
    }
    const query = searchQuery.toLowerCase()
    return allPlayers.filter(
      player =>
        player.username.toLowerCase().includes(query) ||
        player.displayName.toLowerCase().includes(query)
    )
  }, [allPlayers, searchQuery])

  const clearSearch = () => {
    setSearchQuery('')
  }

  const getOnlineStatusBadge = (isOnline: boolean) => {
    if (isOnline) {
      return (
        <Badge variant="outline" className="flex items-center gap-1 text-green-600 border-green-600">
          <Circle className="h-2 w-2 fill-green-500 text-green-500" />
          Online
        </Badge>
      )
    }
    return (
      <Badge variant="outline" className="flex items-center gap-1 text-muted-foreground">
        <Circle className="h-2 w-2" />
        Offline
      </Badge>
    )
  }

  return (
    <div className="space-y-4">
      {/* Search Input */}
      <div className="flex gap-2">
        <div className="relative flex-1">
          <Search className="absolute left-3 top-1/2 -translate-y-1/2 h-4 w-4 text-muted-foreground" />
          <Input
            placeholder="Filter players..."
            value={searchQuery}
            onChange={(e) => setSearchQuery(e.target.value)}
            className="pl-10 pr-10"
          />
          {searchQuery && (
            <Button
              variant="ghost"
              size="sm"
              className="absolute right-1 top-1/2 -translate-y-1/2 h-7 w-7 p-0"
              onClick={clearSearch}
            >
              <X className="h-4 w-4" />
            </Button>
          )}
        </div>
        <Button
          variant="outline"
          size="icon"
          onClick={handleRefresh}
          disabled={isLoading}
          title="Refresh player list"
        >
          <RefreshCw className={`h-4 w-4 ${isLoading ? 'animate-spin' : ''}`} />
        </Button>
      </div>

      {/* Loading State */}
      {isLoading ? (
        <div className="space-y-3">
          <p className="text-sm text-muted-foreground mb-3">Loading players...</p>
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
      ) : error ? (
        <div className="text-center py-8">
          <User className="h-10 w-10 mx-auto text-muted-foreground mb-3" />
          <p className="text-muted-foreground">{error}</p>
          <Button variant="outline" className="mt-4" onClick={handleRefresh}>
            Try Again
          </Button>
        </div>
      ) : allPlayers.length === 0 ? (
        <div className="text-center py-12">
          <Users className="h-12 w-12 mx-auto text-muted-foreground mb-4" />
          <p className="text-muted-foreground">No players found</p>
          <p className="text-sm text-muted-foreground mt-1">
            Be the first to register and play!
          </p>
        </div>
      ) : filteredPlayers.length === 0 ? (
        <div className="text-center py-8">
          <User className="h-10 w-10 mx-auto text-muted-foreground mb-3" />
          <p className="text-muted-foreground">No players match &quot;{searchQuery}&quot;</p>
          <p className="text-sm text-muted-foreground mt-1">
            Try a different search term
          </p>
        </div>
      ) : (
        <div>
          <p className="text-sm text-muted-foreground mb-3">
            {searchQuery
              ? `${filteredPlayers.length} of ${allPlayers.length} player${allPlayers.length !== 1 ? 's' : ''}`
              : `${allPlayers.length} player${allPlayers.length !== 1 ? 's' : ''}`}
          </p>
          <div className="space-y-1">
            {filteredPlayers.map((player, index) => (
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
                      </div>
                      <div className="flex items-center gap-2 text-sm text-muted-foreground">
                        <span>@{player.username}</span>
                        <span>-</span>
                        <span className="font-mono">{player.rating} ELO</span>
                        {player.totalGames > 0 && (
                          <>
                            <span>-</span>
                            <span>{player.totalGames} games</span>
                          </>
                        )}
                      </div>
                    </div>
                  </div>

                  <div className="flex items-center gap-3">
                    {getOnlineStatusBadge(player.isOnline)}
                    <Button
                      variant="ghost"
                      size="sm"
                      onClick={() => navigate(`/profile/${player.username}`)}
                    >
                      View Profile
                    </Button>
                  </div>
                </div>
                {index < filteredPlayers.length - 1 && <Separator />}
              </div>
            ))}
          </div>
        </div>
      )}
    </div>
  )
}
