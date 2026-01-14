import React from 'react'
import { useSignalR } from '@/contexts/SignalRContext'
import type { GameState } from '@/types/generated/Backgammon.Server.Models'
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
  gameState: GameState | null
}

export const AbandonConfirmModal: React.FC<AbandonConfirmModalProps> = ({
  isOpen,
  onClose,
  gameState,
}) => {
  const { hub } = useSignalR()

  const handleConfirm = async () => {
    try {
      await hub?.abandonGame()
      onClose()
    } catch (error) {
      console.error('Failed to abandon game:', error)
    }
  }

  // Determine if this is an abandon or forfeit
  const isAbandon = gameState?.leaveGameAction === 'Abandon'
  const actionText = isAbandon ? 'Abandon' : 'Forfeit'
  const title = isAbandon ? 'Abandon Game?' : 'Forfeit Game?'
  const description = isAbandon
    ? 'The game has not started yet. No points will be awarded.'
    : 'Are you sure you want to forfeit this game in progress?'
  const warningText = isAbandon
    ? 'This action cannot be undone. The game will be cancelled and recorded with no points awarded to either player.'
    : 'This action cannot be undone. You will lose the game and your opponent will be awarded points based on the current board position (Normal, Gammon, or Backgammon).'

  return (
    <Dialog open={isOpen} onOpenChange={onClose}>
      <DialogContent>
        <DialogHeader>
          <DialogTitle>{title}</DialogTitle>
          <DialogDescription>
            {description}
          </DialogDescription>
        </DialogHeader>

        <Alert variant="destructive">
          <AlertDescription>
            {warningText}
          </AlertDescription>
        </Alert>

        <DialogFooter className="gap-2">
          <Button variant="outline" onClick={onClose}>
            Cancel
          </Button>
          <Button variant="destructive" onClick={handleConfirm}>
            {actionText} Game
          </Button>
        </DialogFooter>
      </DialogContent>
    </Dialog>
  )
}
