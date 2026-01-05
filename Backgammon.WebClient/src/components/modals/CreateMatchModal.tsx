import React, { useState } from 'react'
import { useSignalR } from '@/contexts/SignalRContext'
import { HubMethods } from '@/types/signalr.types'
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

interface CreateMatchModalProps {
  isOpen: boolean
  onClose: () => void
  defaultOpponentType?: 'AI' | 'OpenLobby'
}

export const CreateMatchModal: React.FC<CreateMatchModalProps> = ({
  isOpen,
  onClose,
  defaultOpponentType = 'AI',
}) => {
  const { invoke } = useSignalR()
  const [opponentType, setOpponentType] = useState<'AI' | 'OpenLobby'>(defaultOpponentType)
  const [targetScore, setTargetScore] = useState<number>(1)
  const [timeControlType, setTimeControlType] = useState<'None' | 'ChicagoPoint'>('None')
  const [isRated, setIsRated] = useState<boolean>(true)
  const [isCreating, setIsCreating] = useState(false)
  const isAuthenticated = authService.isAuthenticated()

  // Reset to default when modal opens
  React.useEffect(() => {
    if (isOpen) {
      setOpponentType(defaultOpponentType)
      setTargetScore(1)
      setTimeControlType('None')
      setIsRated(true)
    }
  }, [isOpen, defaultOpponentType])

  const handleCreate = async () => {
    setIsCreating(true)
    try {
      // Determine if game can be rated
      const canBeRated = isAuthenticated && opponentType !== 'AI'

      const config = {
        OpponentType: opponentType,
        TargetScore: targetScore,
        DisplayName: authService.getDisplayName() || 'Player',
        TimeControlType: timeControlType,
        IsRated: canBeRated ? isRated : false, // Only rated if authenticated and not AI
      }
      console.log('[CreateMatchModal] Invoking CreateMatch', config)
      // Always use the match system - single games are just matches with targetScore=1
      await invoke(HubMethods.CreateMatch, config)
      console.log('[CreateMatchModal] CreateMatch completed')
      onClose()
    } catch (error) {
      console.error('[CreateMatchModal] Failed to create match:', error)
    } finally {
      setIsCreating(false)
    }
  }

  return (
    <Dialog open={isOpen} onOpenChange={onClose}>
      <DialogContent>
        <DialogHeader>
          <DialogTitle>
            {opponentType === 'AI' ? 'Play vs Computer' : 'Play Online'}
          </DialogTitle>
          <DialogDescription>
            Choose match length
          </DialogDescription>
        </DialogHeader>

        <div className="space-y-6 py-4">
          {/* Match Length */}
          <div className="space-y-3">
            <Label>Match Length</Label>
            <RadioGroup value={String(targetScore)} onValueChange={(value) => setTargetScore(Number(value))}>
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

          {/* Time Control */}
          <div className="space-y-3">
            <Label>Time Control</Label>
            <RadioGroup value={timeControlType} onValueChange={(value) => setTimeControlType(value as 'None' | 'ChicagoPoint')}>
              <div className="flex items-center space-x-2">
                <RadioGroupItem value="None" id="time-none" />
                <Label htmlFor="time-none" className="font-normal cursor-pointer">
                  <div className="flex flex-col">
                    <span>Casual (No Timer)</span>
                    <span className="text-sm text-muted-foreground">Unlimited time to think</span>
                  </div>
                </Label>
              </div>
              <div className="flex items-center space-x-2">
                <RadioGroupItem value="ChicagoPoint" id="time-chicago" />
                <Label htmlFor="time-chicago" className="font-normal cursor-pointer">
                  <div className="flex flex-col">
                    <span>Chicago Point</span>
                    <span className="text-sm text-muted-foreground">
                      12s delay + {2 * targetScore}min reserve
                    </span>
                  </div>
                </Label>
              </div>
            </RadioGroup>
            {timeControlType === 'ChicagoPoint' && (
              <p className="text-sm text-muted-foreground">
                Reserve time adjusts as match progresses: 2min per point remaining
              </p>
            )}
          </div>

          {/* Rated/Unrated - Only show for authenticated users playing online */}
          {isAuthenticated && opponentType !== 'AI' && (
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
