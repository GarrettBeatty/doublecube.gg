import { Card, CardContent, CardHeader, CardTitle } from '@/components/ui/card'
import { Label } from '@/components/ui/label'
import { Switch } from '@/components/ui/switch'
import { useGameStore } from '@/stores/gameStore'
import { Settings2 } from 'lucide-react'

export const AnalysisModeToggles: React.FC = () => {
  const { isFreeMoveEnabled, isCustomDiceEnabled, setFreeMoveEnabled, setCustomDiceEnabled } =
    useGameStore()

  return (
    <Card>
      <CardHeader className="pb-3">
        <CardTitle className="text-sm font-medium flex items-center gap-2">
          <Settings2 className="h-4 w-4" />
          Analysis Options
        </CardTitle>
      </CardHeader>
      <CardContent className="space-y-4">
        <div className="flex items-center justify-between space-x-2">
          <Label htmlFor="free-move" className="flex flex-col gap-1 cursor-pointer">
            <span className="font-medium">Free Movement</span>
            <span className="font-normal text-xs text-muted-foreground">
              Ignore game rules when moving pieces
            </span>
          </Label>
          <Switch
            id="free-move"
            checked={isFreeMoveEnabled}
            onCheckedChange={setFreeMoveEnabled}
          />
        </div>

        <div className="flex items-center justify-between space-x-2">
          <Label htmlFor="custom-dice" className="flex flex-col gap-1 cursor-pointer">
            <span className="font-medium">Custom Dice</span>
            <span className="font-normal text-xs text-muted-foreground">
              Manually set dice values instead of rolling
            </span>
          </Label>
          <Switch
            id="custom-dice"
            checked={isCustomDiceEnabled}
            onCheckedChange={setCustomDiceEnabled}
          />
        </div>
      </CardContent>
    </Card>
  )
}
