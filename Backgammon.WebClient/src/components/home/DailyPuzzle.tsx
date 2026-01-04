import { Card, CardContent, CardDescription, CardHeader, CardTitle } from "@/components/ui/card";
import { Button } from "@/components/ui/button";
import { Badge } from "@/components/ui/badge";
import { Brain, Trophy } from "lucide-react";

export function DailyPuzzle() {
  return (
    <Card>
      <CardHeader>
        <CardTitle className="flex items-center gap-2">
          <Brain className="h-5 w-5 text-purple-500" />
          Daily Puzzle
        </CardTitle>
        <CardDescription>Improve your skills with today's position</CardDescription>
      </CardHeader>
      <CardContent className="space-y-4">
        <div className="relative bg-gradient-to-br from-purple-500/10 to-blue-500/10 rounded-lg p-6 border-2 border-dashed border-purple-500/30">
          <div className="text-center space-y-2">
            <Badge variant="secondary" className="mb-2">
              Checker Play
            </Badge>
            <p className="text-sm text-muted-foreground">
              You rolled 6-4. What's the best play?
            </p>
            <div className="text-xs text-muted-foreground mt-2">
              Difficulty: ⭐⭐⭐
            </div>
          </div>
        </div>
        
        <div className="flex items-center justify-between">
          <div className="text-sm">
            <p className="text-muted-foreground">Your streak</p>
            <div className="flex items-center gap-1">
              <Trophy className="h-4 w-4 text-yellow-500" />
              <span className="font-semibold">12 days</span>
            </div>
          </div>
          <Button>Solve Puzzle</Button>
        </div>
      </CardContent>
    </Card>
  );
}
