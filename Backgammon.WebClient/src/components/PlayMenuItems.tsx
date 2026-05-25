import { DropdownMenuItem } from '@/components/ui/dropdown-menu'
import { Plus, Bot, UserPlus, Mail } from 'lucide-react'

export type PlayChoice = 'quick' | 'computer' | 'friend' | 'correspondence'

interface PlayMenuItemsProps {
  onChoose: (choice: PlayChoice) => void
  variant?: 'compact' | 'full'
}

export function PlayMenuItems({ onChoose, variant = 'full' }: PlayMenuItemsProps) {
  const compact = variant === 'compact'
  return (
    <>
      <DropdownMenuItem onClick={() => onChoose('quick')} className={compact ? '' : 'gap-3 py-3'}>
        <Plus className={compact ? 'h-4 w-4 mr-2' : 'h-5 w-5 text-muted-foreground'} />
        {compact ? (
          'Quick play'
        ) : (
          <div>
            <div className="font-medium">Quick play</div>
            <div className="text-xs text-muted-foreground">Create an open lobby</div>
          </div>
        )}
      </DropdownMenuItem>
      <DropdownMenuItem onClick={() => onChoose('computer')} className={compact ? '' : 'gap-3 py-3'}>
        <Bot className={compact ? 'h-4 w-4 mr-2' : 'h-5 w-5 text-muted-foreground'} />
        {compact ? (
          'Play computer'
        ) : (
          <div>
            <div className="font-medium">Play computer</div>
            <div className="text-xs text-muted-foreground">Practice or play offline</div>
          </div>
        )}
      </DropdownMenuItem>
      <DropdownMenuItem onClick={() => onChoose('friend')} className={compact ? '' : 'gap-3 py-3'}>
        <UserPlus className={compact ? 'h-4 w-4 mr-2' : 'h-5 w-5 text-muted-foreground'} />
        {compact ? (
          'Challenge friend'
        ) : (
          <div>
            <div className="font-medium">Challenge friend</div>
            <div className="text-xs text-muted-foreground">Invite someone you know</div>
          </div>
        )}
      </DropdownMenuItem>
      <DropdownMenuItem onClick={() => onChoose('correspondence')} className={compact ? '' : 'gap-3 py-3'}>
        <Mail className={compact ? 'h-4 w-4 mr-2' : 'h-5 w-5 text-muted-foreground'} />
        {compact ? (
          'Correspondence'
        ) : (
          <div>
            <div className="font-medium">Correspondence</div>
            <div className="text-xs text-muted-foreground">Play at your own pace</div>
          </div>
        )}
      </DropdownMenuItem>
    </>
  )
}
