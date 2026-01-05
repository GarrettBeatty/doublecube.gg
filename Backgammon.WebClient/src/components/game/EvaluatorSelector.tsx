import { Card, CardContent, CardHeader, CardTitle } from '@/components/ui/card'
import {
  Select,
  SelectContent,
  SelectItem,
  SelectTrigger,
  SelectValue,
} from '@/components/ui/select'
import { Brain, Zap } from 'lucide-react'

interface EvaluatorSelectorProps {
  value: 'Heuristic' | 'Gnubg'
  onChange: (value: 'Heuristic' | 'Gnubg') => void
  disabled?: boolean
}

export const EvaluatorSelector: React.FC<EvaluatorSelectorProps> = ({
  value,
  onChange,
  disabled = false,
}) => {
  return (
    <Card>
      <CardHeader className="pb-3">
        <CardTitle className="text-sm font-medium">Analysis Engine</CardTitle>
      </CardHeader>
      <CardContent>
        <Select
          value={value}
          onValueChange={(val) => onChange(val as 'Heuristic' | 'Gnubg')}
          disabled={disabled}
        >
          <SelectTrigger className="w-full">
            <SelectValue />
          </SelectTrigger>
          <SelectContent>
            <SelectItem value="Heuristic">
              <div className="flex items-center gap-2">
                <Zap className="h-4 w-4 text-yellow-500" />
                <div>
                  <div className="font-medium">Fast Analysis</div>
                  <div className="text-xs text-muted-foreground">
                    Instant • Basic heuristic
                  </div>
                </div>
              </div>
            </SelectItem>
            <SelectItem value="Gnubg">
              <div className="flex items-center gap-2">
                <Brain className="h-4 w-4 text-purple-500" />
                <div>
                  <div className="font-medium">Deep Analysis</div>
                  <div className="text-xs text-muted-foreground">
                    1-3s • GNU Backgammon AI
                  </div>
                </div>
              </div>
            </SelectItem>
          </SelectContent>
        </Select>
      </CardContent>
    </Card>
  )
}
