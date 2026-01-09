import { useState, useEffect, useCallback } from 'react';
import { Button } from '@/components/ui/button';
import { Badge } from '@/components/ui/badge';
import { Clock, User, RefreshCw, AlertCircle } from 'lucide-react';
import { useSignalR } from '@/contexts/SignalRContext';
import { HubMethods } from '@/types/signalr.types';

interface CorrespondenceLobby {
  matchId: string;
  gameId: string;
  creatorId: string;
  creatorName: string;
  creatorRating: number;
  targetScore: number;
  timePerMoveDays: number;
  isRated: boolean;
  createdAt: string;
  isCorrespondence: boolean;
}

interface MatchLobbiesResponse {
  lobbies: CorrespondenceLobby[];
}

export function CorrespondenceLobbies() {
  const { invoke, isConnected } = useSignalR();
  const [lobbies, setLobbies] = useState<CorrespondenceLobby[]>([]);
  const [isLoading, setIsLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);

  const fetchLobbies = useCallback(async () => {
    if (!isConnected) return;

    try {
      setIsLoading(true);
      setError(null);

      const response = await invoke<MatchLobbiesResponse>(HubMethods.GetMatchLobbies);

      // Filter for correspondence lobbies only
      const correspondenceLobbies = (response?.lobbies || [])
        .filter((l) => l.isCorrespondence);

      setLobbies(correspondenceLobbies);
    } catch (err) {
      console.error('Error fetching correspondence lobbies:', err);
      setError('Failed to load correspondence lobbies');
    } finally {
      setIsLoading(false);
    }
  }, [isConnected, invoke]);

  useEffect(() => {
    fetchLobbies();
    // Auto-refresh every 30 seconds
    const interval = setInterval(fetchLobbies, 30000);
    return () => clearInterval(interval);
  }, [fetchLobbies]);

  const handleJoinLobby = async (matchId: string) => {
    try {
      await invoke(HubMethods.JoinMatch, matchId);
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
      <div className="space-y-4">
        <div className="flex items-center justify-between">
          <h3 className="text-lg font-semibold">Available Correspondence Games</h3>
          <Button variant="ghost" size="icon" onClick={fetchLobbies}>
            <RefreshCw className="h-4 w-4" />
          </Button>
        </div>
        <div className="flex flex-col items-center justify-center py-8 gap-4 border rounded-lg">
          <AlertCircle className="h-8 w-8 text-muted-foreground" />
          <div className="text-muted-foreground">{error}</div>
          <Button variant="outline" onClick={fetchLobbies}>
            <RefreshCw className="h-4 w-4 mr-2" />
            Retry
          </Button>
        </div>
      </div>
    );
  }

  return (
    <div className="space-y-4">
      <div className="flex items-center justify-between">
        <h3 className="text-lg font-semibold">Available Correspondence Games</h3>
        <Button variant="ghost" size="icon" onClick={fetchLobbies} disabled={isLoading}>
          <RefreshCw className={`h-4 w-4 ${isLoading ? 'animate-spin' : ''}`} />
        </Button>
      </div>

      {lobbies.length === 0 ? (
        <div className="text-center text-muted-foreground py-8 border rounded-lg">
          {isLoading ? 'Loading...' : 'No correspondence lobbies available'}
        </div>
      ) : (
        <div className="space-y-2">
          {lobbies.map((lobby) => (
            <div
              key={lobby.matchId}
              className="flex items-center justify-between p-4 border rounded-lg hover:bg-accent transition-colors"
            >
              <div className="flex items-center gap-3">
                <User className="h-5 w-5 text-muted-foreground" />
                <div>
                  <div className="flex items-center gap-2">
                    <span className="font-semibold">{lobby.creatorName}</span>
                    <span className="text-sm text-muted-foreground">({lobby.creatorRating})</span>
                    {lobby.isRated && (
                      <Badge variant="outline" className="text-xs">Rated</Badge>
                    )}
                  </div>
                  <div className="flex items-center gap-2 mt-1">
                    <Badge variant="secondary" className="text-xs flex items-center gap-1">
                      <Clock className="h-3 w-3" />
                      {lobby.timePerMoveDays} day{lobby.timePerMoveDays > 1 ? 's' : ''}/move
                    </Badge>
                    <Badge variant="outline" className="text-xs">
                      {lobby.targetScore} point{lobby.targetScore > 1 ? 's' : ''}
                    </Badge>
                  </div>
                </div>
              </div>
              <Button onClick={() => handleJoinLobby(lobby.matchId)}>
                Join
              </Button>
            </div>
          ))}
        </div>
      )}
    </div>
  );
}
