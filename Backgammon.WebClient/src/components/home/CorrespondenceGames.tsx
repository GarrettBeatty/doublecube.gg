import { useState } from "react";
import { useNavigate } from "react-router-dom";
import { Card, CardContent } from "@/components/ui/card";
import { Button } from "@/components/ui/button";
import { Badge } from "@/components/ui/badge";
import { Clock, User, RefreshCw, Plus, AlertCircle } from "lucide-react";
import { useCorrespondenceGames } from "@/hooks/useCorrespondenceGames";
import { CreateCorrespondenceMatchModal } from "@/components/modals/CreateCorrespondenceMatchModal";
import { CorrespondenceGameDto } from "@/types/match.types";

function formatTimeRemaining(timeRemaining: string | null): string {
  if (!timeRemaining) return "No deadline";

  // Parse ISO duration format (e.g., "2.12:30:00" for 2 days, 12 hours, 30 minutes)
  // TimeSpan.ToString() returns "d.hh:mm:ss" format
  const match = timeRemaining.match(/^(\d+)\.?(\d{2}):(\d{2}):(\d{2})/);
  if (!match) return timeRemaining;

  const days = parseInt(match[1]) || 0;
  const hours = parseInt(match[2]) || 0;

  if (days > 0) {
    return `${days}d ${hours}h`;
  } else if (hours > 0) {
    return `${hours}h`;
  } else {
    return "< 1h";
  }
}

function GameCard({
  game,
  isYourTurn,
  onPlay,
}: {
  game: CorrespondenceGameDto;
  isYourTurn: boolean;
  onPlay: (gameId: string) => void;
}) {
  const isUrgent = isYourTurn && game.timeRemaining && game.timeRemaining.includes(".") && parseInt(game.timeRemaining.split(".")[0]) <= 1;

  return (
    <div
      className={`flex items-center justify-between p-4 border rounded-lg hover:bg-accent transition-colors ${
        isYourTurn ? "bg-green-500/5" : "opacity-60"
      } ${isUrgent ? "border-orange-500" : ""}`}
    >
      <div className="flex items-center gap-3">
        <User className="h-5 w-5 text-muted-foreground" />
        <div>
          <div className="flex items-center gap-2">
            <span className={isYourTurn ? "font-semibold" : ""}>{game.opponentName}</span>
            <span className="text-sm text-muted-foreground">({game.opponentRating})</span>
            {game.isRated && (
              <Badge variant="outline" className="text-xs">
                Rated
              </Badge>
            )}
          </div>
          <div className="flex flex-wrap items-center gap-2 mt-1">
            <Badge
              variant={isUrgent ? "destructive" : "secondary"}
              className="text-xs flex items-center gap-1"
            >
              <Clock className="h-3 w-3" />
              {formatTimeRemaining(game.timeRemaining)}
            </Badge>
            <Badge variant="outline" className="text-xs">
              {game.matchScore} / {game.targetScore}pt
            </Badge>
            <span className="text-xs text-muted-foreground">{game.moveCount} moves</span>
          </div>
        </div>
      </div>
      <Button
        size="sm"
        variant={isYourTurn ? "default" : "outline"}
        className={isYourTurn ? "bg-green-600 hover:bg-green-700" : ""}
        onClick={() => onPlay(game.gameId)}
      >
        {isYourTurn ? "Play" : "View"}
      </Button>
    </div>
  );
}

