import React from 'react'
import { useSignalR } from '@/contexts/SignalRContext'
import {
  Dialog,
  DialogContent,
  DialogDescription,
  DialogHeader,
  DialogTitle,
  DialogFooter,
} from '@/components/ui/dialog'
import { Button } from '@/components/ui/button'
import { Alert, AlertDescription } from '@/components/ui/alert'

interface ForfeitConfirmModalProps {
  isOpen: boolean
  onClose: () => void
}

export const ForfeitConfirmModal: React.FC<ForfeitConfirmModalProps> = ({
  isOpen,
  onClose,
}) => {
  const { hub } = useSignalR()

  const handleConfirm = async () => {
    try {
      await hub?.abandonGame()
      onClose()
    } catch (error) {
      console.error('Failed to forfeit game:', error)
    }
  }

  return (
    <Dialog open={isOpen} onOpenChange={onClose}>
      <DialogContent>
        <DialogHeader>
          <DialogTitle>Forfeit Game?</DialogTitle>
          <DialogDescription>
            Are you sure you want to forfeit this game?
          </DialogDescription>
        </DialogHeader>

        <Alert variant="destructive">
          <AlertDescription>
            This action cannot be undone. You will lose the game and your opponent
            will be awarded points based on the current board position.
          </AlertDescription>
        </Alert>

        <DialogFooter className="gap-2">
          <Button variant="outline" onClick={onClose}>
            Cancel
          </Button>
          <Button variant="destructive" onClick={handleConfirm}>
            Forfeit Game
          </Button>
        </DialogFooter>
      </DialogContent>
    </Dialog>
  )
}
