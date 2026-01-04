import React, { useState } from 'react'
import { useSignalR } from '@/contexts/SignalRContext'
import { useGameStore } from '@/stores/gameStore'
import { HubMethods } from '@/types/signalr.types'
import { Button } from '@/components/ui/button'
import { AbandonConfirmModal } from '@/components/modals/AbandonConfirmModal'
import { GameState } from '@/types/game.types'
import { Dice1, Check, Undo, RefreshCw, Flag } from 'lucide-react'

interface GameControlsProps {
  gameState: GameState | null
  isSpectator?: boolean
}

export const GameControls: React.FC<GameControlsProps> = ({
  gameState,
  isSpectator = false,
}) => {
  const { invoke } = useSignalR()
  const { toggleBoardFlip } = useGameStore()
  const [showAbandonModal, setShowAbandonModal] = useState(false)

  if (!gameState) return null

  const isYourTurn = gameState.isYourTurn
  // Only consider dice rolled if there are dice with non-zero values
  const hasDiceRolled =
    gameState.dice && gameState.dice.length > 0 && gameState.dice.some((d) => d > 0)
  const hasMovesLeft = gameState.remainingMoves && gameState.remainingMoves.length > 0
  const canRoll = isYourTurn && !hasDiceRolled
  const canEndTurn = isYourTurn && hasDiceRolled && !hasMovesLeft
  const canDouble = isYourTurn && !hasDiceRolled && gameState.doublingCubeOwner === gameState.yourColor

  const handleRollDice = async () => {
    try {
      await invoke(HubMethods.RollDice)
    } catch (error) {
      console.error('Failed to roll dice:', error)
    }
  }

  const handleEndTurn = async () => {
    try {
      await invoke(HubMethods.EndTurn)
    } catch (error) {
      console.error('Failed to end turn:', error)
    }
  }

  const handleUndo = async () => {
    try {
      await invoke(HubMethods.UndoLastMove)
    } catch (error) {
      console.error('Failed to undo:', error)
    }
  }

  const handleDouble = async () => {
    try {
      await invoke(HubMethods.OfferDouble)
    } catch (error) {
      console.error('Failed to offer double:', error)
    }
  }

  if (isSpectator) {
    return (
      <div className="space-y-2">
        <div className="text-center text-muted-foreground text-sm mb-4">
          Spectating - controls disabled
        </div>
        <Button
          variant="outline"
          size="sm"
          onClick={toggleBoardFlip}
          className="w-full"
        >
          <RefreshCw className="h-4 w-4" />
        </Button>
      </div>
    )
  }

  return (
    <div className="space-y-2">
      <div className="grid grid-cols-2 gap-2">
        <Button
          variant="default"
          size="lg"
          onClick={handleRollDice}
          disabled={!canRoll}
          className="h-16"
        >
          <div className="text-center">
            <Dice1 className="h-6 w-6 mx-auto mb-1" />
            <div className="text-sm">Roll Dice</div>
          </div>
        </Button>

        <Button
          variant="default"
          size="lg"
          onClick={handleEndTurn}
          disabled={!canEndTurn}
          className="h-16"
        >
          <div className="text-center">
            <Check className="h-6 w-6 mx-auto mb-1" />
            <div className="text-sm">End Turn</div>
          </div>
        </Button>
      </div>

      <div className="grid grid-cols-2 gap-2">
        <Button
          variant="outline"
          size="lg"
          onClick={handleUndo}
          disabled={!isYourTurn || !hasDiceRolled}
          className="h-16"
        >
          <div className="text-center">
            <Undo className="h-6 w-6 mx-auto mb-1" />
            <div className="text-sm">Undo</div>
          </div>
        </Button>

        <Button
          variant="outline"
          size="lg"
          onClick={handleDouble}
          disabled={!canDouble}
          className="h-16 bg-yellow-500/10 hover:bg-yellow-500/20"
        >
          <div className="text-center">
            <Dice1 className="h-6 w-6 mx-auto mb-1" />
            <div className="text-sm">Double</div>
          </div>
        </Button>
      </div>

      <div className="grid grid-cols-2 gap-2">
        <Button
          variant="outline"
          size="sm"
          onClick={toggleBoardFlip}
          className="w-full"
        >
          <RefreshCw className="h-4 w-4" />
        </Button>

        <Button
          variant="destructive"
          size="sm"
          onClick={() => setShowAbandonModal(true)}
          className="w-full"
        >
          <Flag className="h-4 w-4" />
        </Button>
      </div>

      <AbandonConfirmModal
        isOpen={showAbandonModal}
        onClose={() => setShowAbandonModal(false)}
      />
    </div>
  )
}
