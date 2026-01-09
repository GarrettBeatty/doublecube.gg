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
  defaultOpponentType = 'OpenLobby',
  friendUserId,
  friendName,
}) => {
  const { createCorrespondenceMatch } = useCorrespondenceGames()
  // Note: Only Friend matches supported for correspondence games currently
  const [opponentType] = useState<'Friend'>('Friend')
  const [targetScore, setTargetScore] = useState<number>(5)
  const [timePerMoveDays, setTimePerMoveDays] = useState<number>(3)
  const [isRated, setIsRated] = useState<boolean>(true)
  const [isCreating, setIsCreating] = useState(false)
  const [error, setError] = useState<string | null>(null)
  const isAuthenticated = authService.isAuthenticated()

  // Reset state when modal opens
  React.useEffect(() => {
    if (isOpen) {
      setTargetScore(5)
      setTimePerMoveDays(3)
      setIsRated(true)
      setError(null)
    }
  }, [isOpen])

  const handleCreate = async () => {
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
          {/* Note: Correspondence games only support Friend matches currently */}
          {!friendUserId && (
            <div className="mb-4 p-3 bg-blue-50 border border-blue-200 rounded-md">
              <p className="text-sm text-blue-800">
                Correspondence games currently require a specific opponent.
                Please select a friend to challenge from your friends list.
              </p>
            </div>
          )}

          {/* Opponent Type - Hidden for now, only Friend supported */}
          {false && !friendUserId && (
            <div className="space-y-3">
              <Label>Opponent</Label>
              <RadioGroup
                value={opponentType}
                onValueChange={() => {}}
              >
                <div className="flex items-center space-x-2">
                  <RadioGroupItem value="OpenLobby" id="opponent-open" disabled />
                  <Label htmlFor="opponent-open" className="font-normal cursor-pointer opacity-50">
                    <div className="flex flex-col">
                      <span>Open Challenge (Coming Soon)</span>
                      <span className="text-sm text-muted-foreground">
                        Anyone can accept your challenge
                      </span>
                    </div>
                  </Label>
                </div>
              </RadioGroup>
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
