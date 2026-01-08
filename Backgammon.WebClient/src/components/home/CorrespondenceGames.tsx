import { Card, CardContent, CardDescription, CardHeader, CardTitle } from "@/components/ui/card";
import { Button } from "@/components/ui/button";
import { Badge } from "@/components/ui/badge";
import { Clock, User, Dice6, RotateCcw } from "lucide-react";

interface CorrespondenceGame {
  id: string;
  opponent: string;
  opponentRating: number;
  yourTurn: boolean;
  timePerMove: string;
  movesPlayed: number;
  matchScore: string;
  matchLength: string;
  doublingCube: boolean;
}

const mockCorrespondenceGames: CorrespondenceGame[] = [
  {
    id: "1",
    opponent: "SlowStrategist",
    opponentRating: 1876,
    yourTurn: true,
    timePerMove: "3 days",
    movesPlayed: 12,
    matchScore: "2-1",
    matchLength: "5-point",
    doublingCube: true,
  },
  {
    id: "2",
    opponent: "PatiencePlayer",
    opponentRating: 2012,
    yourTurn: false,
    timePerMove: "5 days",
    movesPlayed: 8,
    matchScore: "0-0",
    matchLength: "7-point",
    doublingCube: true,
  },
  {
    id: "3",
    opponent: "WeekendWarrior",
    opponentRating: 1654,
    yourTurn: true,
    timePerMove: "1 day",
    movesPlayed: 24,
    matchScore: "4-3",
    matchLength: "7-point",
    doublingCube: false,
  },
  {
    id: "4",
    opponent: "CasualThinker",
    opponentRating: 1789,
    yourTurn: false,
    timePerMove: "7 days",
    movesPlayed: 5,
    matchScore: "1-1",
    matchLength: "3-point",
    doublingCube: true,
  },
];

export function CorrespondenceGames() {
  const yourTurnGames = mockCorrespondenceGames.filter((game) => game.yourTurn);

  return (
    <div className="space-y-6">
      <div>
        <div className="flex items-center justify-between mb-3">
          <h3 className="text-lg font-semibold">Your Turn</h3>
          {yourTurnGames.length > 0 && (
            <Badge variant="destructive" className="animate-pulse">
              {yourTurnGames.length} waiting
            </Badge>
          )}
        </div>
        <div className="space-y-2">
          {yourTurnGames.map((game) => (
            <div
              key={game.id}
              className="flex items-center justify-between p-4 border rounded-lg hover:bg-accent transition-colors bg-green-500/5"
            >
              <div className="flex items-center gap-3">
                <User className="h-5 w-5 text-muted-foreground" />
                <div>
                  <div className="flex items-center gap-2">
                    <span className="font-semibold">{game.opponent}</span>
                    <span className="text-sm text-muted-foreground">({game.opponentRating})</span>
                  </div>
                  <div className="flex flex-wrap items-center gap-2 mt-1">
                    <Badge variant="secondary" className="text-xs flex items-center gap-1">
                      <Clock className="h-3 w-3" />
                      {game.timePerMove}/move
                    </Badge>
                    <Badge variant="outline" className="text-xs">
                      {game.matchScore} in {game.matchLength}
                    </Badge>
                    {game.doublingCube && (
                      <Badge variant="outline" className="text-xs flex items-center gap-1">
                        <Dice6 className="h-3 w-3" />
                        Cube
                      </Badge>
                    )}
                    <span className="text-xs text-muted-foreground">{game.movesPlayed} moves</span>
                  </div>
                </div>
              </div>
              <Button size="sm" className="bg-green-600 hover:bg-green-700">
                Play
              </Button>
            </div>
          ))}
        </div>
      </div>

      <div>
        <h3 className="text-lg font-semibold mb-3">Waiting for Opponent</h3>
        <div className="space-y-2">
          {mockCorrespondenceGames
            .filter((game) => !game.yourTurn)
            .map((game) => (
              <div
                key={game.id}
                className="flex items-center justify-between p-4 border rounded-lg hover:bg-accent transition-colors opacity-60"
              >
                <div className="flex items-center gap-3">
                  <User className="h-5 w-5 text-muted-foreground" />
                  <div>
                    <div className="flex items-center gap-2">
                      <span>{game.opponent}</span>
                      <span className="text-sm text-muted-foreground">({game.opponentRating})</span>
                    </div>
                    <div className="flex flex-wrap items-center gap-2 mt-1">
                      <Badge variant="secondary" className="text-xs flex items-center gap-1">
                        <Clock className="h-3 w-3" />
                        {game.timePerMove}/move
                      </Badge>
                      <Badge variant="outline" className="text-xs">
                        {game.matchScore} in {game.matchLength}
                      </Badge>
                      {game.doublingCube && (
                        <Badge variant="outline" className="text-xs flex items-center gap-1">
                          <Dice6 className="h-3 w-3" />
                          Cube
                        </Badge>
                      )}
                      <span className="text-xs text-muted-foreground">{game.movesPlayed} moves</span>
                    </div>
                  </div>
                </div>
                <Button size="sm" variant="outline">
                  View
                </Button>
              </div>
            ))}
        </div>
      </div>
    </div>
  );
}