import React, { useEffect, useState } from 'react'
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
  const { invoke, isConnected } = useSignalR()
  const { toast } = useToast()

  const [friends, setFriends] = useState<Friend[]>([])
  const [pendingRequests, setPendingRequests] = useState<FriendRequest[]>([])
  const [searchQuery, setSearchQuery] = useState('')
  const [searchResults, setSearchResults] = useState<Friend[]>([])
  const [isSearching, setIsSearching] = useState(false)

  useEffect(() => {
    if (isOpen && isConnected) {
      loadFriends()
      loadFriendRequests()
    }
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [isOpen, isConnected])

  const loadFriends = async () => {
    try {
      const friendsList = await invoke('GetFriends')
      setFriends(friendsList || [])
    } catch (error) {
      console.error('Failed to load friends:', error)
    }
  }

  const loadFriendRequests = async () => {
    try {
      const requests = await invoke('GetFriendRequests')
      setPendingRequests(requests || [])
    } catch (error) {
      console.error('Failed to load friend requests:', error)
    }
  }

  const handleSearchFriends = async () => {
    if (!searchQuery.trim()) return

    setIsSearching(true)
    try {
      const results = await invoke('SearchPlayers', searchQuery)
      setSearchResults(results || [])
    } catch (error) {
      console.error('Failed to search players:', error)
      toast({
        title: 'Error',
        description: 'Failed to search for players',
        variant: 'destructive',
      })
    } finally {
      setIsSearching(false)
    }
  }

  const handleSendFriendRequest = async (userId: string, username: string) => {
    try {
      await invoke('SendFriendRequest', userId)
      toast({
        title: 'Friend request sent',
        description: `Sent friend request to ${username}`,
      })
      setSearchResults([])
      setSearchQuery('')
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
      await invoke('AcceptFriendRequest', userId)
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
      await invoke('DeclineFriendRequest', userId)
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
      await invoke('RemoveFriend', userId)
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
            <div className="flex gap-2">
              <Input
                placeholder="Search by username..."
                value={searchQuery}
                onChange={(e) => setSearchQuery(e.target.value)}
                onKeyDown={(e) => e.key === 'Enter' && handleSearchFriends()}
              />
              <Button onClick={handleSearchFriends} disabled={isSearching}>
                <Search className="h-4 w-4 mr-2" />
                Search
              </Button>
            </div>

            {searchResults.length > 0 && (
              <div className="space-y-2">
                {searchResults.map((user) => (
                  <div
                    key={user.userId}
                    className="flex items-center justify-between p-3 border rounded-lg"
                  >
                    <div className="flex items-center gap-3">
                      <Avatar>
                        <AvatarFallback>{user.displayName.charAt(0).toUpperCase()}</AvatarFallback>
                      </Avatar>
                      <div>
                        <div className="font-semibold">{user.displayName}</div>
                        <div className="text-sm text-muted-foreground">@{user.username}</div>
                      </div>
                    </div>
                    <Button
                      size="sm"
                      onClick={() => handleSendFriendRequest(user.userId, user.username)}
                    >
                      <UserPlus className="h-4 w-4 mr-1" />
                      Add Friend
                    </Button>
                  </div>
                ))}
              </div>
            )}
          </TabsContent>
        </Tabs>
      </DialogContent>
    </Dialog>
  )
}
