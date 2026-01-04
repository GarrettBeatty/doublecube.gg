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
import { Alert, AlertDescription } from '@/components/ui/alert'

interface AbandonConfirmModalProps {
  isOpen: boolean
  onClose: () => void
}

export const AbandonConfirmModal: React.FC<AbandonConfirmModalProps> = ({
  isOpen,
  onClose,
}) => {
  const { invoke } = useSignalR()

  const handleConfirm = async () => {
    try {
      await invoke(HubMethods.AbandonGame)
      onClose()
    } catch (error) {
      console.error('Failed to abandon game:', error)
    }
  }

  return (
    <Dialog open={isOpen} onOpenChange={onClose}>
      <DialogContent>
        <DialogHeader>
          <DialogTitle>Abandon Game?</DialogTitle>
          <DialogDescription>
            Are you sure you want to forfeit this game?
          </DialogDescription>
        </DialogHeader>

        <Alert variant="destructive">
          <AlertDescription>
            This action cannot be undone. You will lose the game and your opponent will be declared the winner.
          </AlertDescription>
        </Alert>

        <DialogFooter className="gap-2">
          <Button variant="outline" onClick={onClose}>
            Cancel
          </Button>
          <Button variant="destructive" onClick={handleConfirm}>
            Abandon Game
          </Button>
        </DialogFooter>
      </DialogContent>
    </Dialog>
  )
}
