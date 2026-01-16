import React, { useState, useEffect, useRef } from 'react'
import { useNavigate } from 'react-router-dom'
import { Badge } from '@/components/ui/badge'
import { Button } from '@/components/ui/button'
import { Input } from '@/components/ui/input'
import { Avatar, AvatarFallback } from '@/components/ui/avatar'
import { Skeleton } from '@/components/ui/skeleton'
import { Separator } from '@/components/ui/separator'
import { User, Search, X, Circle } from 'lucide-react'
import type { PlayerSearchResultDto } from '@/types/players'
import type { IGameHub } from '@/types/generated/TypedSignalR.Client/Backgammon.Server.Hubs.Interfaces'

interface AllPlayersListProps {
  hub: IGameHub | null
}

export const AllPlayersList: React.FC<AllPlayersListProps> = ({ hub }) => {
  const navigate = useNavigate()
  const [searchQuery, setSearchQuery] = useState('')
  const [searchResults, setSearchResults] = useState<PlayerSearchResultDto[]>([])
  const [isSearching, setIsSearching] = useState(false)
  const [hasSearched, setHasSearched] = useState(false)
  const debounceRef = useRef<ReturnType<typeof setTimeout> | null>(null)

  // Debounced search effect
  useEffect(() => {
    if (debounceRef.current) {
      clearTimeout(debounceRef.current)
    }

    if (searchQuery.length < 2) {
      setSearchResults([])
      setIsSearching(false)
      setHasSearched(false)
      return
    }

    setIsSearching(true)
    debounceRef.current = setTimeout(async () => {
      if (hub) {
        try {
          const results = await hub.searchPlayers(searchQuery)
          setSearchResults(results)
          setHasSearched(true)
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
    setHasSearched(false)
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
      <div className="relative">
        <Search className="absolute left-3 top-1/2 -translate-y-1/2 h-4 w-4 text-muted-foreground" />
        <Input
          placeholder="Search by username (min 2 characters)..."
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

      {/* Search Results */}
      {searchQuery.length < 2 ? (
        <div className="text-center py-12">
          <Search className="h-12 w-12 mx-auto text-muted-foreground mb-4" />
          <p className="text-muted-foreground">Enter a username to search</p>
          <p className="text-sm text-muted-foreground mt-1">
            Find any player, online or offline
          </p>
        </div>
      ) : isSearching ? (
        <div className="space-y-3">
          <p className="text-sm text-muted-foreground mb-3">Searching...</p>
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
      ) : hasSearched && searchResults.length === 0 ? (
        <div className="text-center py-8">
          <User className="h-10 w-10 mx-auto text-muted-foreground mb-3" />
          <p className="text-muted-foreground">No players found for &quot;{searchQuery}&quot;</p>
          <p className="text-sm text-muted-foreground mt-1">
            Try a different search term
          </p>
        </div>
      ) : (
        <div>
          {hasSearched && (
            <p className="text-sm text-muted-foreground mb-3">
              {searchResults.length} player{searchResults.length !== 1 ? 's' : ''} found
            </p>
          )}
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
        </div>
      )}
    </div>
  )
}
