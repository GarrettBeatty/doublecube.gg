import { Button } from "@/components/ui/button";
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from "@/components/ui/card";
import { Clock, Zap, Users } from "lucide-react";
import { Select, SelectContent, SelectItem, SelectTrigger, SelectValue } from "@/components/ui/select";
import { Switch } from "@/components/ui/switch";
import { Label } from "@/components/ui/label";
import { useState } from "react";

interface QuickPlayOption {
  id: string;
  title: string;
  time: string;
  description: string;
  icon: React.ReactNode;
  color: string;
}

const quickPlayOptions: QuickPlayOption[] = [
  {
    id: "blitz",
    title: "Blitz",
    time: "15 sec",
    description: "15 seconds per move",
    icon: <Zap className="h-6 w-6" />,
    color: "bg-red-500 hover:bg-red-600",
  },
  {
    id: "fast",
    title: "Fast",
    time: "30 sec",
    description: "30 seconds per move",
    icon: <Clock className="h-6 w-6" />,
    color: "bg-orange-500 hover:bg-orange-600",
  },
  {
    id: "standard",
    title: "Standard",
    time: "60 sec",
    description: "1 minute per move",
    icon: <Clock className="h-6 w-6" />,
    color: "bg-green-500 hover:bg-green-600",
  },
  {
    id: "casual",
    title: "Casual",
    time: "∞",
    description: "No timer",
    icon: <Users className="h-6 w-6" />,
    color: "bg-blue-500 hover:bg-blue-600",
  },
];

export function QuickPlayCard() {
  const [matchLength, setMatchLength] = useState("7");
  const [doublingCube, setDoublingCube] = useState(true);

  return (
    <Card>
      <CardHeader>
        <CardTitle>Quick Play</CardTitle>
        <CardDescription>Start a game instantly with automatic matching</CardDescription>
      </CardHeader>
      <CardContent className="space-y-4">
        {/* Match Settings */}
        <div className="grid grid-cols-2 gap-4 p-4 bg-accent rounded-lg">
          <div className="space-y-2">
            <Label htmlFor="match-length">Match Length</Label>
            <Select value={matchLength} onValueChange={setMatchLength}>
              <SelectTrigger id="match-length">
                <SelectValue />
              </SelectTrigger>
              <SelectContent>
                <SelectItem value="3">3-point</SelectItem>
                <SelectItem value="5">5-point</SelectItem>
                <SelectItem value="7">7-point</SelectItem>
                <SelectItem value="9">9-point</SelectItem>
                <SelectItem value="11">11-point</SelectItem>
              </SelectContent>
            </Select>
          </div>
          <div className="space-y-2">
            <Label htmlFor="doubling-cube">Doubling Cube</Label>
            <div className="flex items-center space-x-2 h-10">
              <Switch
                id="doubling-cube"
                checked={doublingCube}
                onCheckedChange={setDoublingCube}
              />
              <span className="text-sm text-muted-foreground">
                {doublingCube ? "Enabled" : "Disabled"}
              </span>
            </div>
          </div>
        </div>

        {/* Time Control Buttons */}
        <div className="grid grid-cols-2 gap-4">
          {quickPlayOptions.map((option) => (
            <Button
              key={option.id}
              className={`h-24 flex flex-col items-center justify-center gap-2 ${option.color} text-white`}
            >
              {option.icon}
              <div className="text-center">
                <div>{option.title}</div>
                <div className="text-xs opacity-90">{option.time}</div>
              </div>
            </Button>
          ))}
        </div>
      </CardContent>
    </Card>
  );
}