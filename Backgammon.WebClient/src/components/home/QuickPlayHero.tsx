import { ReactNode } from 'react'
import { Card, CardContent } from '@/components/ui/card'
import { Plus, Bot, UserPlus } from 'lucide-react'
import { cn } from '@/lib/utils'

interface ActionCardProps {
  title: string
  description: string
  icon: ReactNode
  onClick: () => void
  variant?: 'primary' | 'secondary'
}

function ActionCard({
  title,
  description,
  icon,
  onClick,
  variant = 'secondary',
}: ActionCardProps) {
  return (
    <Card
      className={cn(
        'cursor-pointer transition-all duration-200 hover:scale-[1.02] hover:shadow-lg',
        variant === 'primary'
          ? 'bg-primary text-primary-foreground hover:bg-primary/90'
          : 'bg-card hover:bg-accent'
      )}
      onClick={onClick}
    >
      <CardContent className="p-6 flex flex-col items-center text-center gap-3">
        <div
          className={cn(
            'p-4 rounded-full',
            variant === 'primary'
              ? 'bg-primary-foreground/20'
              : 'bg-primary/10'
          )}
        >
          <div className={cn(
            'h-8 w-8 flex items-center justify-center',
            variant === 'primary' ? 'text-primary-foreground' : 'text-primary'
          )}>
            {icon}
          </div>
        </div>
        <div>
          <h3 className="font-semibold text-lg">{title}</h3>
          <p
            className={cn(
              'text-sm mt-1',
              variant === 'primary'
                ? 'text-primary-foreground/80'
                : 'text-muted-foreground'
            )}
          >
            {description}
          </p>
        </div>
      </CardContent>
    </Card>
  )
}

interface QuickPlayHeroProps {
  onCreateGame: () => void
  onPlayComputer: () => void
  onChallengeFriend: () => void
}

export function QuickPlayHero({
  onCreateGame,
  onPlayComputer,
  onChallengeFriend,
}: QuickPlayHeroProps) {
  return (
    <section className="mb-8">
      <div className="grid grid-cols-1 md:grid-cols-3 gap-4">
        <ActionCard
          title="Play Online"
          description="Create or join a game lobby"
          icon={<Plus className="h-8 w-8" />}
          variant="primary"
          onClick={onCreateGame}
        />
        <ActionCard
          title="Play Computer"
          description="Practice against AI"
          icon={<Bot className="h-8 w-8" />}
          onClick={onPlayComputer}
        />
        <ActionCard
          title="Challenge Friend"
          description="Invite someone you know"
          icon={<UserPlus className="h-8 w-8" />}
          onClick={onChallengeFriend}
        />
      </div>
    </section>
  )
}
