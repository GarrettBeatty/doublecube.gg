import { Card, CardContent, CardDescription, CardHeader, CardTitle } from "@/components/ui/card";
import { Button } from "@/components/ui/button";
import { Badge } from "@/components/ui/badge";
import { Skeleton } from "@/components/ui/skeleton";
import { Trophy, X, Eye } from "lucide-react";
import { useRecentGames } from "@/hooks/useRecentGames";
import { formatDistanceToNow } from "date-fns";

export function RecentGames() {
  const { games, isLoading } = useRecentGames(5);

  const formatDate = (dateString: string) => {
    try {
      return formatDistanceToNow(new Date(dateString), { addSuffix: true });
    } catch {
      return 'Recently';
    }
  };

  if (isLoading) {
    return (
      <Card>
        <CardHeader>
          <CardTitle>Recent Games</CardTitle>
          <CardDescription>Review your recent matches</CardDescription>
        </CardHeader>
        <CardContent className="space-y-3">
          <Skeleton className="h-20 w-full" />
          <Skeleton className="h-20 w-full" />
          <Skeleton className="h-20 w-full" />
        </CardContent>
      </Card>
    );
  }

  if (games.length === 0) {
    return (
      <Card>
        <CardHeader>
          <CardTitle>Recent Games</CardTitle>
          <CardDescription>Review your recent matches</CardDescription>
        </CardHeader>
        <CardContent className="text-center py-8">
          <p className="text-muted-foreground">No completed games yet</p>
          <p className="text-sm text-muted-foreground mt-2">Play a match to see your game history!</p>
        </CardContent>
      </Card>
    );
  }

  return (
    <Card>
      <CardHeader>
        <CardTitle>Recent Games</CardTitle>
        <CardDescription>Review your recent matches</CardDescription>
      </CardHeader>
      <CardContent className="space-y-3">
        {games.map((game) => (
          <div
            key={game.matchId}
            className="flex items-center justify-between p-4 border rounded-lg hover:bg-accent transition-colors"
          >
            <div className="flex items-center gap-3">
              {game.result === "win" ? (
                <Trophy className="h-5 w-5 text-green-500" />
              ) : (
                <X className="h-5 w-5 text-red-500" />
              )}
              <div>
                <div className="flex items-center gap-2 mb-1">
                  <span className="font-semibold">{game.opponentName}</span>
                  {game.opponentRating > 0 && (
                    <span className="text-muted-foreground text-sm">({game.opponentRating})</span>
                  )}
                </div>
                <div className="flex items-center gap-2 text-sm text-muted-foreground">
                  <span>{game.matchLength} match</span>
                  <span>•</span>
                  <span>{game.timeControl}</span>
                  <span>•</span>
                  <span>{formatDate(game.completedAt || game.createdAt)}</span>
                </div>
              </div>
            </div>
            <div className="flex items-center gap-3">
              <div className="text-right">
                <p className="font-semibold">{game.matchScore}</p>
                {game.ratingChange !== 0 && (
                  <Badge
                    variant={game.result === "win" ? "default" : "destructive"}
                    className="text-xs"
                  >
                    {game.ratingChange > 0 ? "+" : ""}
                    {game.ratingChange}
                  </Badge>
                )}
              </div>
              <Button size="sm" variant="outline">
                <Eye className="h-4 w-4 mr-1" />
                Review
              </Button>
            </div>
          </div>
        ))}
        <Button variant="outline" className="w-full mt-2">
          View All Games
        </Button>
      </CardContent>
    </Card>
  );
}
