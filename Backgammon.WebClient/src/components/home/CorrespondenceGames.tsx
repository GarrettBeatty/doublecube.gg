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

  const hasAnyGames = yourTurnGames.length > 0 || waitingGames.length > 0 || myLobbies.length > 0;

  // Combine all games: lobbies first (waiting for opponent), then your turn, then waiting for opponent's turn
  const allGames = [
    ...myLobbies.map(lobby => ({ ...lobby, type: 'lobby' as const })),
    ...yourTurnGames.map(game => ({ ...game, type: 'active' as const })),
    ...waitingGames.map(game => ({ ...game, type: 'active' as const }))
  ];

  return (
    <div className="space-y-6">
      {/* My Correspondence Games Section */}
      <div>
        <div className="flex items-center justify-between mb-4">
          <div className="flex items-center gap-2">
            <h2 className="text-xl font-semibold">My Correspondence Games</h2>
            {totalYourTurn > 0 && (
              <Badge variant="destructive" className="animate-pulse">
                {totalYourTurn} your turn
              </Badge>
            )}
            <Button variant="ghost" size="icon" onClick={refresh} title="Refresh">
              <RefreshCw className="h-4 w-4" />
            </Button>
          </div>
        </div>

        {hasAnyGames ? (
          <div className="space-y-2">
            {allGames.map((item) => {
              if (item.type === 'lobby') {
                // Render lobby card (waiting for opponent to join)
                return (
                  <div
                    key={item.matchId}
                    className="flex items-center justify-between p-4 border rounded-lg bg-amber-500/5 border-amber-500/20 hover:bg-amber-500/10 transition-colors"
                  >
                    <div className="flex items-center gap-3">
                      <User className="h-5 w-5 text-amber-600" />
                      <div>
                        <div className="flex items-center gap-2">
                          <span className="font-semibold text-amber-700 dark:text-amber-500">Waiting for opponent to join</span>
                          {item.isRated && (
                            <Badge variant="outline" className="text-xs">
                              Rated
                            </Badge>
                          )}
                        </div>
                        <div className="flex flex-wrap items-center gap-2 mt-1">
                          <Badge variant="secondary" className="text-xs flex items-center gap-1">
                            <Clock className="h-3 w-3" />
                            {item.timePerMoveDays} day{item.timePerMoveDays > 1 ? 's' : ''}/move
                          </Badge>
                          <Badge variant="outline" className="text-xs">
                            {item.targetScore}pt match
                          </Badge>
                        </div>
                      </div>
                    </div>
                    <Button
                      size="sm"
                      variant="outline"
                      onClick={() => handlePlayGame(item.gameId)}
                    >
                      View
                    </Button>
                  </div>
                );
              } else {
                // Render active game card
                return (
                  <GameCard
                    key={item.matchId}
                    game={item}
                    isYourTurn={item.isYourTurn}
                    onPlay={handlePlayGame}
                  />
                );
              }
            })}
          </div>
        ) : (
          <Card>
            <CardContent className="flex flex-col items-center justify-center py-8 gap-4">
              <div className="text-muted-foreground text-center">
                <p className="text-base mb-2">No correspondence games</p>
                <p className="text-sm">Create a new game or join an available game below</p>
              </div>
            </CardContent>
          </Card>
        )}

        <Button className="w-full" variant="outline" onClick={() => setIsCreateModalOpen(true)}>
          <Plus className="h-4 w-4 mr-2" />
          New Game
        </Button>
      </div>

      {/* Create Match Modal */}
      <CreateCorrespondenceMatchModal
        isOpen={isCreateModalOpen}
        onClose={() => setIsCreateModalOpen(false)}
      />
    </div>
  );
}
