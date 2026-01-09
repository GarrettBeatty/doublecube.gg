import { Card, CardContent, CardDescription, CardHeader, CardTitle } from "@/components/ui/card";
import { Button } from "@/components/ui/button";
import { Skeleton } from "@/components/ui/skeleton";
import { Users, Swords, Bot } from "lucide-react";
import { useRecentOpponents } from "@/hooks/useRecentOpponents";
import { formatDistanceToNow } from "date-fns";

interface RecentOpponentsProps {
  onChallengeClick?: (opponentId: string, opponentName: string) => void;
}

export function RecentOpponents({ onChallengeClick }: RecentOpponentsProps) {
  const { opponents, isLoading } = useRecentOpponents(5);

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
          <CardTitle className="flex items-center gap-2">
            <Users className="h-5 w-5" />
            Recent Opponents
          </CardTitle>
          <CardDescription>Players you've faced recently</CardDescription>
        </CardHeader>
        <CardContent className="space-y-3">
          <Skeleton className="h-16 w-full" />
          <Skeleton className="h-16 w-full" />
          <Skeleton className="h-16 w-full" />
        </CardContent>
      </Card>
    );
  }

  if (opponents.length === 0) {
    return (
      <Card>
        <CardHeader>
          <CardTitle className="flex items-center gap-2">
            <Users className="h-5 w-5" />
            Recent Opponents
          </CardTitle>
          <CardDescription>Players you've faced recently</CardDescription>
        </CardHeader>
        <CardContent className="text-center py-8">
          <p className="text-muted-foreground">No opponents yet</p>
          <p className="text-sm text-muted-foreground mt-2">Play some matches to see your opponents here!</p>
        </CardContent>
      </Card>
    );
  }

  return (
    <Card>
      <CardHeader>
        <CardTitle className="flex items-center gap-2">
          <Users className="h-5 w-5" />
          Recent Opponents
        </CardTitle>
        <CardDescription>Players you've faced recently</CardDescription>
      </CardHeader>
      <CardContent className="space-y-3">
        {opponents.map((opponent) => (
          <div
            key={opponent.opponentId}
            className="flex items-center justify-between p-3 border rounded-lg hover:bg-accent transition-colors"
          >
            <div className="flex items-center gap-3">
              {opponent.isAi ? (
                <Bot className="h-5 w-5 text-muted-foreground" />
              ) : (
                <div className="h-8 w-8 rounded-full bg-primary/10 flex items-center justify-center">
                  <span className="text-sm font-medium">
                    {opponent.opponentName.charAt(0).toUpperCase()}
                  </span>
                </div>
              )}
              <div>
                <div className="flex items-center gap-2">
                  <span className="font-semibold">{opponent.opponentName}</span>
                  {!opponent.isAi && opponent.opponentRating > 0 && (
                    <span className="text-muted-foreground text-sm">({opponent.opponentRating})</span>
                  )}
                </div>
                <div className="flex items-center gap-2 text-sm text-muted-foreground">
                  <span>{opponent.totalMatches} {opponent.totalMatches === 1 ? 'match' : 'matches'}</span>
                  <span>-</span>
                  <span>{formatDate(opponent.lastPlayedAt)}</span>
                </div>
              </div>
            </div>
            <div className="flex items-center gap-3">
              <div className="text-right">
                <p className={`font-semibold ${opponent.wins > opponent.losses ? 'text-green-600' : opponent.wins < opponent.losses ? 'text-red-600' : 'text-muted-foreground'}`}>
                  {opponent.record}
                </p>
                <p className="text-xs text-muted-foreground">
                  {opponent.winRate}% win rate
                </p>
              </div>
              {!opponent.isAi && onChallengeClick && (
                <Button
                  size="sm"
                  variant="outline"
                  onClick={() => onChallengeClick(opponent.opponentId, opponent.opponentName)}
                >
                  <Swords className="h-4 w-4 mr-1" />
                  Challenge
                </Button>
              )}
            </div>
          </div>
        ))}
      </CardContent>
    </Card>
  );
}
