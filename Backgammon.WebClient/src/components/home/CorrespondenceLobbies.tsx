import { useState, useEffect, useCallback } from 'react';
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from '@/components/ui/card';
import { Button } from '@/components/ui/button';
import { Badge } from '@/components/ui/badge';
import { Clock, User, RefreshCw, AlertCircle } from 'lucide-react';
import { useSignalR } from '@/contexts/SignalRContext';
import { HubEvents } from '@/types/signalr.types';
import { authService } from '@/services/auth.service';
import { CreateCorrespondenceMatchModal } from '@/components/modals/CreateCorrespondenceMatchModal';

interface CorrespondenceLobby {
  matchId: string;
  creatorPlayerId: string;
  creatorUsername: string;
  opponentType: string;
  targetScore: number;
  status: string;
  opponentPlayerId?: string;
  opponentUsername?: string;
  createdAt: string;
  isOpenLobby: boolean;
  isCorrespondence: boolean;
  timePerMoveDays?: number;
}

export function CorrespondenceLobbies() {
  const { hub, isConnected, connection } = useSignalR();
  const [lobbies, setLobbies] = useState<CorrespondenceLobby[]>([]);
  const [isLoading, setIsLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [isCreateModalOpen, setIsCreateModalOpen] = useState(false);

  const fetchLobbies = useCallback(async () => {
    if (!isConnected) return;

    try {
      setIsLoading(true);
      setError(null);

      // GetMatchLobbies with 'correspondence' filter returns only correspondence lobbies
      const allLobbies = await hub?.getMatchLobbies('correspondence');

      // Get current player ID to filter out their own lobbies
      const currentPlayerId = authService.getOrCreatePlayerId();

      // Filter out lobbies created by the current player (they appear in "My Lobbies" instead)
      const availableLobbies = (allLobbies || [])
        .filter((l) => l.creatorPlayerId !== currentPlayerId);

      setLobbies(availableLobbies);
    } catch (err) {
      console.error('Error fetching correspondence lobbies:', err);
      setError('Failed to load correspondence lobbies');
    } finally {
      setIsLoading(false);
    }
  }, [isConnected, hub]);

  useEffect(() => {
    fetchLobbies();

    // Listen for new correspondence lobbies being created
    const handleLobbyCreated = () => {
      console.log('Correspondence lobby created - refreshing list');
      fetchLobbies();
    };

    connection?.on(HubEvents.CorrespondenceLobbyCreated, handleLobbyCreated);

    // Auto-refresh every 30 seconds as fallback
    const interval = setInterval(fetchLobbies, 30000);

    return () => {
      connection?.off(HubEvents.CorrespondenceLobbyCreated, handleLobbyCreated);
      clearInterval(interval);
    };
  }, [connection, fetchLobbies]);

  const handleJoinLobby = async (matchId: string) => {
    try {
      await hub?.joinMatch(matchId);
      // SignalR will send MatchCreated event, navigate handled by event handler
      // Refresh lobbies to remove the joined lobby
      setTimeout(fetchLobbies, 500);
    } catch (err) {
      console.error('Error joining lobby:', err);
      setError('Failed to join lobby. Please try again.');
    }
  };

  if (error) {
    return (
      <Card>
        <CardHeader className="flex flex-row items-center justify-between space-y-0 pb-2">
          <div>
            <CardTitle>Correspondence Games</CardTitle>
            <CardDescription>Play at your own pace</CardDescription>
          </div>
          <Button variant="ghost" size="icon" onClick={fetchLobbies} title="Refresh">
            <RefreshCw className="h-4 w-4" />
          </Button>
        </CardHeader>
        <CardContent>
          <div className="flex flex-col items-center justify-center py-8 gap-4">
            <AlertCircle className="h-8 w-8 text-muted-foreground" />
            <div className="text-muted-foreground">{error}</div>
            <Button variant="outline" onClick={fetchLobbies}>
              <RefreshCw className="h-4 w-4 mr-2" />
              Retry
            </Button>
          </div>
        </CardContent>
      </Card>
    );
  }

  return (
    <Card>
      <CardHeader className="flex flex-row items-center justify-between space-y-0 pb-2">
        <div>
          <CardTitle>Correspondence Games</CardTitle>
          <CardDescription>Play at your own pace</CardDescription>
        </div>
        <Button variant="ghost" size="icon" onClick={fetchLobbies} disabled={isLoading} title="Refresh">
          <RefreshCw className={`h-4 w-4 ${isLoading ? 'animate-spin' : ''}`} />
        </Button>
      </CardHeader>
      <CardContent className="space-y-4">
        {lobbies.length === 0 ? (
          <div className="text-center py-8">
            <p className="text-muted-foreground">No games available to join</p>
            <p className="text-sm text-muted-foreground mt-2">Create one to get started!</p>
          </div>
        ) : (
          <div className="space-y-2">
            {lobbies.map((lobby) => (
              <div
                key={lobby.matchId}
                className="flex items-center justify-between p-3 border rounded-lg hover:bg-accent transition-colors"
              >
                <div className="flex items-center gap-3 flex-1">
                  <User className="h-5 w-5 text-muted-foreground" />
                  <div className="flex-1">
                    <div className="flex items-center gap-2">
                      <span className="font-semibold">{lobby.creatorUsername || 'Anonymous'}</span>
                    </div>
                    <div className="flex flex-wrap items-center gap-2 mt-1">
                      <Badge variant="secondary" className="text-xs flex items-center gap-1">
                        <Clock className="h-3 w-3" />
                        {lobby.timePerMoveDays ?? 3} day{(lobby.timePerMoveDays ?? 3) > 1 ? 's' : ''}/move
                      </Badge>
                      <Badge variant="outline" className="text-xs">
                        {lobby.targetScore}-point
                      </Badge>
                    </div>
                  </div>
                </div>
                <Button size="sm" onClick={() => handleJoinLobby(lobby.matchId)}>
                  Join
                </Button>
              </div>
            ))}
          </div>
        )}
        <Button className="w-full" variant="outline" onClick={() => setIsCreateModalOpen(true)}>
          New Game
        </Button>
      </CardContent>

      <CreateCorrespondenceMatchModal
        isOpen={isCreateModalOpen}
        onClose={() => setIsCreateModalOpen(false)}
      />
    </Card>
  );
}
