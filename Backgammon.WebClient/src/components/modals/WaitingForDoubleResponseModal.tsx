import {
  Dialog,
  DialogContent,
  DialogDescription,
  DialogHeader,
  DialogTitle,
} from '@/components/ui/dialog'
import { Badge } from '@/components/ui/badge'
import { Dice6, Loader2 } from 'lucide-react'

interface WaitingForDoubleResponseModalProps {
  isOpen: boolean
  currentValue: number
  newValue: number
}

export function WaitingForDoubleResponseModal({
  isOpen,
  currentValue,
  newValue,
}: WaitingForDoubleResponseModalProps) {
  return (
    <Dialog open={isOpen}>
      <DialogContent className="sm:max-w-md" hideCloseButton>
        <DialogHeader>
          <DialogTitle className="flex items-center gap-2">
            <Dice6 className="h-6 w-6 text-yellow-500" />
            Double Offered
          </DialogTitle>
          <DialogDescription>
            Waiting for your opponent to respond to your double offer...
          </DialogDescription>
        </DialogHeader>

        <div className="py-4 space-y-4">
          <div className="flex items-center justify-center gap-4">
            <div className="text-center">
              <div className="text-sm text-muted-foreground mb-2">Current Stakes</div>
              <Badge variant="secondary" className="text-lg px-4 py-2">
                {currentValue}x
              </Badge>
            </div>

            <div className="text-2xl text-muted-foreground">→</div>

            <div className="text-center">
              <div className="text-sm text-muted-foreground mb-2">Proposed Stakes</div>
              <Badge className="text-lg px-4 py-2 bg-yellow-500 hover:bg-yellow-600">
                {newValue}x
              </Badge>
            </div>
          </div>

          <div className="flex items-center justify-center gap-2 text-muted-foreground">
            <Loader2 className="h-4 w-4 animate-spin" />
            <span>Awaiting opponent response...</span>
          </div>

          <div className="bg-blue-50 border border-blue-200 rounded-lg p-3">
            <p className="text-sm text-gray-700">
              Your opponent can either{' '}
              <span className="font-semibold text-green-600">accept</span> the double (and take
              ownership of the cube) or <span className="font-semibold text-red-600">decline</span>{' '}
              (and you win at the current stake of {currentValue}x).
            </p>
          </div>
        </div>
      </DialogContent>
    </Dialog>
  )
}
