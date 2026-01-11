import { useState } from 'react'
import { Button } from '@/components/ui/button'
import { Avatar, AvatarFallback } from '@/components/ui/avatar'
import { Users, X, Swords, MessageCircle, ChevronUp } from 'lucide-react'
import { useFriends } from '@/hooks/useFriends'
import { cn } from '@/lib/utils'

export function FriendsWidget() {
  const [isOpen, setIsOpen] = useState(false)
  const { friends, isLoading } = useFriends()

  // Filter to show only online and playing friends
  const onlineFriends = friends.filter(f => f.status !== 'Offline')
  const onlineCount = onlineFriends.length

  const handleChallenge = (friendUserId: string) => {
    // TODO: Implement direct challenge
    console.log('Challenge friend:', friendUserId)
  }

  const handleChat = (friendUserId: string) => {
    // TODO: Implement chat
    console.log('Chat with friend:', friendUserId)
  }

  return (
    <div className="fixed bottom-4 right-4 z-50">
      {/* Expanded Panel */}
      {isOpen && (
        <div className="mb-2 w-80 bg-card border rounded-lg shadow-lg overflow-hidden animate-in slide-in-from-bottom-2 duration-200">
          {/* Header */}
          <div className="flex items-center justify-between p-3 bg-muted/50 border-b">
            <div className="flex items-center gap-2">
              <Users className="h-4 w-4 text-primary" />
              <span className="font-semibold">Friends Online</span>
              {onlineCount > 0 && (
                <span className="text-xs bg-primary text-primary-foreground px-1.5 py-0.5 rounded-full">
                  {onlineCount}
                </span>
              )}
            </div>
            <Button
              variant="ghost"
              size="icon"
              className="h-6 w-6"
              onClick={() => setIsOpen(false)}
            >
              <X className="h-4 w-4" />
            </Button>
          </div>

          {/* Friends List */}
          <div className="max-h-80 overflow-y-auto">
            {isLoading ? (
              <div className="p-4 text-center text-muted-foreground">
                Loading...
              </div>
            ) : onlineFriends.length === 0 ? (
              <div className="p-6 text-center">
                <Users className="h-8 w-8 text-muted-foreground mx-auto mb-2" />
                <p className="text-sm text-muted-foreground">
                  No friends online
                </p>
              </div>
            ) : (
              <div className="divide-y">
                {onlineFriends.map((friend) => (
                  <div
                    key={friend.userId}
                    className="flex items-center justify-between p-3 hover:bg-accent transition-colors"
                  >
                    <div className="flex items-center gap-2 min-w-0">
                      <div className="relative flex-shrink-0">
                        <Avatar className="h-8 w-8">
                          <AvatarFallback className="text-xs">
                            {(friend.displayName || friend.username).slice(0, 2).toUpperCase()}
                          </AvatarFallback>
                        </Avatar>
                        <div
                          className={cn(
                            'absolute -bottom-0.5 -right-0.5 h-2.5 w-2.5 rounded-full border-2 border-card',
                            friend.status === 'Online' ? 'bg-green-500' :
                            friend.status === 'Playing' ? 'bg-yellow-500' : 'bg-gray-500'
                          )}
                        />
                      </div>
                      <div className="min-w-0">
                        <p className="text-sm font-medium truncate">
                          {friend.displayName || friend.username}
                        </p>
                        <p className="text-xs text-muted-foreground">
                          {friend.status === 'Playing' && friend.currentOpponent
                            ? `vs ${friend.currentOpponent}`
                            : friend.rating ? `${friend.rating}` : 'Online'
                          }
                        </p>
                      </div>
                    </div>
                    <div className="flex items-center gap-1 flex-shrink-0">
                      {friend.status === 'Online' && (
                        <Button
                          size="icon"
                          variant="ghost"
                          className="h-7 w-7"
                          onClick={() => handleChallenge(friend.userId)}
                          title="Challenge"
                        >
                          <Swords className="h-3.5 w-3.5" />
                        </Button>
                      )}
                      <Button
                        size="icon"
                        variant="ghost"
                        className="h-7 w-7"
                        onClick={() => handleChat(friend.userId)}
                        title="Message"
                      >
                        <MessageCircle className="h-3.5 w-3.5" />
                      </Button>
                    </div>
                  </div>
                ))}
              </div>
            )}
          </div>
        </div>
      )}

      {/* Collapsed Button */}
      <button
        onClick={() => setIsOpen(!isOpen)}
        className={cn(
          'flex items-center gap-2 px-4 py-2.5 rounded-full shadow-lg transition-all',
          'bg-primary text-primary-foreground hover:bg-primary/90',
          isOpen && 'bg-muted text-muted-foreground hover:bg-muted/90'
        )}
      >
        <Users className="h-4 w-4" />
        <span className="font-medium text-sm">
          {onlineCount} friend{onlineCount !== 1 ? 's' : ''} online
        </span>
        <ChevronUp className={cn(
          'h-4 w-4 transition-transform',
          isOpen && 'rotate-180'
        )} />
      </button>
    </div>
  )
}
