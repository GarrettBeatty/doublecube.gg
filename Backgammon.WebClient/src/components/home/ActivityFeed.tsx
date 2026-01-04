import { Card, CardContent, CardDescription, CardHeader, CardTitle } from "@/components/ui/card";
import { Trophy, TrendingUp, Star, Users } from "lucide-react";
import { ScrollArea } from "@/components/ui/scroll-area";

interface ActivityItem {
  id: string;
  type: "tournament" | "achievement" | "milestone" | "highRating";
  title: string;
  description: string;
  icon: React.ReactNode;
  timestamp: string;
}

const mockActivity: ActivityItem[] = [
  {
    id: "1",
    type: "tournament",
    title: "Tournament Winner",
    description: "GammonPro won the Weekly Speed Tournament",
    icon: <Trophy className="h-4 w-4 text-yellow-500" />,
    timestamp: "30 min ago",
  },
  {
    id: "2",
    type: "highRating",
    title: "New Top Player",
    description: "DoubleKing reached 2400 rating",
    icon: <TrendingUp className="h-4 w-4 text-green-500" />,
    timestamp: "1 hour ago",
  },
  {
    id: "3",
    type: "achievement",
    title: "Milestone Reached",
    description: "BackgammonMaster played their 1000th game",
    icon: <Star className="h-4 w-4 text-purple-500" />,
    timestamp: "2 hours ago",
  },
  {
    id: "4",
    type: "milestone",
    title: "Platform Growth",
    description: "10,000 players online simultaneously!",
    icon: <Users className="h-4 w-4 text-blue-500" />,
    timestamp: "3 hours ago",
  },
  {
    id: "5",
    type: "tournament",
    title: "Tournament Starting",
    description: "Monthly Championship begins in 1 hour",
    icon: <Trophy className="h-4 w-4 text-yellow-500" />,
    timestamp: "4 hours ago",
  },
];

export function ActivityFeed() {
  return (
    <Card>
      <CardHeader>
        <CardTitle>Activity Feed</CardTitle>
        <CardDescription>Recent platform highlights</CardDescription>
      </CardHeader>
      <CardContent>
        <ScrollArea className="h-[300px] pr-4">
          <div className="space-y-3">
            {mockActivity.map((item) => (
              <div
                key={item.id}
                className="flex items-start gap-3 p-3 rounded-lg hover:bg-accent transition-colors"
              >
                <div className="mt-1">{item.icon}</div>
                <div className="flex-1 min-w-0">
                  <p className="font-semibold text-sm">{item.title}</p>
                  <p className="text-sm text-muted-foreground">{item.description}</p>
                  <p className="text-xs text-muted-foreground mt-1">{item.timestamp}</p>
                </div>
              </div>
            ))}
          </div>
        </ScrollArea>
      </CardContent>
    </Card>
  );
}
