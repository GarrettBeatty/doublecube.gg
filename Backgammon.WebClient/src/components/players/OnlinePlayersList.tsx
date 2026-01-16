import React, { useState, useEffect, useRef } from 'react'
import { useNavigate } from 'react-router-dom'
import { Badge } from '@/components/ui/badge'
import { Button } from '@/components/ui/button'
import { Input } from '@/components/ui/input'
import { Avatar, AvatarFallback } from '@/components/ui/avatar'
import { Skeleton } from '@/components/ui/skeleton'
import { Separator } from '@/components/ui/separator'
import { User, Gamepad2, UserPlus, Eye, Search, X, Circle } from 'lucide-react'
import type { OnlinePlayerDto, OnlinePlayerStatus, PlayerSearchResultDto } from '@/types/players'
import type { IGameHub } from '@/types/generated/TypedSignalR.Client/Backgammon.Server.Hubs.Interfaces'

interface OnlinePlayersListProps {
  players: OnlinePlayerDto[]
  isLoading: boolean
  onRefresh: () => void
  hub: IGameHub | null
}

export const OnlinePlayersList: React.FC<OnlinePlayersListProps> = ({
  players,
  isLoading,
  hub
}) => {
  const navigate = useNavigate()
  const [searchQuery, setSearchQuery] = useState('')
  const [searchResults, setSearchResults] = useState<PlayerSearchResultDto[]>([])
  const [isSearching, setIsSearching] = useState(false)
  const debounceRef = useRef<ReturnType<typeof setTimeout> | null>(null)

  // Debounced search effect
  useEffect(() => {
    if (debounceRef.current) {
      clearTimeout(debounceRef.current)
    }

    if (searchQuery.length < 2) {
      setSearchResults([])
      setIsSearching(false)
      return
    }

    setIsSearching(true)
    debounceRef.current = setTimeout(async () => {
      if (hub) {
        try {
          const results = await hub.searchPlayers(searchQuery)
          setSearchResults(results)
        } catch (error) {
          console.error('Search failed:', error)
          setSearchResults([])
        }
      }
      setIsSearching(false)
    }, 300)

    return () => {
      if (debounceRef.current) {
        clearTimeout(debounceRef.current)
      }
    }
  }, [searchQuery, hub])

  const clearSearch = () => {
    setSearchQuery('')
    setSearchResults([])
  }

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

  const isShowingSearchResults = searchQuery.length >= 2

  if (isLoading && !isShowingSearchResults) {
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

  return (
    <div className="space-y-4">
      {/* Search Input */}
      <div className="relative">
        <Search className="absolute left-3 top-1/2 -translate-y-1/2 h-4 w-4 text-muted-foreground" />
        <Input
          placeholder="Search all players by username..."
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

      {/* Search Results or Online Players */}
      {isShowingSearchResults ? (
        <div>
          <p className="text-sm text-muted-foreground mb-3">
            {isSearching ? (
              'Searching...'
            ) : (
              <>Search results for &quot;{searchQuery}&quot; ({searchResults.length} found)</>
            )}
          </p>
          {isSearching ? (
            <div className="space-y-3">
              {[1, 2, 3].map((i) => (
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
          ) : searchResults.length === 0 ? (
            <div className="text-center py-8">
              <User className="h-10 w-10 mx-auto text-muted-foreground mb-3" />
              <p className="text-muted-foreground">No players found</p>
              <p className="text-sm text-muted-foreground mt-1">
                Try a different search term
              </p>
            </div>
          ) : (
            <div className="space-y-1">
              {searchResults.map((player, index) => (
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
                  {index < searchResults.length - 1 && <Separator />}
                </div>
              ))}
            </div>
          )}
        </div>
      ) : (
        <>
          {players.length === 0 ? (
            <div className="text-center py-12">
              <User className="h-12 w-12 mx-auto text-muted-foreground mb-4" />
              <p className="text-muted-foreground">No other players online right now</p>
              <p className="text-sm text-muted-foreground mt-1">
                Check back later or play against a bot!
              </p>
            </div>
          ) : (
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
          )}
        </>
      )}
    </div>
  )
}
