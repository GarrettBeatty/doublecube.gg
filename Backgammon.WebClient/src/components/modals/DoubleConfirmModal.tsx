import {
  Dialog,
  DialogContent,
  DialogDescription,
  DialogFooter,
  DialogHeader,
  DialogTitle,
} from '@/components/ui/dialog'
import { Button } from '@/components/ui/button'
import { Badge } from '@/components/ui/badge'
import { Dice6 } from 'lucide-react'

interface DoubleConfirmModalProps {
  isOpen: boolean
  onClose: () => void
  onConfirm: () => void
  currentValue: number
  newValue: number
}

export function DoubleConfirmModal({
  isOpen,
  onClose,
  onConfirm,
  currentValue,
  newValue,
}: DoubleConfirmModalProps) {
  const handleConfirm = () => {
    onConfirm()
    onClose()
  }

  return (
    <Dialog open={isOpen} onOpenChange={onClose}>
      <DialogContent className="sm:max-w-md">
        <DialogHeader>
          <DialogTitle className="flex items-center gap-2">
            <Dice6 className="h-6 w-6 text-yellow-500" />
            Offer Double?
          </DialogTitle>
          <DialogDescription>
            Do you want to offer to double the stakes to your opponent?
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
              <div className="text-sm text-muted-foreground mb-2">New Stakes</div>
              <Badge className="text-lg px-4 py-2 bg-yellow-500 hover:bg-yellow-600">
                {newValue}x
              </Badge>
            </div>
          </div>

          <div className="bg-blue-50 border border-blue-200 rounded-lg p-3">
            <p className="text-sm text-gray-700">
              <span className="font-semibold">Note:</span> Your opponent can either{' '}
              <span className="font-semibold text-green-600">accept</span> (taking ownership
              of the cube) or <span className="font-semibold text-red-600">decline</span> (you
              win at the current stake of {currentValue}x).
            </p>
          </div>
        </div>

        <DialogFooter className="gap-2 sm:gap-0">
          <Button variant="outline" onClick={onClose}>
            Cancel
          </Button>
          <Button onClick={handleConfirm} className="bg-yellow-500 hover:bg-yellow-600">
            Offer Double
          </Button>
        </DialogFooter>
      </DialogContent>
    </Dialog>
  )
}
