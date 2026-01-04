import React from 'react'
import { useSignalR } from '@/contexts/SignalRContext'
import { HubMethods } from '@/types/signalr.types'
import {
  Dialog,
  DialogContent,
  DialogDescription,
  DialogHeader,
  DialogTitle,
  DialogFooter,
} from '@/components/ui/dialog'
import { Button } from '@/components/ui/button'
import { Badge } from '@/components/ui/badge'
import { Dice6 } from 'lucide-react'

interface DoubleOfferModalProps {
  isOpen: boolean
  onClose: () => void
  currentStakes: number
  newStakes: number
}

export const DoubleOfferModal: React.FC<DoubleOfferModalProps> = ({
  isOpen,
  onClose,
  currentStakes,
  newStakes,
}) => {
  const { invoke } = useSignalR()

  const handleAccept = async () => {
    try {
      await invoke(HubMethods.RespondToDouble, true)
      onClose()
    } catch (error) {
      console.error('Failed to accept double:', error)
    }
  }

  const handleDecline = async () => {
    try {
      await invoke(HubMethods.RespondToDouble, false)
      onClose()
    } catch (error) {
      console.error('Failed to decline double:', error)
    }
  }

  return (
    <Dialog open={isOpen} onOpenChange={onClose}>
      <DialogContent>
        <DialogHeader>
          <DialogTitle className="flex items-center gap-2">
            <Dice6 className="h-6 w-6" />
            Doubling Cube Offered!
          </DialogTitle>
          <DialogDescription>
            Your opponent has offered to double the stakes
          </DialogDescription>
        </DialogHeader>

        <div className="py-6 space-y-4">
          <div className="flex items-center justify-center gap-4">
            <div className="text-center">
              <div className="text-sm text-muted-foreground mb-2">Current Stakes</div>
              <Badge variant="secondary" className="text-lg px-4 py-2">
                {currentStakes}x
              </Badge>
            </div>

            <div className="text-2xl">→</div>

            <div className="text-center">
              <div className="text-sm text-muted-foreground mb-2">New Stakes</div>
              <Badge variant="default" className="text-lg px-4 py-2 bg-yellow-500">
                {newStakes}x
              </Badge>
            </div>
          </div>

          <div className="text-sm text-center text-muted-foreground">
            If you accept, the game will be worth <strong>{newStakes}x</strong> points.
            <br />
            If you decline, you forfeit the game.
          </div>
        </div>

        <DialogFooter className="gap-2">
          <Button variant="destructive" onClick={handleDecline}>
            Decline (Forfeit)
          </Button>
          <Button variant="default" onClick={handleAccept} className="bg-green-600 hover:bg-green-700">
            Accept Double
          </Button>
        </DialogFooter>
      </DialogContent>
    </Dialog>
  )
}
