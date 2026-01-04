import { Card, CardContent, CardDescription, CardHeader, CardTitle } from "@/components/ui/card";
import { Button } from "@/components/ui/button";
import { Badge } from "@/components/ui/badge";
import { Sparkles, Zap, Target } from "lucide-react";

interface Variant {
  id: string;
  name: string;
  description: string;
  icon: React.ReactNode;
  playersOnline: number;
  difficulty: "Easy" | "Medium" | "Hard";
}

const variants: Variant[] = [
  {
    id: "nackgammon",
    name: "Nackgammon",
    description: "Different starting position for more tactical play",
    icon: <Target className="h-5 w-5 text-blue-500" />,
    playersOnline: 145,
    difficulty: "Medium",
  },
  {
    id: "hypergammon",
    name: "Hypergammon",
    description: "Quick 3-checker variant, fast-paced action",
    icon: <Zap className="h-5 w-5 text-yellow-500" />,
    playersOnline: 89,
    difficulty: "Easy",
  },
  {
    id: "longgammon",
    name: "Long Gammon",
    description: "All checkers start off the board",
    icon: <Sparkles className="h-5 w-5 text-purple-500" />,
    playersOnline: 34,
    difficulty: "Hard",
  },
];

export function VariantsCard() {
  return (
    <Card>
      <CardHeader>
        <CardTitle>Game Variants</CardTitle>
        <CardDescription>Try different backgammon variations</CardDescription>
      </CardHeader>
      <CardContent className="space-y-3">
        {variants.map((variant) => (
          <div
            key={variant.id}
            className="flex items-center justify-between p-4 border rounded-lg hover:bg-accent transition-colors"
          >
            <div className="flex items-center gap-3 flex-1">
              <div className="p-2 bg-accent rounded-lg">
                {variant.icon}
              </div>
              <div className="flex-1">
                <div className="flex items-center gap-2 mb-1">
                  <span className="font-semibold">{variant.name}</span>
                  <Badge variant="secondary" className="text-xs">
                    {variant.difficulty}
                  </Badge>
                </div>
                <p className="text-sm text-muted-foreground">{variant.description}</p>
                <p className="text-xs text-muted-foreground mt-1">
                  {variant.playersOnline} players online
                </p>
              </div>
            </div>
            <Button size="sm">Play</Button>
          </div>
        ))}
      </CardContent>
    </Card>
  );
}
