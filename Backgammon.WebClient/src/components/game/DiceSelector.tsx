import { useState } from 'react'
import { Card, CardContent, CardHeader, CardTitle } from '@/components/ui/card'
import { Button } from '@/components/ui/button'
import { Select, SelectContent, SelectItem, SelectTrigger, SelectValue } from '@/components/ui/select'
import { Dices } from 'lucide-react'
import { useSignalR } from '@/contexts/SignalRContext'
import { useToast } from '@/hooks/use-toast'

export const DiceSelector: React.FC = () => {
  const { hub } = useSignalR()
  const { toast } = useToast()
  const [die1, setDie1] = useState<string>('1')
  const [die2, setDie2] = useState<string>('1')

  const handleSetDice = async () => {
    try {
      await hub?.setDice(parseInt(die1), parseInt(die2))
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
            <Select value={die1} onValueChange={setDie1}>
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
            <Select value={die2} onValueChange={setDie2}>
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
        <Button onClick={handleSetDice} className="w-full" size="sm">
          Apply
        </Button>
      </CardContent>
    </Card>
  )
}
