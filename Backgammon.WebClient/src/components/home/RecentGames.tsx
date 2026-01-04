import { Card, CardContent, CardDescription, CardHeader, CardTitle } from "@/components/ui/card";
import { Button } from "@/components/ui/button";
import { Badge } from "@/components/ui/badge";
import { Trophy, X, Eye } from "lucide-react";

interface RecentGame {
  id: string;
  opponent: string;
  opponentRating: number;
  result: "win" | "loss";
  matchScore: string;
  timeControl: string;
  matchLength: string;
  ratingChange: number;
  date: string;
}

const mockRecentGames: RecentGame[] = [
  {
    id: "1",
    opponent: "BackgammonMaster",
    opponentRating: 2150,
    result: "win",
    matchScore: "7-5",
    timeControl: "30s/move",
    matchLength: "7-point",
    ratingChange: +8,
    date: "2 hours ago",
  },
  {
    id: "2",
    opponent: "DiceWarrior",
    opponentRating: 1790,
    result: "loss",
    matchScore: "4-7",
    timeControl: "60s/move",
    matchLength: "7-point",
    ratingChange: -6,
    date: "5 hours ago",
  },
  {
    id: "3",
    opponent: "CheckerChamp",
    opponentRating: 1920,
    result: "win",
    matchScore: "5-3",
    timeControl: "45s/move",
    matchLength: "5-point",
    ratingChange: +7,
    date: "Yesterday",
  },
];

export function RecentGames() {
  return (
    <Card>
      <CardHeader>
        <CardTitle>Recent Games</CardTitle>
        <CardDescription>Review your recent matches</CardDescription>
      </CardHeader>
      <CardContent className="space-y-3">
        {mockRecentGames.map((game) => (
          <div
            key={game.id}
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
                  <span className="font-semibold">{game.opponent}</span>
                  <span className="text-muted-foreground text-sm">({game.opponentRating})</span>
                </div>
                <div className="flex items-center gap-2 text-sm text-muted-foreground">
                  <span>{game.matchLength} match</span>
                  <span>•</span>
                  <span>{game.timeControl}</span>
                  <span>•</span>
                  <span>{game.date}</span>
                </div>
              </div>
            </div>
            <div className="flex items-center gap-3">
              <div className="text-right">
                <p className="font-semibold">{game.matchScore}</p>
                <Badge
                  variant={game.result === "win" ? "default" : "destructive"}
                  className="text-xs"
                >
                  {game.ratingChange > 0 ? "+" : ""}
                  {game.ratingChange}
                </Badge>
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
