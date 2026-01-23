import React, { useState } from 'react'
import { useSignalR } from '@/contexts/SignalRContext'
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
  const { hub } = useSignalR()
  const [opponentType, setOpponentType] = useState<'AI' | 'OpenLobby'>(defaultOpponentType)
  const [targetScore, setTargetScore] = useState<number>(1)
  const [isRated, setIsRated] = useState<boolean>(true)
  const [aiType, setAiType] = useState<'greedy' | 'random' | 'gnubg_easy' | 'gnubg_medium' | 'gnubg_hard' | 'gnubg_expert'>('greedy')
  const [isCreating, setIsCreating] = useState(false)
  const isAuthenticated = authService.isAuthenticated()

  // Reset to default when modal opens
  React.useEffect(() => {
    if (isOpen) {
      setOpponentType(defaultOpponentType)
      setTargetScore(1)
      setIsRated(true)
      setAiType('greedy')
    }
  }, [isOpen, defaultOpponentType])

  // Force rated to false when AI opponent is selected
  React.useEffect(() => {
    if (opponentType === 'AI') {
      setIsRated(false)
    }
  }, [opponentType])

  const handleCreate = async () => {
    setIsCreating(true)
    try {
      // Determine if game can be rated
      const canBeRated = isAuthenticated && opponentType !== 'AI'

      const config = {
        opponentType: opponentType,
        targetScore: targetScore,
        displayName: authService.getDisplayName(),
        timeControlType: 'ChicagoPoint', // All lobby games now use ChicagoPoint time control
        isRated: canBeRated ? isRated : false, // Only rated if authenticated and not AI
        isCorrespondence: false,
        timePerMoveDays: 0,
        aiType: opponentType === 'AI' ? aiType : 'greedy', // Only relevant for AI matches
      }
      console.log('[CreateMatchModal] Invoking CreateMatch', config)
      // Always use the match system - single games are just matches with targetScore=1
      await hub?.createMatch(config)
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

          {/* AI Type - Only show for AI opponents */}
          {opponentType === 'AI' && (
            <div className="space-y-3">
              <Label>AI Difficulty</Label>
              <RadioGroup value={aiType} onValueChange={(value) => setAiType(value as 'greedy' | 'random' | 'gnubg_easy' | 'gnubg_medium' | 'gnubg_hard' | 'gnubg_expert')}>
                <div className="flex items-center space-x-2">
                  <RadioGroupItem value="greedy" id="ai-greedy" />
                  <Label htmlFor="ai-greedy" className="font-normal cursor-pointer">
                    <div className="flex flex-col">
                      <span>Greedy Bot</span>
                      <span className="text-sm text-muted-foreground">Strategic play: prioritizes hitting and bearing off</span>
                    </div>
                  </Label>
                </div>
                <div className="flex items-center space-x-2">
                  <RadioGroupItem value="random" id="ai-random" />
                  <Label htmlFor="ai-random" className="font-normal cursor-pointer">
                    <div className="flex flex-col">
                      <span>Random Bot</span>
                      <span className="text-sm text-muted-foreground">Makes random valid moves - good for practice</span>
                    </div>
                  </Label>
                </div>
                <div className="border-t pt-3 mt-2">
                  <span className="text-sm font-medium text-muted-foreground">GNU Backgammon (Neural Network AI)</span>
                </div>
                <div className="flex items-center space-x-2">
                  <RadioGroupItem value="gnubg_easy" id="ai-gnubg-easy" />
                  <Label htmlFor="ai-gnubg-easy" className="font-normal cursor-pointer">
                    <div className="flex flex-col">
                      <span>Easy (GNUBG)</span>
                      <span className="text-sm text-muted-foreground">0-ply analysis (~1200 ELO) - instant moves, makes mistakes</span>
                    </div>
                  </Label>
                </div>
                <div className="flex items-center space-x-2">
                  <RadioGroupItem value="gnubg_medium" id="ai-gnubg-medium" />
                  <Label htmlFor="ai-gnubg-medium" className="font-normal cursor-pointer">
                    <div className="flex flex-col">
                      <span>Medium (GNUBG)</span>
                      <span className="text-sm text-muted-foreground">1-ply analysis (~1600 ELO) - fast, decent play</span>
                    </div>
                  </Label>
                </div>
                <div className="flex items-center space-x-2">
                  <RadioGroupItem value="gnubg_hard" id="ai-gnubg-hard" />
                  <Label htmlFor="ai-gnubg-hard" className="font-normal cursor-pointer">
                    <div className="flex flex-col">
                      <span>Hard (GNUBG) (Recommended)</span>
                      <span className="text-sm text-muted-foreground">2-ply analysis (~1900 ELO) - strong play</span>
                    </div>
                  </Label>
                </div>
                <div className="flex items-center space-x-2">
                  <RadioGroupItem value="gnubg_expert" id="ai-gnubg-expert" />
                  <Label htmlFor="ai-gnubg-expert" className="font-normal cursor-pointer">
                    <div className="flex flex-col">
                      <span>Expert (GNUBG)</span>
                      <span className="text-sm text-muted-foreground">3-ply analysis (~2100 ELO) - world-class play, slower</span>
                    </div>
                  </Label>
                </div>
              </RadioGroup>
            </div>
          )}

          {/* Time Control - Always ChicagoPoint */}
          <div className="p-3 bg-blue-50 border border-blue-200 rounded-md">
            <div className="space-y-1">
              <p className="text-sm font-medium text-blue-900">Time Control: Chicago Point</p>
              <p className="text-sm text-blue-800">
                12-second delay + {2 * targetScore}-minute reserve time
              </p>
              <p className="text-xs text-blue-700">
                Reserve time adjusts as match progresses: 2min per point remaining
              </p>
            </div>
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
