import React, { useState } from 'react'
import { useSignalR } from '@/contexts/SignalRContext'
import { useAuth } from '@/contexts/AuthContext'
import { Badge } from '@/components/ui/badge'
import { Button } from '@/components/ui/button'
import { Skeleton } from '@/components/ui/skeleton'
import { Separator } from '@/components/ui/separator'
import { Bot, Dice1, Target, Star, Gamepad2 } from 'lucide-react'
import { useToast } from '@/hooks/use-toast'
import type { BotInfoDto } from '@/types/players'

interface OnlineBotsListProps {
  bots: BotInfoDto[]
  isLoading: boolean
}

export const OnlineBotsList: React.FC<OnlineBotsListProps> = ({ bots, isLoading }) => {
  const { hub } = useSignalR()
  const { user } = useAuth()
  const { toast } = useToast()
  const [startingGame, setStartingGame] = useState<string | null>(null)

  const getIcon = (icon: string) => {
    switch (icon) {
      case 'dice':
        return <Dice1 className="h-6 w-6" />
      case 'target':
        return <Target className="h-6 w-6" />
      default:
        return <Bot className="h-6 w-6" />
    }
  }

  const getDifficultyStars = (difficulty: number) => {
    return Array.from({ length: 5 }, (_, i) => (
      <Star
        key={i}
        className={`h-4 w-4 ${i < difficulty ? 'text-yellow-500 fill-yellow-500' : 'text-muted-foreground'}`}
      />
    ))
  }

  const getDifficultyLabel = (difficulty: number) => {
    switch (difficulty) {
      case 1:
        return 'Beginner'
      case 2:
        return 'Easy'
      case 3:
        return 'Intermediate'
      case 4:
        return 'Advanced'
      case 5:
        return 'Expert'
      default:
        return 'Unknown'
    }
  }

  const handlePlayBot = async (botId: string) => {
    if (!user) {
      toast({
        title: 'Login Required',
        description: 'Please log in to play against bots.',
        variant: 'destructive'
      })
      return
    }

    setStartingGame(botId)
    try {
      // Create an AI game match - navigation handled by MatchCreated event listener
      await hub?.createMatch({
        opponentType: 'AI',
        targetScore: 1,
        displayName: user.username,
        timeControlType: 'None',
        isRated: false,
        isCorrespondence: false,
        timePerMoveDays: 0,
        aiType: botId
      })
      // The MatchCreated event handler will navigate to the game
    } catch (error) {
      console.error('Failed to start bot game:', error)
      toast({
        title: 'Error',
        description: 'Failed to start game. Please try again.',
        variant: 'destructive'
      })
    } finally {
      setStartingGame(null)
    }
  }

  if (isLoading) {
    return (
      <div className="space-y-4">
        {[1, 2].map((i) => (
          <div key={i} className="p-4 border rounded-lg">
            <div className="flex items-start gap-4">
              <Skeleton className="h-12 w-12 rounded-lg" />
              <div className="flex-1 space-y-2">
                <Skeleton className="h-5 w-32" />
                <Skeleton className="h-4 w-full" />
                <Skeleton className="h-4 w-24" />
              </div>
              <Skeleton className="h-9 w-24" />
            </div>
          </div>
        ))}
      </div>
    )
  }

  if (bots.length === 0) {
    return (
      <div className="text-center py-12">
        <Bot className="h-12 w-12 mx-auto text-muted-foreground mb-4" />
        <p className="text-muted-foreground">No bots available</p>
        <p className="text-sm text-muted-foreground mt-1">
          Check back later for AI opponents!
        </p>
      </div>
    )
  }

  return (
    <div className="space-y-4">
      {bots.map((bot, index) => (
        <div key={bot.id}>
          <div className="p-4 border rounded-lg hover:bg-muted/30 transition-colors">
            <div className="flex items-start gap-4">
              <div className="p-3 bg-accent rounded-lg">
                {getIcon(bot.icon)}
              </div>

              <div className="flex-1">
                <div className="flex items-center gap-2 mb-1">
                  <h3 className="font-semibold text-lg">{bot.name}</h3>
                  {bot.isAvailable && (
                    <Badge variant="outline" className="text-green-600 border-green-600">
                      <div className="h-2 w-2 bg-green-500 rounded-full mr-1" />
                      Available
                    </Badge>
                  )}
                </div>

                <p className="text-muted-foreground text-sm mb-3">
                  {bot.description}
                </p>

                <div className="flex items-center gap-4">
                  <div className="flex items-center gap-1">
                    {getDifficultyStars(bot.difficulty)}
                  </div>
                  <Badge variant="secondary">
                    {getDifficultyLabel(bot.difficulty)}
                  </Badge>
                </div>
              </div>

              <Button
                onClick={() => handlePlayBot(bot.id)}
                disabled={!bot.isAvailable || startingGame !== null}
                className="flex items-center gap-2"
              >
                <Gamepad2 className="h-4 w-4" />
                {startingGame === bot.id ? 'Starting...' : 'Play'}
              </Button>
            </div>
          </div>
          {index < bots.length - 1 && <Separator className="my-4" />}
        </div>
      ))}

      <div className="mt-6 p-4 bg-muted/50 rounded-lg">
        <p className="text-sm text-muted-foreground text-center">
          AI games are unrated and don't affect your ELO ranking.
          Perfect for practice and learning!
        </p>
      </div>
    </div>
  )
}
