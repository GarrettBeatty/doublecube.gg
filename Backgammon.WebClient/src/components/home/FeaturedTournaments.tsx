import { Card, CardContent, CardDescription, CardHeader, CardTitle } from "@/components/ui/card";
import { Button } from "@/components/ui/button";
import { Badge } from "@/components/ui/badge";
import { Trophy, Users, Clock, Calendar } from "lucide-react";

interface Tournament {
  id: string;
  name: string;
  type: string;
  prize: string;
  participants: number;
  maxParticipants: number;
  startTime: string;
  timeControl: string;
  matchLength: string;
  status: "upcoming" | "registration" | "live";
}

const mockTournaments: Tournament[] = [
  {
    id: "1",
    name: "Weekly Speed Championship",
    type: "Swiss",
    prize: "Premium Membership",
    participants: 87,
    maxParticipants: 128,
    startTime: "Starts in 2h 34m",
    timeControl: "15s/move",
    matchLength: "5-point",
    status: "registration",
  },
  {
    id: "2",
    name: "Monthly Masters Tournament",
    type: "Knockout",
    prize: "$500 Prize Pool",
    participants: 45,
    maxParticipants: 64,
    startTime: "Jan 15, 7:00 PM",
    timeControl: "60s/move",
    matchLength: "7-point",
    status: "upcoming",
  },
  {
    id: "3",
    name: "Rapid Fire Arena",
    type: "Arena",
    prize: "Rating Points",
    participants: 156,
    maxParticipants: 999,
    startTime: "Live Now!",
    timeControl: "30s/move",
    matchLength: "3-point",
    status: "live",
  },
];

export function FeaturedTournaments() {
  return (
    <Card>
      <CardHeader>
        <CardTitle className="flex items-center gap-2">
          <Trophy className="h-5 w-5 text-yellow-500" />
          Featured Tournaments
        </CardTitle>
        <CardDescription>Join competitive tournaments</CardDescription>
      </CardHeader>
      <CardContent className="space-y-4">
        {mockTournaments.map((tournament) => (
          <div
            key={tournament.id}
            className="p-4 border rounded-lg hover:bg-accent transition-colors space-y-3"
          >
            <div className="flex items-start justify-between">
              <div className="flex-1">
                <div className="flex items-center gap-2 mb-1">
                  <h3 className="font-semibold">{tournament.name}</h3>
                  {tournament.status === "live" && (
                    <Badge variant="destructive" className="animate-pulse">
                      LIVE
                    </Badge>
                  )}
                </div>
                <div className="flex flex-wrap gap-2 text-xs text-muted-foreground">
                  <span className="flex items-center gap-1">
                    <Calendar className="h-3 w-3" />
                    {tournament.type}
                  </span>
                  <span>•</span>
                  <span>{tournament.matchLength} matches</span>
                  <span>•</span>
                  <span>{tournament.timeControl}</span>
                </div>
              </div>
            </div>

            <div className="flex items-center justify-between">
              <div className="space-y-1 text-sm">
                <div className="flex items-center gap-2 text-muted-foreground">
                  <Users className="h-4 w-4" />
                  <span>
                    {tournament.participants}/{tournament.maxParticipants} players
                  </span>
                </div>
                <div className="flex items-center gap-2 text-muted-foreground">
                  <Clock className="h-4 w-4" />
                  <span>{tournament.startTime}</span>
                </div>
                <div className="flex items-center gap-2">
                  <Trophy className="h-4 w-4 text-yellow-500" />
                  <span className="font-semibold">{tournament.prize}</span>
                </div>
              </div>
              <Button size="sm" variant={tournament.status === "live" ? "default" : "outline"}>
                {tournament.status === "live" ? "Join Now" : "Register"}
              </Button>
            </div>
          </div>
        ))}
      </CardContent>
    </Card>
  );
}
