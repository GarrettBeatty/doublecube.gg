import { Card, CardContent, CardHeader, CardTitle } from "@/components/ui/card";
import { TrendingUp, TrendingDown, Trophy, Target } from "lucide-react";
import { Badge } from "@/components/ui/badge";
import { useUserStats } from "@/hooks/useUserStats";
import { Skeleton } from "@/components/ui/skeleton";

export function YourStatsCard() {
  const { stats, isLoading } = useUserStats();

  if (isLoading) {
    return (
      <Card>
        <CardHeader>
          <CardTitle className="flex items-center justify-between">
            <span>Your Stats</span>
            <Trophy className="h-5 w-5 text-yellow-500" />
          </CardTitle>
        </CardHeader>
        <CardContent className="space-y-4">
          <Skeleton className="h-20 w-full" />
          <div className="grid grid-cols-2 gap-3">
            <Skeleton className="h-24 w-full" />
            <Skeleton className="h-24 w-full" />
          </div>
          <Skeleton className="h-16 w-full" />
        </CardContent>
      </Card>
    );
  }

  if (!stats) {
    return (
      <Card>
        <CardHeader>
          <CardTitle className="flex items-center justify-between">
            <span>Your Stats</span>
            <Trophy className="h-5 w-5 text-yellow-500" />
          </CardTitle>
        </CardHeader>
        <CardContent className="text-center py-8">
          <p className="text-muted-foreground">Login to see your stats</p>
        </CardContent>
      </Card>
    );
  }

  // Calculate rating change (mock for now - backend would provide this)
  const ratingChange = 0; // TODO: Backend should provide recent rating change

  return (
    <Card>
      <CardHeader>
        <CardTitle className="flex items-center justify-between">
          <span>Your Stats</span>
          <Trophy className="h-5 w-5 text-yellow-500" />
        </CardTitle>
      </CardHeader>
      <CardContent className="space-y-4">
        {/* Rating */}
        <div className="flex items-center justify-between p-3 bg-accent rounded-lg">
          <div>
            <p className="text-sm text-muted-foreground">Rating</p>
            <div className="flex items-center gap-2">
              <span className="text-2xl font-bold">{stats.rating}</span>
              {ratingChange !== 0 && (
                <Badge
                  variant={ratingChange > 0 ? "default" : "destructive"}
                  className="flex items-center gap-1"
                >
                  {ratingChange > 0 ? (
                    <TrendingUp className="h-3 w-3" />
                  ) : (
                    <TrendingDown className="h-3 w-3" />
                  )}
                  {ratingChange > 0 ? "+" : ""}
                  {ratingChange}
                </Badge>
              )}
            </div>
          </div>
        </div>

        {/* Win/Loss Record */}
        <div className="grid grid-cols-2 gap-3">
          <div className="p-3 bg-accent rounded-lg">
            <p className="text-sm text-muted-foreground">Record</p>
            <p className="text-lg font-semibold">
              {stats.wins}W - {stats.losses}L
            </p>
            <p className="text-xs text-muted-foreground">{stats.winRate.toFixed(1)}% win rate</p>
          </div>
          <div className="p-3 bg-accent rounded-lg">
            <p className="text-sm text-muted-foreground">Current Streak</p>
            <div className="flex items-center gap-2">
              <Target className={`h-4 w-4 ${stats.streakType === "win" ? "text-green-500" : "text-red-500"}`} />
              <p className="text-lg font-semibold">
                {stats.currentStreak} {stats.streakType}
              </p>
            </div>
          </div>
        </div>

        {/* Activity */}
        <div className="p-3 bg-accent rounded-lg">
          <p className="text-sm text-muted-foreground mb-2">Activity</p>
          <div className="flex justify-between text-sm">
            <span>Today: <span className="font-semibold">{stats.gamesToday} games</span></span>
            <span>This week: <span className="font-semibold">{stats.gamesThisWeek} games</span></span>
          </div>
        </div>
      </CardContent>
    </Card>
  );
}
