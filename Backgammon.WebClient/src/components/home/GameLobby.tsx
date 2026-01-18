import { Card, CardContent, CardDescription, CardHeader, CardTitle } from "@/components/ui/card";
import { Button } from "@/components/ui/button";
import { Badge } from "@/components/ui/badge";
import { Clock, User, Dice6, Filter, Users, Plus } from "lucide-react";
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

interface GameLobbyProps {
  onCreateGame?: () => void;
}

export function GameLobby({ onCreateGame }: GameLobbyProps) {
  const { lobbies, isLoading } = useMatchLobbies();
  const { hub } = useSignalR();
  const { toast } = useToast();

  const [showFilters, setShowFilters] = useState(false);
  const [ratingRange, setRatingRange] = useState([1200, 2400]);
  const [matchLengthFilter, setMatchLengthFilter] = useState("all");
  const [ratedFilter, setRatedFilter] = useState("all");
  const [cubeFilter, setCubeFilter] = useState("all");

  const handleJoinLobby = async (matchId: string) => {
    try {
      await hub?.joinMatch(matchId);
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

  // Map backend lobbies to UI display format
  // Backend already filters to return only regular (non-correspondence) lobbies
  const lobbyGames: LobbyGame[] = lobbies.map(mapMatchLobbyToLobbyGame);

  // Count active filters
  const activeFilterCount = [
    ratingRange[0] !== 1200 || ratingRange[1] !== 2400,
    matchLengthFilter !== "all",
    ratedFilter !== "all",
    cubeFilter !== "all",
  ].filter(Boolean).length;

  // Apply client-side filtering
  const filteredLobbies = lobbyGames.filter((game) => {
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
      <CardHeader className="flex flex-row items-center justify-between space-y-0 pb-2">
        <div>
          <CardTitle>Game Lobby</CardTitle>
          <CardDescription>Join an open game or create your own</CardDescription>
        </div>
        <Button
          variant={showFilters ? "secondary" : "ghost"}
          size="sm"
          onClick={() => setShowFilters(!showFilters)}
          className="gap-2"
        >
          <Filter className="h-4 w-4" />
          <span>Filters</span>
          {activeFilterCount > 0 && (
            <Badge variant="default" className="h-5 w-5 p-0 flex items-center justify-center text-xs rounded-full">
              {activeFilterCount}
            </Badge>
          )}
        </Button>
      </CardHeader>
      <CardContent className="space-y-4">
        {/* Quick Filter Chips - always visible */}
        <div className="flex flex-wrap gap-2">
          <button
            onClick={() => setRatedFilter(ratedFilter === "rated" ? "all" : "rated")}
            className={`px-3 py-1.5 text-sm rounded-full border transition-colors ${
              ratedFilter === "rated"
                ? "bg-primary text-primary-foreground border-primary"
                : "bg-background hover:bg-accent border-border"
            }`}
          >
            Rated Only
          </button>
          <button
            onClick={() => setMatchLengthFilter(matchLengthFilter === "5" ? "all" : "5")}
            className={`px-3 py-1.5 text-sm rounded-full border transition-colors ${
              matchLengthFilter === "5"
                ? "bg-primary text-primary-foreground border-primary"
                : "bg-background hover:bg-accent border-border"
            }`}
          >
            5-point
          </button>
          <button
            onClick={() => setCubeFilter(cubeFilter === "with" ? "all" : "with")}
            className={`px-3 py-1.5 text-sm rounded-full border transition-colors ${
              cubeFilter === "with"
                ? "bg-primary text-primary-foreground border-primary"
                : "bg-background hover:bg-accent border-border"
            }`}
          >
            With Cube
          </button>
        </div>

        {/* Advanced Filter Panel */}
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
              }}
            >
              Reset Filters
            </Button>
          </div>
        )}

        {/* Game List */}
        {filteredLobbies.length === 0 ? (
          <div className="text-center py-12 space-y-4">
            <div className="mx-auto w-16 h-16 rounded-full bg-muted flex items-center justify-center">
              <Users className="h-8 w-8 text-muted-foreground" />
            </div>
            {lobbyGames.length === 0 ? (
              <>
                <div>
                  <p className="font-medium text-lg">No open games yet</p>
                  <p className="text-sm text-muted-foreground mt-1">
                    Be the first to create a game and others will join!
                  </p>
                </div>
                <Button onClick={onCreateGame} className="mt-4">
                  <Plus className="h-4 w-4 mr-2" />
                  Create a Game
                </Button>
              </>
            ) : (
              <>
                <div>
                  <p className="font-medium text-lg">No matching lobbies</p>
                  <p className="text-sm text-muted-foreground mt-1">
                    {activeFilterCount} filter{activeFilterCount !== 1 ? 's' : ''} active. Try adjusting your filters or create a new game.
                  </p>
                </div>
                <div className="flex gap-2 justify-center">
                  <Button
                    variant="outline"
                    onClick={() => {
                      setRatingRange([1200, 2400]);
                      setMatchLengthFilter("all");
                      setRatedFilter("all");
                      setCubeFilter("all");
                    }}
                  >
                    Clear Filters
                  </Button>
                  <Button onClick={onCreateGame}>
                    <Plus className="h-4 w-4 mr-2" />
                    Create Game
                  </Button>
                </div>
              </>
            )}
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
            {/* Show count and create option after list */}
            <div className="pt-4 flex items-center justify-between text-sm text-muted-foreground border-t mt-4">
              <span>{filteredLobbies.length} game{filteredLobbies.length !== 1 ? 's' : ''} available</span>
              <Button variant="ghost" size="sm" onClick={onCreateGame}>
                <Plus className="h-4 w-4 mr-1" />
                Create your own
              </Button>
            </div>
          </div>
        )}
      </CardContent>
    </Card>
  );
}