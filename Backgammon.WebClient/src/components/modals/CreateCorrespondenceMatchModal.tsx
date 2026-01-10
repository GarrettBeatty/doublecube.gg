import React, { useState } from 'react'
import { useCorrespondenceGames } from '@/hooks/useCorrespondenceGames'
import { authService } from '@/services/auth.service'
import {
  Dialog,
  DialogContent,
  DialogDescription,
  DialogHeader,
  DialogTitle,
  DialogFooter,
} from '@/components/ui/dialog'
import { Button } from '@/components/ui/button'
import { Label } from '@/components/ui/label'
import { RadioGroup, RadioGroupItem } from '@/components/ui/radio-group'
import { Switch } from '@/components/ui/switch'

interface CreateCorrespondenceMatchModalProps {
  isOpen: boolean
  onClose: () => void
  defaultOpponentType?: 'Friend' | 'OpenLobby'
  friendUserId?: string
  friendName?: string
}

export const CreateCorrespondenceMatchModal: React.FC<CreateCorrespondenceMatchModalProps> = ({
  isOpen,
  onClose,
  friendUserId,
  friendName,
}) => {
  const { createCorrespondenceMatch } = useCorrespondenceGames()
  const [opponentType, setOpponentType] = useState<'Friend' | 'OpenLobby'>(
    friendUserId ? 'Friend' : 'OpenLobby'
  )
  const [targetScore, setTargetScore] = useState<number>(5)
  const [timePerMoveDays, setTimePerMoveDays] = useState<number>(3)
  const [isRated, setIsRated] = useState<boolean>(true)
  const [isCreating, setIsCreating] = useState(false)
  const [error, setError] = useState<string | null>(null)
  const isAuthenticated = authService.isAuthenticated()

  // Reset state when modal opens
  React.useEffect(() => {
    if (isOpen) {
      setOpponentType(friendUserId ? 'Friend' : 'OpenLobby')
      setTargetScore(5)
      setTimePerMoveDays(3)
      setIsRated(true)
      setError(null)
    }
  }, [isOpen, friendUserId])

  const handleCreate = async () => {
    // Validation
    if (opponentType === 'Friend' && !friendUserId) {
      setError('Please select a friend to challenge')
      return
    }

    setIsCreating(true)
    setError(null)

    try {
      await createCorrespondenceMatch({
        opponentType,
        targetScore,
        timePerMoveDays,
        friendUserId: opponentType === 'Friend' ? friendUserId : undefined,
        isRated: isAuthenticated ? isRated : false,
      })
      onClose()
    } catch (err) {
      console.error('Failed to create correspondence match:', err)
      setError('Failed to create correspondence match. Please try again.')
    } finally {
      setIsCreating(false)
    }
  }

  return (
    <Dialog open={isOpen} onOpenChange={onClose}>
      <DialogContent className="sm:max-w-md">
        <DialogHeader>
          <DialogTitle>
            {friendName ? `Challenge ${friendName}` : 'New Correspondence Game'}
          </DialogTitle>
          <DialogDescription>
            Take your time with each move - days, not seconds
          </DialogDescription>
        </DialogHeader>

        <div className="space-y-6 py-4">
          {/* Opponent Type */}
          {!friendUserId && (
            <div className="space-y-3">
              <Label>Opponent</Label>
              <RadioGroup
                value={opponentType}
                onValueChange={(value) => setOpponentType(value as 'Friend' | 'OpenLobby')}
              >
                <div className="flex items-center space-x-2">
                  <RadioGroupItem value="OpenLobby" id="opponent-open" />
                  <Label htmlFor="opponent-open" className="font-normal cursor-pointer">
                    <div className="flex flex-col">
                      <span>Open Challenge</span>
                      <span className="text-sm text-muted-foreground">
                        Anyone can accept your challenge
                      </span>
                    </div>
                  </Label>
                </div>
                <div className="flex items-center space-x-2">
                  <RadioGroupItem value="Friend" id="opponent-friend" />
                  <Label htmlFor="opponent-friend" className="font-normal cursor-pointer">
                    <div className="flex flex-col">
                      <span>Challenge Friend</span>
                      <span className="text-sm text-muted-foreground">
                        Invite a specific friend
                      </span>
                    </div>
                  </Label>
                </div>
              </RadioGroup>
            </div>
          )}

          {/* Friend selection warning */}
          {opponentType === 'Friend' && !friendUserId && (
            <div className="p-3 bg-amber-50 border border-amber-200 rounded-md">
              <p className="text-sm text-amber-800">
                Please select a friend from your friends list to challenge them.
              </p>
            </div>
          )}

          {/* Match Length */}
          <div className="space-y-3">
            <Label>Match Length</Label>
            <RadioGroup
              value={String(targetScore)}
              onValueChange={(value) => setTargetScore(Number(value))}
            >
              <div className="flex items-center space-x-2">
                <RadioGroupItem value="1" id="score-1" />
                <Label htmlFor="score-1" className="font-normal cursor-pointer">
                  1 point (Single Game)
                </Label>
              </div>
              <div className="flex items-center space-x-2">
                <RadioGroupItem value="3" id="score-3" />
                <Label htmlFor="score-3" className="font-normal cursor-pointer">
                  3 points (Short Match)
                </Label>
              </div>
              <div className="flex items-center space-x-2">
                <RadioGroupItem value="5" id="score-5" />
                <Label htmlFor="score-5" className="font-normal cursor-pointer">
                  5 points (Medium Match)
                </Label>
              </div>
              <div className="flex items-center space-x-2">
                <RadioGroupItem value="7" id="score-7" />
                <Label htmlFor="score-7" className="font-normal cursor-pointer">
                  7 points (Long Match)
                </Label>
              </div>
            </RadioGroup>
          </div>

          {/* Time Per Move */}
          <div className="space-y-3">
            <Label>Time Per Move</Label>
            <RadioGroup
              value={String(timePerMoveDays)}
              onValueChange={(value) => setTimePerMoveDays(Number(value))}
            >
              <div className="flex items-center space-x-2">
                <RadioGroupItem value="1" id="time-1" />
                <Label htmlFor="time-1" className="font-normal cursor-pointer">
                  1 day (Fast)
                </Label>
              </div>
              <div className="flex items-center space-x-2">
                <RadioGroupItem value="3" id="time-3" />
                <Label htmlFor="time-3" className="font-normal cursor-pointer">
                  3 days (Standard)
                </Label>
              </div>
              <div className="flex items-center space-x-2">
                <RadioGroupItem value="7" id="time-7" />
                <Label htmlFor="time-7" className="font-normal cursor-pointer">
                  7 days (Relaxed)
                </Label>
              </div>
            </RadioGroup>
            <p className="text-sm text-muted-foreground">
              Each player has this much time to make their move
            </p>
          </div>

          {/* Rated/Unrated */}
          {isAuthenticated && (
            <div className="flex items-center justify-between">
              <div className="space-y-0.5">
                <Label htmlFor="rated-toggle">Rated Game</Label>
                <div className="text-sm text-muted-foreground">
                  This game will affect your rating
                </div>
              </div>
              <Switch
                id="rated-toggle"
                checked={isRated}
                onCheckedChange={setIsRated}
              />
            </div>
          )}

          {error && (
            <div className="text-sm text-red-500 bg-red-50 p-3 rounded-md">
              {error}
            </div>
          )}
        </div>

        <DialogFooter className="gap-2">
          <Button variant="outline" onClick={onClose} disabled={isCreating}>
            Cancel
          </Button>
          <Button onClick={handleCreate} disabled={isCreating}>
            {isCreating ? 'Creating...' : 'Create Game'}
          </Button>
        </DialogFooter>
      </DialogContent>
    </Dialog>
  )
}
