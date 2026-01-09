import { Card, CardContent, CardDescription, CardHeader, CardTitle } from "@/components/ui/card";
import { Button } from "@/components/ui/button";
import { Badge } from "@/components/ui/badge";
import { Clock, User, Search, Filter, Dice6 } from "lucide-react";
import { Input } from "@/components/ui/input";
import { Label } from "@/components/ui/label";
import { Select, SelectContent, SelectItem, SelectTrigger, SelectValue } from "@/components/ui/select";
import { Slider } from "@/components/ui/slider";
import { Skeleton } from "@/components/ui/skeleton";
import { useState } from "react";
import { useMatchLobbies } from "@/hooks/useMatchLobbies";
import { useSignalR } from "@/contexts/SignalRContext";
import { useToast } from "@/hooks/use-toast";
import { mapMatchLobbyToLobbyGame } from "@/utils/mappers";
import type { LobbyGame } from "@/types/home.types";

export function GameLobby() {
  const { lobbies, isLoading } = useMatchLobbies();
  const { invoke } = useSignalR();
  const { toast } = useToast();

  const [showFilters, setShowFilters] = useState(false);
  const [searchQuery, setSearchQuery] = useState("");
  const [ratingRange, setRatingRange] = useState([1200, 2400]);
  const [matchLengthFilter, setMatchLengthFilter] = useState("all");
  const [ratedFilter, setRatedFilter] = useState("all");
  const [cubeFilter, setCubeFilter] = useState("all");

  const handleJoinLobby = async (matchId: string) => {
    try {
      await invoke('JoinMatch', matchId);
      // Navigation happens via MatchGameStarting event in useSignalREvents
    } catch (error) {
      console.error('Failed to join lobby:', error);
      toast({
        title: 'Cannot join lobby',
        description: 'Failed to join match lobby',
        variant: 'destructive',
      });
    }
  };

  // Map backend lobbies to UI display format, filtering out correspondence games
  const lobbyGames: LobbyGame[] = lobbies
    .filter((lobby) => !lobby.isCorrespondence)
    .map(mapMatchLobbyToLobbyGame);

  // Apply client-side filtering
  const filteredLobbies = lobbyGames.filter((game) => {
    // Search filter
    if (searchQuery && !game.creatorUsername.toLowerCase().includes(searchQuery.toLowerCase())) {
      return false;
    }

    // Rating range filter
    if (game.creatorRating !== undefined) {
      if (game.creatorRating < ratingRange[0] || game.creatorRating > ratingRange[1]) {
        return false;
      }
    }

    // Match length filter
    if (matchLengthFilter !== "all" && game.matchLength.toString() !== matchLengthFilter) {
      return false;
    }

    // Rated filter
    if (ratedFilter === "rated" && !game.isRated) {
      return false;
    }
    if (ratedFilter === "casual" && game.isRated) {
      return false;
    }

    // Cube filter
    if (cubeFilter === "with" && !game.doublingCube) {
      return false;
    }
    if (cubeFilter === "without" && game.doublingCube) {
      return false;
    }

    return true;
  });

  if (isLoading) {
    return (
      <Card>
        <CardHeader>
          <CardTitle>Game Lobby</CardTitle>
          <CardDescription>Join an open game or create your own challenge</CardDescription>
        </CardHeader>
        <CardContent className="space-y-3">
          <Skeleton className="h-10 w-full" />
          <Skeleton className="h-24 w-full" />
          <Skeleton className="h-24 w-full" />
          <Skeleton className="h-24 w-full" />
        </CardContent>
      </Card>
    );
  }

  return (
    <Card>
      <CardHeader>
        <CardTitle>Game Lobby</CardTitle>
        <CardDescription>Join an open game or create your own challenge</CardDescription>
      </CardHeader>
      <CardContent className="space-y-4">
        {/* Search and Filter Bar */}
        <div className="flex gap-2">
          <div className="relative flex-1">
            <Search className="absolute left-3 top-1/2 -translate-y-1/2 h-4 w-4 text-muted-foreground" />
            <Input
              placeholder="Search players..."
              value={searchQuery}
              onChange={(e) => setSearchQuery(e.target.value)}
              className="pl-9"
            />
          </div>
          <Button
            variant={showFilters ? "default" : "outline"}
            onClick={() => setShowFilters(!showFilters)}
          >
            <Filter className="h-4 w-4 mr-2" />
            Filters
          </Button>
        </div>

        {/* Filter Panel */}
        {showFilters && (
          <div className="p-4 bg-accent rounded-lg space-y-4">
            <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
              {/* Rating Range */}
              <div className="space-y-2">
                <Label>Rating Range: {ratingRange[0]} - {ratingRange[1]}</Label>
                <Slider
                  min={800}
                  max={2800}
                  step={50}
                  value={ratingRange}
                  onValueChange={setRatingRange}
                />
              </div>

              {/* Match Length Filter */}
              <div className="space-y-2">
                <Label htmlFor="match-length-filter">Match Length</Label>
                <Select value={matchLengthFilter} onValueChange={setMatchLengthFilter}>
                  <SelectTrigger id="match-length-filter">
                    <SelectValue />
                  </SelectTrigger>
                  <SelectContent>
                    <SelectItem value="all">All Lengths</SelectItem>
                    <SelectItem value="3">3-point</SelectItem>
                    <SelectItem value="5">5-point</SelectItem>
                    <SelectItem value="7">7-point</SelectItem>
                    <SelectItem value="9">9-point</SelectItem>
                    <SelectItem value="11">11-point</SelectItem>
                  </SelectContent>
                </Select>
              </div>

              {/* Rated Filter */}
              <div className="space-y-2">
                <Label htmlFor="rated-filter">Game Type</Label>
                <Select value={ratedFilter} onValueChange={setRatedFilter}>
                  <SelectTrigger id="rated-filter">
                    <SelectValue />
                  </SelectTrigger>
                  <SelectContent>
                    <SelectItem value="all">All Games</SelectItem>
                    <SelectItem value="rated">Rated Only</SelectItem>
                    <SelectItem value="casual">Casual Only</SelectItem>
                  </SelectContent>
                </Select>
              </div>

              {/* Cube Filter */}
              <div className="space-y-2">
                <Label htmlFor="cube-filter">Doubling Cube</Label>
                <Select value={cubeFilter} onValueChange={setCubeFilter}>
                  <SelectTrigger id="cube-filter">
                    <SelectValue />
                  </SelectTrigger>
                  <SelectContent>
                    <SelectItem value="all">Any</SelectItem>
                    <SelectItem value="with">With Cube</SelectItem>
                    <SelectItem value="without">Without Cube</SelectItem>
                  </SelectContent>
                </Select>
              </div>
            </div>
            <Button
              variant="outline"
              size="sm"
              onClick={() => {
                setRatingRange([1200, 2400]);
                setMatchLengthFilter("all");
                setRatedFilter("all");
                setCubeFilter("all");
                setSearchQuery("");
              }}
            >
              Reset Filters
            </Button>
          </div>
        )}

        {/* Game List */}
        {filteredLobbies.length === 0 ? (
          <div className="text-center py-8">
            <p className="text-muted-foreground">No lobbies match your filters</p>
            <p className="text-sm text-muted-foreground mt-2">
              {lobbyGames.length === 0 ? 'Create a game to get started!' : 'Try adjusting your filters'}
            </p>
          </div>
        ) : (
          <div className="space-y-2">
            {filteredLobbies.map((game) => (
              <div
                key={game.matchId}
                className="flex items-center justify-between p-3 border rounded-lg hover:bg-accent transition-colors"
              >
                <div className="flex items-center gap-3 flex-1">
                  <User className="h-5 w-5 text-muted-foreground" />
                  <div className="flex-1">
                    <div className="flex items-center gap-2">
                      <span className="font-semibold">{game.creatorUsername}</span>
                      {game.creatorRating && (
                        <span className="text-sm text-muted-foreground">({game.creatorRating})</span>
                      )}
                    </div>
                    <div className="flex flex-wrap items-center gap-2 mt-1">
                      {game.timeControl && (
                        <Badge variant="secondary" className="text-xs flex items-center gap-1">
                          <Clock className="h-3 w-3" />
                          {game.timeControl}
                        </Badge>
                      )}
                      <Badge variant="outline" className="text-xs">
                        {game.matchLength}-point
                      </Badge>
                      {game.isRated && (
                        <Badge variant="outline" className="text-xs">
                          Rated
                        </Badge>
                      )}
                      {game.doublingCube && (
                        <Badge variant="outline" className="text-xs flex items-center gap-1">
                          <Dice6 className="h-3 w-3" />
                          Cube
                        </Badge>
                      )}
                    </div>
                  </div>
                </div>
                <Button size="sm" onClick={() => handleJoinLobby(game.matchId)}>
                  Join
                </Button>
              </div>
            ))}
          </div>
        )}
        <Button className="w-full" variant="outline">
          Create Custom Game
        </Button>
      </CardContent>
    </Card>
  );
}