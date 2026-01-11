import React, { useEffect, useState, useRef } from 'react'
import { useSignalR } from '@/contexts/SignalRContext'
import {
  Dialog,
  DialogContent,
  DialogDescription,
  DialogHeader,
  DialogTitle,
} from '@/components/ui/dialog'
import { Button } from '@/components/ui/button'
import { Input } from '@/components/ui/input'
import { Tabs, TabsContent, TabsList, TabsTrigger } from '@/components/ui/tabs'
import { Avatar, AvatarFallback } from '@/components/ui/avatar'
import { Badge } from '@/components/ui/badge'
import { useToast } from '@/hooks/use-toast'
import { Users, UserPlus, Check, X, Search } from 'lucide-react'

interface Friend {
  userId: string
  username: string
  displayName: string
  status: 'Online' | 'Offline' | 'InGame'
}

interface FriendRequest {
  userId: string
  username: string
  displayName: string
  requestedAt: string
}

interface FriendsDialogProps {
  isOpen: boolean
  onClose: () => void
}

export const FriendsDialog: React.FC<FriendsDialogProps> = ({ isOpen, onClose }) => {
  const { hub, isConnected } = useSignalR()
  const { toast } = useToast()

  const [friends, setFriends] = useState<Friend[]>([])
  const [pendingRequests, setPendingRequests] = useState<FriendRequest[]>([])
  const [searchQuery, setSearchQuery] = useState('')
  const [searchResults, setSearchResults] = useState<Friend[]>([])
  const [isSearching, setIsSearching] = useState(false)
  const [showSuggestions, setShowSuggestions] = useState(false)
  const debounceTimerRef = useRef<ReturnType<typeof setTimeout> | null>(null)

  useEffect(() => {
    if (isOpen && isConnected) {
      loadFriends()
      loadFriendRequests()
    }
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [isOpen, isConnected])

  // Debounced search effect
  useEffect(() => {
    // Clear previous timer
    if (debounceTimerRef.current) {
      clearTimeout(debounceTimerRef.current)
    }

    // Only search if query is at least 2 characters
    if (searchQuery.trim().length >= 2) {
      setIsSearching(true)
      debounceTimerRef.current = setTimeout(async () => {
        try {
          const results = await hub?.searchPlayers(searchQuery)
          setSearchResults(
            (results || []).map((p) => ({
              userId: p.userId,
              username: p.username,
              displayName: p.displayName,
              status: 'Offline' as const,
            }))
          )
          setShowSuggestions(true)
        } catch (error) {
          console.error('Failed to search players:', error)
        } finally {
          setIsSearching(false)
        }
      }, 300) // 300ms debounce
    } else {
      setSearchResults([])
      setShowSuggestions(false)
      setIsSearching(false)
    }

    return () => {
      if (debounceTimerRef.current) {
        clearTimeout(debounceTimerRef.current)
      }
    }
  }, [searchQuery, hub])

  const loadFriends = async () => {
    try {
      const friendsList = await hub?.getFriends()
      // Map FriendDto to local Friend type
      setFriends(
        (friendsList || []).map((f) => ({
          userId: f.userId,
          username: f.username,
          displayName: f.displayName,
          status: f.isOnline ? 'Online' : 'Offline' as const,
        }))
      )
    } catch (error) {
      console.error('Failed to load friends:', error)
    }
  }

  const loadFriendRequests = async () => {
    try {
      const requests = await hub?.getFriendRequests()
      // Map FriendDto to local FriendRequest type
      setPendingRequests(
        (requests || []).map((f) => ({
          userId: f.userId,
          username: f.username,
          displayName: f.displayName,
          requestedAt: new Date().toISOString(), // FriendDto doesn't have requestedAt
        }))
      )
    } catch (error) {
      console.error('Failed to load friend requests:', error)
    }
  }

  const handleSendFriendRequest = async (userId: string, username: string) => {
    try {
      await hub?.sendFriendRequest(userId)
      toast({
        title: 'Friend request sent',
        description: `Sent friend request to ${username}`,
      })
      setSearchResults([])
      setSearchQuery('')
      setShowSuggestions(false)
    } catch (error) {
      console.error('Failed to send friend request:', error)
      toast({
        title: 'Error',
        description: 'Failed to send friend request',
        variant: 'destructive',
      })
    }
  }

  const handleAcceptRequest = async (userId: string, username: string) => {
    try {
      await hub?.acceptFriendRequest(userId)
      toast({
        title: 'Friend request accepted',
        description: `You are now friends with ${username}`,
      })
      loadFriends()
      loadFriendRequests()
    } catch (error) {
      console.error('Failed to accept friend request:', error)
      toast({
        title: 'Error',
        description: 'Failed to accept friend request',
        variant: 'destructive',
      })
    }
  }

  const handleDeclineRequest = async (userId: string) => {
    try {
      await hub?.declineFriendRequest(userId)
      toast({
        title: 'Friend request declined',
      })
      loadFriendRequests()
    } catch (error) {
      console.error('Failed to decline friend request:', error)
      toast({
        title: 'Error',
        description: 'Failed to decline friend request',
        variant: 'destructive',
      })
    }
  }

  const handleRemoveFriend = async (userId: string, username: string) => {
    try {
      await hub?.removeFriend(userId)
      toast({
        title: 'Friend removed',
        description: `Removed ${username} from your friends list`,
      })
      loadFriends()
    } catch (error) {
      console.error('Failed to remove friend:', error)
      toast({
        title: 'Error',
        description: 'Failed to remove friend',
        variant: 'destructive',
      })
    }
  }

  return (
    <Dialog open={isOpen} onOpenChange={onClose}>
      <DialogContent className="max-w-2xl max-h-[80vh] overflow-y-auto">
        <DialogHeader>
          <DialogTitle className="flex items-center gap-2">
            <Users className="h-5 w-5" />
            Friends
          </DialogTitle>
          <DialogDescription>Manage your friends and friend requests</DialogDescription>
        </DialogHeader>

        <Tabs defaultValue="friends" className="w-full">
          <TabsList className="grid w-full grid-cols-3">
            <TabsTrigger value="friends">
              Friends {friends.length > 0 && `(${friends.length})`}
            </TabsTrigger>
            <TabsTrigger value="requests">
              Requests {pendingRequests.length > 0 && `(${pendingRequests.length})`}
            </TabsTrigger>
            <TabsTrigger value="add">Add Friends</TabsTrigger>
          </TabsList>

          <TabsContent value="friends" className="space-y-4 mt-4">
            {friends.length > 0 ? (
              <div className="space-y-2">
                {friends.map((friend) => (
                  <div
                    key={friend.userId}
                    className="flex items-center justify-between p-3 border rounded-lg"
                  >
                    <div className="flex items-center gap-3">
                      <Avatar>
                        <AvatarFallback>{friend.displayName.charAt(0).toUpperCase()}</AvatarFallback>
                      </Avatar>
                      <div>
                        <div className="font-semibold">{friend.displayName}</div>
                        <div className="text-sm text-muted-foreground">@{friend.username}</div>
                      </div>
                    </div>
                    <div className="flex items-center gap-2">
                      <Badge
                        variant={friend.status === 'Online' ? 'default' : 'secondary'}
                      >
                        {friend.status}
                      </Badge>
                      <Button
                        variant="outline"
                        size="sm"
                        onClick={() => handleRemoveFriend(friend.userId, friend.username)}
                      >
                        Remove
                      </Button>
                    </div>
                  </div>
                ))}
              </div>
            ) : (
              <div className="text-center py-8 text-muted-foreground">
                <Users className="h-12 w-12 mx-auto mb-2 opacity-50" />
                <p>No friends yet. Add some friends to get started!</p>
              </div>
            )}
          </TabsContent>

          <TabsContent value="requests" className="space-y-4 mt-4">
            {pendingRequests.length > 0 ? (
              <div className="space-y-2">
                {pendingRequests.map((request) => (
                  <div
                    key={request.userId}
                    className="flex items-center justify-between p-3 border rounded-lg"
                  >
                    <div className="flex items-center gap-3">
                      <Avatar>
                        <AvatarFallback>
                          {request.displayName.charAt(0).toUpperCase()}
                        </AvatarFallback>
                      </Avatar>
                      <div>
                        <div className="font-semibold">{request.displayName}</div>
                        <div className="text-sm text-muted-foreground">@{request.username}</div>
                      </div>
                    </div>
                    <div className="flex gap-2">
                      <Button
                        size="sm"
                        onClick={() => handleAcceptRequest(request.userId, request.username)}
                      >
                        <Check className="h-4 w-4 mr-1" />
                        Accept
                      </Button>
                      <Button
                        variant="outline"
                        size="sm"
                        onClick={() => handleDeclineRequest(request.userId)}
                      >
                        <X className="h-4 w-4 mr-1" />
                        Decline
                      </Button>
                    </div>
                  </div>
                ))}
              </div>
            ) : (
              <div className="text-center py-8 text-muted-foreground">
                <UserPlus className="h-12 w-12 mx-auto mb-2 opacity-50" />
                <p>No pending friend requests</p>
              </div>
            )}
          </TabsContent>

          <TabsContent value="add" className="space-y-4 mt-4">
            <div className="relative">
              <div className="relative">
                <Search className="absolute left-3 top-1/2 -translate-y-1/2 h-4 w-4 text-muted-foreground" />
                <Input
                  placeholder="Start typing to search users..."
                  value={searchQuery}
                  onChange={(e) => setSearchQuery(e.target.value)}
                  onFocus={() => searchResults.length > 0 && setShowSuggestions(true)}
                  className="pl-10"
                />
                {isSearching && (
                  <div className="absolute right-3 top-1/2 -translate-y-1/2">
                    <div className="h-4 w-4 animate-spin rounded-full border-2 border-primary border-t-transparent" />
                  </div>
                )}
              </div>

              {/* Autocomplete suggestions dropdown */}
              {showSuggestions && searchResults.length > 0 && (
                <div className="absolute z-50 w-full mt-1 bg-popover border rounded-lg shadow-lg max-h-64 overflow-y-auto">
                  {searchResults.map((user) => (
                    <div
                      key={user.userId}
                      className="flex items-center justify-between p-3 hover:bg-accent cursor-pointer transition-colors border-b last:border-b-0"
                      onClick={() => handleSendFriendRequest(user.userId, user.username)}
                    >
                      <div className="flex items-center gap-3">
                        <Avatar className="h-8 w-8">
                          <AvatarFallback className="text-sm">
                            {user.displayName.charAt(0).toUpperCase()}
                          </AvatarFallback>
                        </Avatar>
                        <div>
                          <div className="font-medium text-sm">{user.displayName}</div>
                          <div className="text-xs text-muted-foreground">@{user.username}</div>
                        </div>
                      </div>
                      <div className="flex items-center gap-1 text-xs text-muted-foreground">
                        <UserPlus className="h-3 w-3" />
                        <span>Add</span>
                      </div>
                    </div>
                  ))}
                </div>
              )}

              {/* No results message */}
              {showSuggestions && searchQuery.trim().length >= 2 && searchResults.length === 0 && !isSearching && (
                <div className="absolute z-50 w-full mt-1 bg-popover border rounded-lg shadow-lg p-4 text-center text-sm text-muted-foreground">
                  No users found matching "{searchQuery}"
                </div>
              )}
            </div>

            <p className="text-xs text-muted-foreground">
              Type at least 2 characters to search. Click a user to send a friend request.
            </p>
          </TabsContent>
        </Tabs>
      </DialogContent>
    </Dialog>
  )
}
