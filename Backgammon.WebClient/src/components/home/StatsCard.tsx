import { Card, CardContent, CardHeader, CardTitle } from "@/components/ui/card";
import { Users, Gamepad, Trophy } from "lucide-react";

interface StatItem {
  label: string;
  value: string;
  icon: React.ReactNode;
}

const stats: StatItem[] = [
  {
    label: "Players Online",
    value: "12,543",
    icon: <Users className="h-5 w-5 text-green-500" />,
  },
  {
    label: "Games Playing",
    value: "3,287",
    icon: <Gamepad className="h-5 w-5 text-blue-500" />,
  },
  {
    label: "Tournaments Active",
    value: "14",
    icon: <Trophy className="h-5 w-5 text-yellow-500" />,
  },
];

export function StatsCard() {
  return (
    <Card>
      <CardHeader>
        <CardTitle>Server Stats</CardTitle>
      </CardHeader>
      <CardContent className="grid grid-cols-3 gap-4">
        {stats.map((stat, index) => (
          <div key={index} className="flex flex-col items-center text-center">
            <div className="mb-2">{stat.icon}</div>
            <div className="text-muted-foreground text-sm">{stat.label}</div>
          </div>
        ))}
      </CardContent>
    </Card>
  );
}
