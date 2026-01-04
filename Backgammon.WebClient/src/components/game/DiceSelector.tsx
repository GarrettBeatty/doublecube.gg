import { useState } from 'react'
import { Card, CardContent, CardHeader, CardTitle } from '@/components/ui/card'
import { Button } from '@/components/ui/button'
import { Select, SelectContent, SelectItem, SelectTrigger, SelectValue } from '@/components/ui/select'
import { Dices } from 'lucide-react'
import { useSignalR } from '@/contexts/SignalRContext'
import { HubMethods } from '@/types/signalr.types'
import { useToast } from '@/hooks/use-toast'
import { useGameStore } from '@/stores/gameStore'

export const DiceSelector: React.FC = () => {
  const { invoke } = useSignalR()
  const { toast } = useToast()
  const { currentGameState } = useGameStore()
  const [die1, setDie1] = useState<string>('1')
  const [die2, setDie2] = useState<string>('1')

  // Check if dice are set and if moves have been made
  const hasDiceSet = currentGameState?.dice && currentGameState.dice.length > 0 && currentGameState.dice.some((d) => d > 0)
  const remainingMoves = currentGameState?.remainingMoves?.length ?? 0
  const totalMoves = hasDiceSet ? currentGameState!.dice.length : 0
  
  // Disable if:
  // 1. Dice are set AND
  // 2. Not all moves are remaining (some moves made OR all moves used but turn not ended yet)
  const isDisabled = hasDiceSet && remainingMoves !== totalMoves

  const handleSetDice = async () => {
    try {
      await invoke(HubMethods.SetDice, parseInt(die1), parseInt(die2))
    } catch (error) {
      console.error('Failed to set dice:', error)
      toast({
        title: 'Error',
        description: 'Failed to set dice values',
        variant: 'destructive',
      })
    }
  }

  const diceOptions = ['1', '2', '3', '4', '5', '6']

  return (
    <Card>
      <CardHeader className="pb-3">
        <CardTitle className="text-sm font-medium flex items-center gap-2">
          <Dices className="h-4 w-4" />
          Set Dice
        </CardTitle>
      </CardHeader>
      <CardContent className="space-y-3">
        <div className="flex gap-2">
          <div className="flex-1">
            <label className="text-xs text-muted-foreground mb-1 block">Die 1</label>
            <Select value={die1} onValueChange={setDie1} disabled={isDisabled}>
              <SelectTrigger>
                <SelectValue />
              </SelectTrigger>
              <SelectContent>
                {diceOptions.map((val) => (
                  <SelectItem key={val} value={val}>
                    {val}
                  </SelectItem>
                ))}
              </SelectContent>
            </Select>
          </div>
          <div className="flex-1">
            <label className="text-xs text-muted-foreground mb-1 block">Die 2</label>
            <Select value={die2} onValueChange={setDie2} disabled={isDisabled}>
              <SelectTrigger>
                <SelectValue />
              </SelectTrigger>
              <SelectContent>
                {diceOptions.map((val) => (
                  <SelectItem key={val} value={val}>
                    {val}
                  </SelectItem>
                ))}
              </SelectContent>
            </Select>
          </div>
        </div>
        <Button onClick={handleSetDice} className="w-full" size="sm" disabled={isDisabled}>
          {isDisabled ? 'Undo or End Turn First' : 'Apply'}
        </Button>
      </CardContent>
    </Card>
  )
}
