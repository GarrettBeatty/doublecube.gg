import { Card, CardContent, CardDescription, CardHeader, CardTitle } from "@/components/ui/card";
import { Button } from "@/components/ui/button";
import { Badge } from "@/components/ui/badge";
import { Skeleton } from "@/components/ui/skeleton";
import { Clock, Eye, Dice6 } from "lucide-react";
import { useActiveGames } from "@/hooks/useActiveGames";
import { useSignalR } from "@/contexts/SignalRContext";
import { useToast } from "@/hooks/use-toast";

export function ActiveGamesList() {
  const { games, isLoading } = useActiveGames();
  const { invoke } = useSignalR();
  const { toast } = useToast();

  const handleWatch = async (gameId: string) => {
    try {
      await invoke('SpectateGame', gameId);
      // Navigation handled by SpectatorJoined event in useSignalREvents
    } catch (error) {
      console.error('Failed to spectate game:', error);
      toast({
        title: 'Cannot watch game',
        description: 'Spectator mode not yet implemented',
        variant: 'destructive',
      });
    }
  };

  if (isLoading) {
    return (
      <Card>
        <CardHeader>
          <CardTitle>Live Games</CardTitle>
          <CardDescription>Watch ongoing matches or join when available</CardDescription>
        </CardHeader>
        <CardContent className="space-y-3">
          <Skeleton className="h-24 w-full" />
          <Skeleton className="h-24 w-full" />
          <Skeleton className="h-24 w-full" />
        </CardContent>
      </Card>
    );
  }

  if (games.length === 0) {
    return (
      <Card>
        <CardHeader>
          <CardTitle>Live Games</CardTitle>
          <CardDescription>Watch ongoing matches or join when available</CardDescription>
        </CardHeader>
        <CardContent className="text-center py-8">
          <p className="text-muted-foreground">No active games at the moment</p>
          <p className="text-sm text-muted-foreground mt-2">Create a game to get started!</p>
        </CardContent>
      </Card>
    );
  }

  return (
    <Card>
      <CardHeader>
        <CardTitle>Live Games</CardTitle>
        <CardDescription>Watch ongoing matches or join when available</CardDescription>
      </CardHeader>
      <CardContent className="space-y-3">
        {games.map((game) => (
          <div
            key={game.gameId}
            className="flex items-center justify-between p-4 border rounded-lg hover:bg-accent transition-colors"
          >
            <div className="flex-1">
              <div className="flex items-center gap-2 mb-1">
                <span className={game.currentPlayer === 'White' ? "font-semibold" : ""}>
                  {game.player1Name}
                </span>
                {game.player1Rating && (
                  <span className="text-muted-foreground text-sm">({game.player1Rating})</span>
                )}
                {game.cubeOwner === 'White' && game.cubeValue && (
                  <Badge variant="outline" className="text-xs flex items-center gap-1">
                    <Dice6 className="h-3 w-3" />
                    {game.cubeValue}
                  </Badge>
                )}
              </div>
              <div className="flex items-center gap-2 mb-2">
                <span className={game.currentPlayer === 'Red' ? "font-semibold" : ""}>
                  {game.player2Name}
                </span>
                {game.player2Rating && (
                  <span className="text-muted-foreground text-sm">({game.player2Rating})</span>
                )}
                {game.cubeOwner === 'Red' && game.cubeValue && (
                  <Badge variant="outline" className="text-xs flex items-center gap-1">
                    <Dice6 className="h-3 w-3" />
                    {game.cubeValue}
                  </Badge>
                )}
              </div>
              <div className="flex items-center gap-2 text-sm text-muted-foreground">
                {game.matchScore && (
                  <>
                    <span className="font-semibold text-foreground">{game.matchScore}</span>
                    <span>in {game.matchLength || 'match'}</span>
                  </>
                )}
                {game.isCrawford && (
                  <Badge variant="secondary" className="text-xs">Crawford</Badge>
                )}
              </div>
            </div>
            <div className="flex items-center gap-3">
              {game.timeControl && (
                <Badge variant="secondary" className="flex items-center gap-1">
                  <Clock className="h-3 w-3" />
                  {game.timeControl}
                </Badge>
              )}
              {game.viewers !== undefined && (
                <Badge variant="outline" className="flex items-center gap-1">
                  <Eye className="h-3 w-3" />
                  {game.viewers}
                </Badge>
              )}
              <Button size="sm" onClick={() => handleWatch(game.gameId)}>
                Watch
              </Button>
            </div>
          </div>
        ))}
      </CardContent>
    </Card>
  );
}
