import { useEffect, useState } from "react";
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from "@/components/ui/card";
import { LineChart, Line, XAxis, YAxis, CartesianGrid, Tooltip, ResponsiveContainer } from "recharts";
import { TrendingUp } from "lucide-react";
import { useSignalR } from "@/contexts/SignalRContext";
import { Skeleton } from "@/components/ui/skeleton";
import type { RatingHistoryEntryDto } from "@/types/generated/Backgammon.Server.Models";

interface ChartDataPoint {
  date: string;
  rating: number;
  change: number;
  opponent?: string;
  won: boolean;
}

export function PerformanceGraph() {
  const { hub, isConnected } = useSignalR();
  const [chartData, setChartData] = useState<ChartDataPoint[]>([]);
  const [isLoading, setIsLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    const fetchRatingHistory = async () => {
      if (!isConnected || !hub) {
        setIsLoading(false);
        return;
      }

      try {
        setIsLoading(true);
        setError(null);

        const history = await hub.getRatingHistory(30);

        if (history && history.length > 0) {
          // Transform the data for the chart (reverse to show oldest first)
          const data = history.reverse().map((entry: RatingHistoryEntryDto) => {
            const date = new Date(entry.timestamp);
            return {
              date: date.toLocaleDateString('en-US', { month: 'short', day: 'numeric' }),
              rating: entry.rating,
              change: entry.ratingChange,
              opponent: entry.opponentUsername ?? undefined,
              won: entry.won,
            };
          });
          setChartData(data);
        } else {
          setChartData([]);
        }
      } catch (err) {
        console.error('Failed to fetch rating history:', err);
        setError('Failed to load rating history');
        setChartData([]);
      } finally {
        setIsLoading(false);
      }
    };

    fetchRatingHistory();
  }, [isConnected, hub]);

  if (isLoading) {
    return (
      <Card>
        <CardHeader>
          <CardTitle className="flex items-center gap-2">
            <TrendingUp className="h-5 w-5 text-green-500" />
            Rating Progress
          </CardTitle>
          <CardDescription>Your rating over recent games</CardDescription>
        </CardHeader>
        <CardContent>
          <Skeleton className="h-[200px] w-full" />
        </CardContent>
      </Card>
    );
  }

  if (error) {
    return (
      <Card>
        <CardHeader>
          <CardTitle className="flex items-center gap-2">
            <TrendingUp className="h-5 w-5 text-green-500" />
            Rating Progress
          </CardTitle>
          <CardDescription>Your rating over recent games</CardDescription>
        </CardHeader>
        <CardContent>
          <div className="h-[200px] flex items-center justify-center text-muted-foreground">
            {error}
          </div>
        </CardContent>
      </Card>
    );
  }

  if (chartData.length === 0) {
    return (
      <Card>
        <CardHeader>
          <CardTitle className="flex items-center gap-2">
            <TrendingUp className="h-5 w-5 text-green-500" />
            Rating Progress
          </CardTitle>
          <CardDescription>Your rating over recent games</CardDescription>
        </CardHeader>
        <CardContent>
          <div className="h-[200px] flex items-center justify-center text-muted-foreground">
            No rated games yet. Play some rated matches to see your progress!
          </div>
        </CardContent>
      </Card>
    );
  }

  return (
    <Card>
      <CardHeader>
        <CardTitle className="flex items-center gap-2">
          <TrendingUp className="h-5 w-5 text-green-500" />
          Rating Progress
        </CardTitle>
        <CardDescription>Your rating over the last {chartData.length} rated games</CardDescription>
      </CardHeader>
      <CardContent>
        <ResponsiveContainer width="100%" height={200}>
          <LineChart data={chartData}>
            <CartesianGrid strokeDasharray="3 3" stroke="hsl(var(--border))" />
            <XAxis
              dataKey="date"
              stroke="hsl(var(--muted-foreground))"
              fontSize={12}
            />
            <YAxis
              stroke="hsl(var(--muted-foreground))"
              fontSize={12}
              domain={['dataMin - 20', 'dataMax + 20']}
            />
            <Tooltip
              contentStyle={{
                backgroundColor: "hsl(var(--popover))",
                border: "1px solid hsl(var(--border))",
                borderRadius: "6px",
              }}
              formatter={(value, _name, props) => {
                const { change, opponent, won } = props.payload as ChartDataPoint;
                const changeStr = change >= 0 ? `+${change}` : `${change}`;
                const result = won ? 'Won' : 'Lost';
                return [
                  `${value} (${changeStr})`,
                  opponent ? `${result} vs ${opponent}` : result
                ];
              }}
              labelFormatter={(label) => `Date: ${label}`}
            />
            <Line
              type="monotone"
              dataKey="rating"
              stroke="hsl(var(--primary))"
              strokeWidth={2}
              dot={{ fill: "hsl(var(--primary))", r: 4 }}
            />
          </LineChart>
        </ResponsiveContainer>
      </CardContent>
    </Card>
  );
}
