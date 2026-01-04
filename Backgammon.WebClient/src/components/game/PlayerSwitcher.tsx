import { CheckerColor } from '@/types/game.types'
import { Button } from '@/components/ui/button'
import { Card, CardContent, CardHeader, CardTitle } from '@/components/ui/card'
import { useSignalR } from '@/contexts/SignalRContext'
import { HubMethods } from '@/types/signalr.types'
import { Users } from 'lucide-react'

interface PlayerSwitcherProps {
  currentPlayer: CheckerColor
}

export const PlayerSwitcher: React.FC<PlayerSwitcherProps> = ({ currentPlayer }) => {
  const { invoke } = useSignalR()

  const handleSwitch = async (color: CheckerColor) => {
    if (color === currentPlayer) return
    try {
      await invoke(HubMethods.SetCurrentPlayer, color)
    } catch (error) {
      console.error('Failed to switch player:', error)
    }
  }

  return (
    <Card>
      <CardHeader className="pb-3">
        <CardTitle className="text-sm font-medium flex items-center gap-2">
          <Users className="h-4 w-4" />
          Current Player
        </CardTitle>
      </CardHeader>
      <CardContent>
        <div className="grid grid-cols-2 gap-2">
          <Button
            variant={currentPlayer === CheckerColor.White ? 'default' : 'outline'}
            onClick={() => handleSwitch(CheckerColor.White)}
            size="sm"
          >
            White
          </Button>
          <Button
            variant={currentPlayer === CheckerColor.Red ? 'default' : 'outline'}
            onClick={() => handleSwitch(CheckerColor.Red)}
            size="sm"
          >
            Red
          </Button>
        </div>
      </CardContent>
    </Card>
  )
}