export function CorrespondenceGames() {
  const navigate = useNavigate();
  const {
    yourTurnGames,
    waitingGames,
    myLobbies,
    totalYourTurn,
    isLoading,
    error,
    refresh,
  } = useCorrespondenceGames();
  const [isCreateModalOpen, setIsCreateModalOpen] = useState(false);

  const handlePlayGame = (gameId: string) => {
    navigate(`/game/${gameId}`);
  };

  if (isLoading) {
    return (
      <div className="flex items-center justify-center py-12">
        <div className="text-muted-foreground">Loading correspondence games...</div>
      </div>
    );
  }

  if (error) {
    return (
      <div className="flex flex-col items-center justify-center py-12 gap-4">
        <AlertCircle className="h-8 w-8 text-muted-foreground" />
        <div className="text-muted-foreground">{error}</div>
        <Button variant="outline" onClick={refresh}>
          <RefreshCw className="h-4 w-4 mr-2" />
          Retry
        </Button>
      </div>
    );
  }

  const hasGames = yourTurnGames.length > 0 || waitingGames.length > 0 || myLobbies.length > 0;

  return (
    <div className="space-y-6">
      {/* Header with actions */}
      <div className="flex items-center justify-between">
        <div className="flex items-center gap-2">
          <h2 className="text-lg font-semibold">My Correspondence Games</h2>
          <Button variant="ghost" size="icon" onClick={refresh} title="Refresh">
            <RefreshCw className="h-4 w-4" />
          </Button>
        </div>
        <Button onClick={() => setIsCreateModalOpen(true)}>
          <Plus className="h-4 w-4 mr-2" />
          New Game
        </Button>
      </div>

      {!hasGames ? (
        <Card>
          <CardContent className="flex flex-col items-center justify-center py-12 gap-4">
            <div className="text-muted-foreground text-center">
              <p className="text-lg mb-2">No correspondence games</p>
              <p className="text-sm">Use the "New Game" button above to start a game at your own pace</p>
            </div>
          </CardContent>
        </Card>
      ) : (
        <>
          {/* Your Turn Section */}
          <div>
            <div className="flex items-center justify-between mb-3">
              <h3 className="text-lg font-semibold">Your Turn</h3>
              {totalYourTurn > 0 && (
                <Badge variant="destructive" className="animate-pulse">
                  {totalYourTurn} waiting
                </Badge>
              )}
            </div>
            {yourTurnGames.length > 0 ? (
              <div className="space-y-2">
                {yourTurnGames.map((game) => (
                  <GameCard
                    key={game.matchId}
                    game={game}
                    isYourTurn={true}
                    onPlay={handlePlayGame}
                  />
                ))}
              </div>
            ) : (
              <div className="text-muted-foreground text-sm py-4 text-center border rounded-lg">
                No games waiting for your move
              </div>
            )}
          </div>

          {/* Opponent's Turn Section - Active games where it's not your turn */}
          <div>
            <h3 className="text-lg font-semibold mb-3">Opponent's Turn</h3>
            {waitingGames.length > 0 ? (
              <div className="space-y-2">
                {waitingGames.map((game) => (
                  <GameCard
                    key={game.matchId}
                    game={game}
                    isYourTurn={false}
                    onPlay={handlePlayGame}
                  />
                ))}
              </div>
            ) : (
              <div className="text-muted-foreground text-sm py-4 text-center border rounded-lg">
                All your games are waiting for your move
              </div>
            )}
          </div>

          {/* My Lobbies Section - Games created by user waiting for someone to join */}
          {myLobbies.length > 0 && (
            <div>
              <h3 className="text-lg font-semibold mb-3">Open Lobbies</h3>
              <div className="space-y-2">
                {myLobbies.map((lobby) => (
                  <div
                    key={lobby.matchId}
                    className="flex items-center justify-between p-4 border rounded-lg bg-blue-500/5"
                  >
                    <div className="flex items-center gap-3">
                      <User className="h-5 w-5 text-muted-foreground" />
                      <div>
                        <div className="flex items-center gap-2">
                          <span className="font-semibold">Waiting for player to join</span>
                          {lobby.isRated && (
                            <Badge variant="outline" className="text-xs">
                              Rated
                            </Badge>
                          )}
                        </div>
                        <div className="flex flex-wrap items-center gap-2 mt-1">
                          <Badge variant="secondary" className="text-xs flex items-center gap-1">
                            <Clock className="h-3 w-3" />
                            {lobby.timePerMoveDays} day{lobby.timePerMoveDays > 1 ? 's' : ''}/move
                          </Badge>
                          <Badge variant="outline" className="text-xs">
                            {lobby.targetScore}pt match
                          </Badge>
                        </div>
                      </div>
                    </div>
                    <Button
                      size="sm"
                      variant="outline"
                      onClick={() => handlePlayGame(lobby.gameId)}
                    >
                      View
                    </Button>
                  </div>
                ))}
              </div>
            </div>
          )}
        </>
      )}

      {/* Create Match Modal */}
      <CreateCorrespondenceMatchModal
        isOpen={isCreateModalOpen}
        onClose={() => setIsCreateModalOpen(false)}
      />
    </div>
  );
}
