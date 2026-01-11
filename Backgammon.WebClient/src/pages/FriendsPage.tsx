import React, { useCallback, useEffect, useState } from 'react'
import { useNavigate } from 'react-router-dom'
import { useSignalR } from '@/contexts/SignalRContext'
import { useAuth } from '@/contexts/AuthContext'
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from '@/components/ui/card'
import { Tabs, TabsContent, TabsList, TabsTrigger } from '@/components/ui/tabs'
import { Button } from '@/components/ui/button'
import { Input } from '@/components/ui/input'
import { Avatar, AvatarFallback } from '@/components/ui/avatar'
import { Badge } from '@/components/ui/badge'
import { useToast } from '@/hooks/use-toast'
import {
  Users,
  UserPlus,
  ArrowLeft,
  Check,
  X,
  Search,
  UserMinus,
  Gamepad2,
  Circle
} from 'lucide-react'

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

interface SearchResult {
  userId: string
  username: string
  displayName: string
}

export const FriendsPage: React.FC = () => {
  const navigate = useNavigate()
  const { hub, isConnected } = useSignalR()
  const { isAuthenticated } = useAuth()
  const { toast } = useToast()

  const [activeTab, setActiveTab] = useState('all')
  const [isLoading, setIsLoading] = useState(true)

  const [friends, setFriends] = useState<Friend[]>([])
  const [pendingRequests, setPendingRequests] = useState<FriendRequest[]>([])
  const [searchQuery, setSearchQuery] = useState('')
  const [searchResults, setSearchResults] = useState<SearchResult[]>([])
  const [isSearching, setIsSearching] = useState(false)

  const loadFriends = useCallback(async () => {
    try {
      const friendsList = await hub?.getFriends()
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
  }, [hub])

  const loadFriendRequests = useCallback(async () => {
    try {
      const requests = await hub?.getFriendRequests()
      setPendingRequests(
        (requests || []).map((f) => ({
          userId: f.userId,
          username: f.username,
          displayName: f.displayName,
          requestedAt: new Date().toISOString(),
        }))
      )
    } catch (error) {
      console.error('Failed to load friend requests:', error)
    }
  }, [hub])

  const loadAllData = useCallback(async () => {
    if (!isAuthenticated) {
      setIsLoading(false)
      return
    }
    setIsLoading(true)
    try {
      await Promise.all([loadFriends(), loadFriendRequests()])
    } catch (error) {
      console.error('Failed to load friends data:', error)
    } finally {
      setIsLoading(false)
    }
  }, [isAuthenticated, loadFriends, loadFriendRequests])

  useEffect(() => {
    if (isConnected) {
      loadAllData()
    }
  }, [isConnected, loadAllData])

  const handleRefresh = () => {
    loadAllData()
  }

  const handleSearchPlayers = async () => {
    if (!searchQuery.trim()) return

    setIsSearching(true)
    try {
      const results = await hub?.searchPlayers(searchQuery)
      setSearchResults(
        (results || []).map((p) => ({
          userId: p.userId,
          username: p.username,
          displayName: p.displayName,
        }))
      )
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
      await hub?.sendFriendRequest(userId)
      toast({
        title: 'Friend request sent',
        description: `Sent friend request to ${username}`,
      })
      setSearchResults((prev) => prev.filter((u) => u.userId !== userId))
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

  const handleChallengeFriend = (_userId: string, username: string) => {
    // TODO: Implement challenge friend functionality
    toast({
      title: 'Coming soon',
      description: `Challenging ${username} will be available soon!`,
    })
  }

  const onlineFriends = friends.filter((f) => f.status !== 'Offline')

  const getStatusColor = (status: Friend['status']) => {
    switch (status) {
      case 'Online':
        return 'text-green-500'
      case 'InGame':
        return 'text-yellow-500'
      default:
        return 'text-muted-foreground'
    }
  }

  const getStatusBadgeVariant = (status: Friend['status']) => {
    switch (status) {
      case 'Online':
        return 'default' as const
      case 'InGame':
        return 'secondary' as const
      default:
        return 'outline' as const
    }
  }

  if (!isAuthenticated) {
    return (
      <div className="min-h-screen bg-background">
        <div className="max-w-6xl mx-auto px-4 py-8">
          <Button variant="outline" onClick={() => navigate('/')} className="mb-6">
            <ArrowLeft className="h-4 w-4 mr-2" />
            Back to Home
          </Button>

          <Card>
            <CardContent className="py-16 text-center">
              <Users className="h-16 w-16 mx-auto mb-4 text-muted-foreground opacity-50" />
              <h2 className="text-xl font-semibold mb-2">Sign in to view friends</h2>
              <p className="text-muted-foreground">
                You need to be logged in to manage your friends list.
              </p>
            </CardContent>
          </Card>
        </div>
      </div>
    )
  }

  return (
    <div className="min-h-screen bg-background">
      <div className="max-w-6xl mx-auto px-4 py-8">
        <Button variant="outline" onClick={() => navigate('/')} className="mb-6">
          <ArrowLeft className="h-4 w-4 mr-2" />
          Back to Home
        </Button>

        <div className="flex items-center justify-between mb-6">
          <div>
            <h1 className="text-3xl font-bold flex items-center gap-3">
              <Users className="h-8 w-8" />
              Friends
            </h1>
            <p className="text-muted-foreground mt-1">
              Manage your friends, view who's online, and send friend requests
            </p>
          </div>
          <Button variant="outline" onClick={handleRefresh} disabled={isLoading}>
            {isLoading ? 'Loading...' : 'Refresh'}
          </Button>
        </div>

        <Tabs value={activeTab} onValueChange={setActiveTab}>
          <TabsList className="grid w-full grid-cols-4 mb-6">
            <TabsTrigger value="all" className="flex items-center gap-2">
              <Users className="h-4 w-4" />
              <span className="hidden sm:inline">All</span>
              {friends.length > 0 && (
                <span className="ml-1 text-xs bg-primary/20 px-1.5 py-0.5 rounded-full">
                  {friends.length}
                </span>
              )}
            </TabsTrigger>
            <TabsTrigger value="online" className="flex items-center gap-2">
              <Circle className="h-4 w-4 fill-green-500 text-green-500" />
              <span className="hidden sm:inline">Online</span>
              {onlineFriends.length > 0 && (
                <span className="ml-1 text-xs bg-green-500/20 px-1.5 py-0.5 rounded-full">
                  {onlineFriends.length}
                </span>
              )}
            </TabsTrigger>
            <TabsTrigger value="requests" className="flex items-center gap-2">
              <UserPlus className="h-4 w-4" />
              <span className="hidden sm:inline">Requests</span>
              {pendingRequests.length > 0 && (
                <span className="ml-1 text-xs bg-orange-500/20 text-orange-500 px-1.5 py-0.5 rounded-full">
                  {pendingRequests.length}
                </span>
              )}
            </TabsTrigger>
            <TabsTrigger value="add" className="flex items-center gap-2">
              <Search className="h-4 w-4" />
              <span className="hidden sm:inline">Add Friends</span>
            </TabsTrigger>
          </TabsList>

          <TabsContent value="all">
            <Card>
              <CardHeader>
                <CardTitle className="flex items-center gap-2">
                  <Users className="h-5 w-5 text-blue-500" />
                  All Friends
                </CardTitle>
                <CardDescription>
                  Your complete friends list
                </CardDescription>
              </CardHeader>
              <CardContent>
                {isLoading ? (
                  <div className="text-center py-8 text-muted-foreground">
                    Loading friends...
                  </div>
                ) : friends.length > 0 ? (
                  <div className="space-y-2">
                    {friends.map((friend) => (
                      <div
                        key={friend.userId}
                        className="flex items-center justify-between p-4 border rounded-lg hover:bg-muted/50 transition-colors"
                      >
                        <div className="flex items-center gap-3">
                          <div className="relative">
                            <Avatar>
                              <AvatarFallback>
                                {friend.displayName.charAt(0).toUpperCase()}
                              </AvatarFallback>
                            </Avatar>
                            <Circle
                              className={`absolute -bottom-0.5 -right-0.5 h-3 w-3 ${getStatusColor(friend.status)} fill-current`}
                            />
                          </div>
                          <div>
                            <div
                              className="font-semibold cursor-pointer hover:underline"
                              onClick={() => navigate(`/profile/${friend.username}`)}
                            >
                              {friend.displayName}
                            </div>
                            <div className="text-sm text-muted-foreground">@{friend.username}</div>
                          </div>
                        </div>
                        <div className="flex items-center gap-2">
                          <Badge variant={getStatusBadgeVariant(friend.status)}>
                            {friend.status === 'InGame' ? 'In Game' : friend.status}
                          </Badge>
                          {friend.status !== 'Offline' && (
                            <Button
                              variant="outline"
                              size="sm"
                              onClick={() => handleChallengeFriend(friend.userId, friend.username)}
                            >
                              <Gamepad2 className="h-4 w-4 mr-1" />
                              Challenge
                            </Button>
                          )}
                          <Button
                            variant="ghost"
                            size="sm"
                            onClick={() => handleRemoveFriend(friend.userId, friend.username)}
                          >
                            <UserMinus className="h-4 w-4" />
                          </Button>
                        </div>
                      </div>
                    ))}
                  </div>
                ) : (
                  <div className="text-center py-12 text-muted-foreground">
                    <Users className="h-16 w-16 mx-auto mb-4 opacity-50" />
                    <p className="text-lg font-medium">No friends yet</p>
                    <p className="text-sm mt-1">Use the "Add Friends" tab to find players</p>
                  </div>
                )}
              </CardContent>
            </Card>
          </TabsContent>

          <TabsContent value="online">
            <Card>
              <CardHeader>
                <CardTitle className="flex items-center gap-2">
                  <Circle className="h-5 w-5 fill-green-500 text-green-500" />
                  Online Friends
                </CardTitle>
                <CardDescription>
                  Friends currently online and available to play
                </CardDescription>
              </CardHeader>
              <CardContent>
                {isLoading ? (
                  <div className="text-center py-8 text-muted-foreground">
                    Loading friends...
                  </div>
                ) : onlineFriends.length > 0 ? (
                  <div className="space-y-2">
                    {onlineFriends.map((friend) => (
                      <div
                        key={friend.userId}
                        className="flex items-center justify-between p-4 border rounded-lg hover:bg-muted/50 transition-colors"
                      >
                        <div className="flex items-center gap-3">
                          <div className="relative">
                            <Avatar>
                              <AvatarFallback>
                                {friend.displayName.charAt(0).toUpperCase()}
                              </AvatarFallback>
                            </Avatar>
                            <Circle
                              className={`absolute -bottom-0.5 -right-0.5 h-3 w-3 ${getStatusColor(friend.status)} fill-current`}
                            />
                          </div>
                          <div>
                            <div
                              className="font-semibold cursor-pointer hover:underline"
                              onClick={() => navigate(`/profile/${friend.username}`)}
                            >
                              {friend.displayName}
                            </div>
                            <div className="text-sm text-muted-foreground">@{friend.username}</div>
                          </div>
                        </div>
                        <div className="flex items-center gap-2">
                          <Badge variant={getStatusBadgeVariant(friend.status)}>
                            {friend.status === 'InGame' ? 'In Game' : friend.status}
                          </Badge>
                          <Button
                            variant="default"
                            size="sm"
                            onClick={() => handleChallengeFriend(friend.userId, friend.username)}
                          >
                            <Gamepad2 className="h-4 w-4 mr-1" />
                            Challenge
                          </Button>
                        </div>
                      </div>
                    ))}
                  </div>
                ) : (
                  <div className="text-center py-12 text-muted-foreground">
                    <Circle className="h-16 w-16 mx-auto mb-4 opacity-50" />
                    <p className="text-lg font-medium">No friends online</p>
                    <p className="text-sm mt-1">Check back later or add more friends</p>
                  </div>
                )}
              </CardContent>
            </Card>
          </TabsContent>

          <TabsContent value="requests">
            <Card>
              <CardHeader>
                <CardTitle className="flex items-center gap-2">
                  <UserPlus className="h-5 w-5 text-orange-500" />
                  Friend Requests
                </CardTitle>
                <CardDescription>
                  Pending friend requests from other players
                </CardDescription>
              </CardHeader>
              <CardContent>
                {isLoading ? (
                  <div className="text-center py-8 text-muted-foreground">
                    Loading requests...
                  </div>
                ) : pendingRequests.length > 0 ? (
                  <div className="space-y-2">
                    {pendingRequests.map((request) => (
                      <div
                        key={request.userId}
                        className="flex items-center justify-between p-4 border rounded-lg hover:bg-muted/50 transition-colors"
                      >
                        <div className="flex items-center gap-3">
                          <Avatar>
                            <AvatarFallback>
                              {request.displayName.charAt(0).toUpperCase()}
                            </AvatarFallback>
                          </Avatar>
                          <div>
                            <div
                              className="font-semibold cursor-pointer hover:underline"
                              onClick={() => navigate(`/profile/${request.username}`)}
                            >
                              {request.displayName}
                            </div>
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
                  <div className="text-center py-12 text-muted-foreground">
                    <UserPlus className="h-16 w-16 mx-auto mb-4 opacity-50" />
                    <p className="text-lg font-medium">No pending requests</p>
                    <p className="text-sm mt-1">Friend requests will appear here</p>
                  </div>
                )}
              </CardContent>
            </Card>
          </TabsContent>

          <TabsContent value="add">
            <Card>
              <CardHeader>
                <CardTitle className="flex items-center gap-2">
                  <Search className="h-5 w-5 text-purple-500" />
                  Add Friends
                </CardTitle>
                <CardDescription>
                  Search for players by username to add them as friends
                </CardDescription>
              </CardHeader>
              <CardContent>
                <div className="flex gap-2 mb-6">
                  <Input
                    placeholder="Search by username..."
                    value={searchQuery}
                    onChange={(e) => setSearchQuery(e.target.value)}
                    onKeyDown={(e) => e.key === 'Enter' && handleSearchPlayers()}
                    className="flex-1"
                  />
                  <Button onClick={handleSearchPlayers} disabled={isSearching || !searchQuery.trim()}>
                    <Search className="h-4 w-4 mr-2" />
                    {isSearching ? 'Searching...' : 'Search'}
                  </Button>
                </div>

                {searchResults.length > 0 ? (
                  <div className="space-y-2">
                    {searchResults.map((user) => {
                      const isFriend = friends.some((f) => f.userId === user.userId)
                      const hasPendingRequest = pendingRequests.some((r) => r.userId === user.userId)

                      return (
                        <div
                          key={user.userId}
                          className="flex items-center justify-between p-4 border rounded-lg hover:bg-muted/50 transition-colors"
                        >
                          <div className="flex items-center gap-3">
                            <Avatar>
                              <AvatarFallback>
                                {user.displayName.charAt(0).toUpperCase()}
                              </AvatarFallback>
                            </Avatar>
                            <div>
                              <div
                                className="font-semibold cursor-pointer hover:underline"
                                onClick={() => navigate(`/profile/${user.username}`)}
                              >
                                {user.displayName}
                              </div>
                              <div className="text-sm text-muted-foreground">@{user.username}</div>
                            </div>
                          </div>
                          {isFriend ? (
                            <Badge variant="secondary">Already friends</Badge>
                          ) : hasPendingRequest ? (
                            <Badge variant="outline">Request pending</Badge>
                          ) : (
                            <Button
                              size="sm"
                              onClick={() => handleSendFriendRequest(user.userId, user.username)}
                            >
                              <UserPlus className="h-4 w-4 mr-1" />
                              Add Friend
                            </Button>
                          )}
                        </div>
                      )
                    })}
                  </div>
                ) : searchQuery && !isSearching ? (
                  <div className="text-center py-8 text-muted-foreground">
                    <Search className="h-12 w-12 mx-auto mb-2 opacity-50" />
                    <p>No players found matching "{searchQuery}"</p>
                  </div>
                ) : (
                  <div className="text-center py-8 text-muted-foreground">
                    <Search className="h-12 w-12 mx-auto mb-2 opacity-50" />
                    <p>Enter a username to search for players</p>
                  </div>
                )}
              </CardContent>
            </Card>
          </TabsContent>
        </Tabs>
      </div>
    </div>
  )
}
