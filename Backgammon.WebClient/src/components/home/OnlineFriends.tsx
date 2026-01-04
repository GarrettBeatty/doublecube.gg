import { Card, CardContent, CardDescription, CardHeader, CardTitle } from "@/components/ui/card";
import { Button } from "@/components/ui/button";
import { Avatar, AvatarFallback } from "@/components/ui/avatar";
import { Skeleton } from "@/components/ui/skeleton";
import { Users, Swords, MessageCircle } from "lucide-react";
import { useFriends } from "@/hooks/useFriends";

interface OnlineFriendsProps {
  onChallengeClick?: (friendUserId: string) => void;
}

export function OnlineFriends({ onChallengeClick }: OnlineFriendsProps) {
  const { friends, isLoading } = useFriends();

  // Filter to show only online and playing friends (not offline)
  const onlineFriends = friends.filter(f => f.status !== 'Offline');

  if (isLoading) {
    return (
      <Card>
        <CardHeader>
          <CardTitle className="flex items-center gap-2">
            <Users className="h-5 w-5 text-blue-500" />
            Online Friends
          </CardTitle>
          <CardDescription>Loading friends...</CardDescription>
        </CardHeader>
        <CardContent className="space-y-3">
          <Skeleton className="h-16 w-full" />
          <Skeleton className="h-16 w-full" />
          <Skeleton className="h-16 w-full" />
        </CardContent>
      </Card>
    );
  }

  if (onlineFriends.length === 0) {
    return (
      <Card>
        <CardHeader>
          <CardTitle className="flex items-center gap-2">
            <Users className="h-5 w-5 text-blue-500" />
            Online Friends
          </CardTitle>
          <CardDescription>No friends online</CardDescription>
        </CardHeader>
        <CardContent className="text-center py-8">
          <p className="text-muted-foreground">None of your friends are online right now</p>
        </CardContent>
      </Card>
    );
  }

  return (
    <Card>
      <CardHeader>
        <CardTitle className="flex items-center gap-2">
          <Users className="h-5 w-5 text-blue-500" />
          Online Friends
        </CardTitle>
        <CardDescription>{onlineFriends.length} friends online</CardDescription>
      </CardHeader>
      <CardContent className="space-y-3">
        {onlineFriends.map((friend) => (
          <div
            key={friend.userId}
            className="flex items-center justify-between p-3 border rounded-lg hover:bg-accent transition-colors"
          >
            <div className="flex items-center gap-3">
              <div className="relative">
                <Avatar>
                  <AvatarFallback>
                    {(friend.displayName || friend.username).slice(0, 2).toUpperCase()}
                  </AvatarFallback>
                </Avatar>
                <div
                  className={`absolute -bottom-0.5 -right-0.5 h-3 w-3 rounded-full border-2 border-background ${
                    friend.status === "Online"
                      ? "bg-green-500"
                      : friend.status === "Playing"
                      ? "bg-yellow-500"
                      : "bg-gray-500"
                  }`}
                />
              </div>
              <div>
                <p className="font-semibold">{friend.displayName || friend.username}</p>
                <div className="flex items-center gap-2 text-sm text-muted-foreground">
                  {friend.rating && <span>{friend.rating}</span>}
                  {friend.status === "Playing" && friend.currentOpponent && (
                    <>
                      <span>•</span>
                      <span className="text-yellow-500">vs {friend.currentOpponent}</span>
                    </>
                  )}
                </div>
              </div>
            </div>
            <div className="flex items-center gap-2">
              {friend.status === "Online" && onChallengeClick && (
                <Button
                  size="sm"
                  variant="outline"
                  onClick={() => onChallengeClick(friend.userId)}
                >
                  <Swords className="h-4 w-4 mr-1" />
                  Challenge
                </Button>
              )}
              <Button size="sm" variant="ghost">
                <MessageCircle className="h-4 w-4" />
              </Button>
            </div>
          </div>
        ))}
      </CardContent>
    </Card>
  );
}
