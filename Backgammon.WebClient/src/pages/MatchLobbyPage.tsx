import React, { useEffect, useState } from 'react'
import { useParams, useNavigate } from 'react-router-dom'
import { useSignalR } from '@/contexts/SignalRContext'
import { useMatchStore } from '@/stores/matchStore'
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from '@/components/ui/card'
import { Button } from '@/components/ui/button'
import { Badge } from '@/components/ui/badge'
import { Separator } from '@/components/ui/separator'
import { Avatar, AvatarFallback } from '@/components/ui/avatar'
import { useToast } from '@/hooks/use-toast'
import { Users, Trophy, Target, Clock, Play } from 'lucide-react'
import { authService } from '@/services/auth.service'

interface MatchLobby {
  matchId: string
  player1Id: string
  player1Name: string
  player2Id: string | null
  player2Name: string | null
  targetScore: number
  opponentType: 'Human' | 'AI' | 'Friend'
  isOpenLobby: boolean
  lobbyStatus: 'WaitingForOpponent' | 'Ready' | 'InProgress'
  createdAt: string
}

export const MatchLobbyPage: React.FC = () => {
  const { matchId } = useParams<{ matchId: string }>()
  const navigate = useNavigate()
  const { invoke, isConnected, connection } = useSignalR()
  const { toast } = useToast()
  const { setCurrentMatchId } = useMatchStore()

  const [lobby, setLobby] = useState<MatchLobby | null>(null)
  const [isLoading, setIsLoading] = useState(true)
  const [isCreator, setIsCreator] = useState(false)

  useEffect(() => {
    if (!matchId) {
      navigate('/')
      return
    }

    joinLobby()
    registerEventHandlers()

    return () => {
      // Cleanup event handlers
      connection?.off('MatchLobbyUpdate')
      connection?.off('MatchStarted')
    }
  }, [matchId, isConnected])

  const registerEventHandlers = () => {
    if (!connection) return

    // Handle lobby updates (when players join/leave)
    connection.on('MatchLobbyUpdate', (updatedLobby: MatchLobby) => {
      console.log('[MatchLobby] Lobby updated:', updatedLobby)
      setLobby(updatedLobby)
    })

    // Handle match start
    connection.on('MatchStarted', (data: { matchId: string; gameId: string }) => {
      console.log('[MatchLobby] Match started:', data)
      setCurrentMatchId(data.matchId)
      toast({
        title: 'Match starting!',
        description: 'Navigating to game...',
      })
      navigate(`/game/${data.gameId}`)
    })
  }

  const joinLobby = async () => {
    if (!isConnected || !matchId) return

    setIsLoading(true)
    try {
      const displayName = authService.getDisplayName() || 'Guest'

      await invoke('JoinMatchLobby', matchId, displayName)

      // The MatchLobbyUpdate event will provide the lobby data
    } catch (error) {
      console.error('[MatchLobby] Failed to join lobby:', error)
      toast({
        title: 'Error',
        description: 'Failed to join match lobby',
        variant: 'destructive',
      })
      navigate('/')
    } finally {
      setIsLoading(false)
    }
  }

  const handleStartMatch = async () => {
    if (!matchId) return

    try {
      await invoke('StartMatchFromLobby', matchId)
      // Navigation will happen when MatchStarted event is received
    } catch (error) {
      console.error('[MatchLobby] Failed to start match:', error)
      toast({
        title: 'Error',
        description: 'Failed to start match',
        variant: 'destructive',
      })
    }
  }

  const handleLeaveLobby = async () => {
    try {
      if (matchId) {
        await invoke('LeaveMatchLobby', matchId)
      }
      navigate('/')
    } catch (error) {
      console.error('[MatchLobby] Failed to leave lobby:', error)
      navigate('/')
    }
  }

  // Update isCreator flag when lobby data changes
  useEffect(() => {
    if (lobby) {
      const playerId = authService.getOrCreatePlayerId()
      setIsCreator(lobby.player1Id === playerId)
    }
  }, [lobby])

  if (isLoading) {
    return (
      <div className="min-h-screen bg-background flex items-center justify-center">
        <Card>
          <CardContent className="p-8">
            <div className="text-center">
              <div className="text-lg font-semibold">Joining lobby...</div>
            </div>
          </CardContent>
        </Card>
      </div>
    )
  }

  if (!lobby) {
    return (
      <div className="min-h-screen bg-background flex items-center justify-center">
        <Card>
          <CardContent className="p-8">
            <div className="text-center">
              <div className="text-lg font-semibold">Waiting for lobby data...</div>
            </div>
          </CardContent>
        </Card>
      </div>
    )
  }

  const canStartMatch = isCreator && lobby.player2Id !== null && lobby.lobbyStatus === 'Ready'

  return (
    <div className="min-h-screen bg-background">
      <div className="max-w-4xl mx-auto px-4 py-8">
        <Button variant="outline" onClick={handleLeaveLobby} className="mb-6">
          ← Leave Lobby
        </Button>

        <Card className="mb-6">
          <CardHeader>
            <div className="flex items-center justify-between">
              <div>
                <CardTitle className="text-3xl mb-2">Match Lobby</CardTitle>
                <CardDescription>Waiting for players to join</CardDescription>
              </div>
              <Badge
                variant={lobby.lobbyStatus === 'Ready' ? 'default' : 'secondary'}
                className="text-lg px-4 py-2"
              >
                {lobby.lobbyStatus === 'Ready' ? (
                  <>
                    <Play className="h-4 w-4 mr-2" />
                    Ready to Start
                  </>
                ) : (
                  <>
                    <Clock className="h-4 w-4 mr-2" />
                    Waiting
                  </>
                )}
              </Badge>
            </div>
          </CardHeader>
        </Card>

        {/* Match Settings */}
        <Card className="mb-6">
          <CardHeader>
            <CardTitle>Match Settings</CardTitle>
          </CardHeader>
          <CardContent>
            <div className="grid grid-cols-1 md:grid-cols-3 gap-4">
              <div className="flex items-center gap-3 p-4 bg-muted rounded-lg">
                <Trophy className="h-8 w-8 text-primary" />
                <div>
                  <div className="text-sm text-muted-foreground">Target Score</div>
                  <div className="text-2xl font-bold">{lobby.targetScore}</div>
                </div>
              </div>

              <div className="flex items-center gap-3 p-4 bg-muted rounded-lg">
                <Target className="h-8 w-8 text-primary" />
                <div>
                  <div className="text-sm text-muted-foreground">Opponent Type</div>
                  <div className="text-2xl font-bold">{lobby.opponentType}</div>
                </div>
              </div>

              <div className="flex items-center gap-3 p-4 bg-muted rounded-lg">
                <Users className="h-8 w-8 text-primary" />
                <div>
                  <div className="text-sm text-muted-foreground">Lobby Type</div>
                  <div className="text-2xl font-bold">{lobby.isOpenLobby ? 'Open' : 'Private'}</div>
                </div>
              </div>
            </div>
          </CardContent>
        </Card>

        {/* Players */}
        <Card>
          <CardHeader>
            <CardTitle>Players</CardTitle>
            <CardDescription>2 players needed to start</CardDescription>
          </CardHeader>
          <CardContent>
            <div className="space-y-4">
              {/* Player 1 */}
              <div className="flex items-center justify-between p-4 border rounded-lg">
                <div className="flex items-center gap-4">
                  <Avatar className="h-12 w-12">
                    <AvatarFallback>
                      {lobby.player1Name.charAt(0).toUpperCase()}
                    </AvatarFallback>
                  </Avatar>
                  <div>
                    <div className="font-semibold">{lobby.player1Name}</div>
                    <div className="text-sm text-muted-foreground">Player 1</div>
                  </div>
                </div>
                <Badge>Host</Badge>
              </div>

              <Separator />

              {/* Player 2 */}
              {lobby.player2Id ? (
                <div className="flex items-center justify-between p-4 border rounded-lg">
                  <div className="flex items-center gap-4">
                    <Avatar className="h-12 w-12">
                      <AvatarFallback>
                        {lobby.player2Name?.charAt(0).toUpperCase() || '?'}
                      </AvatarFallback>
                    </Avatar>
                    <div>
                      <div className="font-semibold">{lobby.player2Name || 'Unknown'}</div>
                      <div className="text-sm text-muted-foreground">Player 2</div>
                    </div>
                  </div>
                  <Badge variant="secondary">Ready</Badge>
                </div>
              ) : (
                <div className="flex items-center justify-center p-8 border-2 border-dashed rounded-lg">
                  <div className="text-center">
                    <Users className="h-12 w-12 mx-auto mb-2 text-muted-foreground" />
                    <div className="font-semibold text-muted-foreground">
                      Waiting for opponent...
                    </div>
                    {lobby.opponentType === 'AI' && (
                      <div className="text-sm text-muted-foreground mt-1">
                        AI opponent will join automatically
                      </div>
                    )}
                  </div>
                </div>
              )}
            </div>

            {/* Start Button */}
            {isCreator && (
              <div className="mt-6">
                <Button
                  onClick={handleStartMatch}
                  disabled={!canStartMatch}
                  className="w-full"
                  size="lg"
                >
                  {canStartMatch ? (
                    <>
                      <Play className="h-5 w-5 mr-2" />
                      Start Match
                    </>
                  ) : (
                    <>
                      <Clock className="h-5 w-5 mr-2" />
                      Waiting for opponent...
                    </>
                  )}
                </Button>
                {!canStartMatch && lobby.player2Id && (
                  <p className="text-sm text-muted-foreground text-center mt-2">
                    Waiting for all players to be ready
                  </p>
                )}
              </div>
            )}

            {!isCreator && (
              <div className="mt-6">
                <div className="p-4 bg-muted rounded-lg text-center">
                  <p className="text-muted-foreground">
                    Waiting for host to start the match...
                  </p>
                </div>
              </div>
            )}
          </CardContent>
        </Card>

        {/* Match Info */}
        <Card className="mt-6">
          <CardContent className="p-4">
            <div className="text-sm text-muted-foreground">
              <p>
                <strong>Match ID:</strong> {lobby.matchId}
              </p>
              <p className="mt-1">
                <strong>Created:</strong> {new Date(lobby.createdAt).toLocaleString()}
              </p>
            </div>
          </CardContent>
        </Card>
      </div>
    </div>
  )
}
