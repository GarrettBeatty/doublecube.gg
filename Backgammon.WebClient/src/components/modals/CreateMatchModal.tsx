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
  const [isCreating, setIsCreating] = useState(false)

  // Reset to default when modal opens
  React.useEffect(() => {
    if (isOpen) {
      setOpponentType(defaultOpponentType)
      setTargetScore(1)
    }
  }, [isOpen, defaultOpponentType])

  const handleCreate = async () => {
    setIsCreating(true)
    try {
      const config = {
        OpponentType: opponentType,
        TargetScore: targetScore,
        DisplayName: authService.getDisplayName() || 'Player',
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
