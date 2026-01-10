import React, { useState } from 'react'
import { useSignalR } from '@/contexts/SignalRContext'
import { useGameStore } from '@/stores/gameStore'
import { HubMethods } from '@/types/signalr.types'
import { Button } from '@/components/ui/button'
import { AbandonConfirmModal } from '@/components/modals/AbandonConfirmModal'
import { ChatPanel } from '@/components/game/ChatPanel'
import { GameState } from '@/types/game.types'
import { Dice1, RefreshCw, Flag, MessageCircle } from 'lucide-react'

interface GameControlsProps {
  gameState: GameState | null
  isSpectator?: boolean
}

export const GameControls: React.FC<GameControlsProps> = ({
  gameState,
  isSpectator = false,
}) => {
  const { invoke } = useSignalR()
  const { toggleBoardFlip, toggleChat, showChat, chatMessages } = useGameStore()
  const [showAbandonModal, setShowAbandonModal] = useState(false)

  if (!gameState) return null

  const hasDiceRolled =
    gameState.dice && gameState.dice.length > 0 && gameState.dice.some((d) => d > 0)
  // Use server-provided canDouble (checks: IsFull, !IsOpeningRoll, !IsCrawford, IsYourTurn, CubeOwnership)
  // Also ensure dice haven't been rolled yet (can only double before rolling)
  const canDouble = gameState.canDouble && !hasDiceRolled

  const handleDouble = async () => {
    try {
      await invoke(HubMethods.OfferDouble)
    } catch (error) {
      console.error('Failed to offer double:', error)
    }
  }

  const hasUnreadMessages = !showChat && chatMessages.length > 0

  // Check if this is an AI game or analysis mode (no chat needed)
  const isAiGame = gameState.whitePlayerId?.startsWith('ai_') || gameState.redPlayerId?.startsWith('ai_')
  const showChatButton = !gameState.isAnalysisMode && !isAiGame

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
        <ChatPanel />
      </div>
    )
  }

  return (
    <div className="space-y-2">
      {/* Doubling Cube Button */}
      {canDouble && (
        <Button
          variant="outline"
          size="lg"
          onClick={handleDouble}
          className="w-full h-16 bg-yellow-500/10 hover:bg-yellow-500/20"
        >
          <div className="text-center">
            <Dice1 className="h-6 w-6 mx-auto mb-1" />
            <div className="text-sm">Double</div>
          </div>
        </Button>
      )}

      {/* Utility Buttons */}
      <div className={`grid gap-2 ${showChatButton ? 'grid-cols-3' : gameState.isAnalysisMode ? 'grid-cols-1' : 'grid-cols-2'}`}>
        <Button
          variant="outline"
          size="lg"
          onClick={toggleBoardFlip}
          className="w-full h-12"
        >
          <div className="text-center">
            <RefreshCw className="h-5 w-5 mx-auto mb-1" />
            <div className="text-xs">Flip{showChatButton ? '' : ' Board'}</div>
          </div>
        </Button>

        {showChatButton && (
          <Button
            variant="outline"
            size="lg"
            onClick={toggleChat}
            className={`w-full h-12 relative ${hasUnreadMessages ? 'bg-primary/10' : ''}`}
          >
            <div className="text-center">
              <MessageCircle className="h-5 w-5 mx-auto mb-1" />
              <div className="text-xs">Chat</div>
            </div>
            {hasUnreadMessages && (
              <div className="absolute top-1 right-1 h-2 w-2 bg-primary rounded-full" />
            )}
          </Button>
        )}

        {!gameState.isAnalysisMode && (
          <Button
            variant="destructive"
            size="lg"
            onClick={() => setShowAbandonModal(true)}
            className="w-full h-12"
          >
            <div className="text-center">
              <Flag className="h-5 w-5 mx-auto mb-1" />
              <div className="text-xs">Abandon</div>
            </div>
          </Button>
        )}
      </div>

      {/* Chat Panel - only show in multiplayer games */}
      {showChatButton && <ChatPanel />}

      <AbandonConfirmModal
        isOpen={showAbandonModal}
        onClose={() => setShowAbandonModal(false)}
      />
    </div>
  )
}
