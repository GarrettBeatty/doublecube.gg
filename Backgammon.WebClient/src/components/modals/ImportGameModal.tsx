import React, { useState } from 'react'
import {
  Dialog,
  DialogContent,
  DialogDescription,
  DialogHeader,
  DialogTitle,
  DialogFooter,
} from '@/components/ui/dialog'
import { Button } from '@/components/ui/button'
import { Textarea } from '@/components/ui/textarea'
import { Label } from '@/components/ui/label'

interface ImportGameModalProps {
  isOpen: boolean
  onClose: () => void
  onImport: (gameData: string) => void
}

export const ImportGameModal: React.FC<ImportGameModalProps> = ({
  isOpen,
  onClose,
  onImport,
}) => {
  const [gameData, setGameData] = useState('')

  const handleImport = () => {
    if (gameData.trim()) {
      onImport(gameData.trim())
      setGameData('')
      onClose()
    }
  }

  const handleClose = () => {
    setGameData('')
    onClose()
  }

  return (
    <Dialog open={isOpen} onOpenChange={handleClose}>
      <DialogContent className="sm:max-w-[500px]">
        <DialogHeader>
          <DialogTitle>Import a Game</DialogTitle>
          <DialogDescription>
            Paste your game data below to analyze it on the analysis board.
            Supported formats include SGF and match notation.
          </DialogDescription>
        </DialogHeader>

        <div className="space-y-4 py-4">
          <div className="space-y-2">
            <Label htmlFor="game-data">Game Data</Label>
            <Textarea
              id="game-data"
              value={gameData}
              onChange={(e) => setGameData(e.target.value)}
              placeholder="Paste your game data here..."
              className="min-h-[200px] font-mono text-sm"
              autoFocus
            />
          </div>
        </div>

        <DialogFooter>
          <Button variant="outline" onClick={handleClose}>
            Cancel
          </Button>
          <Button onClick={handleImport} disabled={!gameData.trim()}>
            Import & Analyze
          </Button>
        </DialogFooter>
      </DialogContent>
    </Dialog>
  )
}
