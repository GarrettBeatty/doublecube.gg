import React, { useState } from 'react'
import { type GameState, GameStatus } from '@/types/generated/Backgammon.Server.Models'
import { useGameStore } from '@/stores/gameStore'
import { Button } from '@/components/ui/button'
import { ForfeitConfirmModal } from '@/components/modals/ForfeitConfirmModal'
import { ChatPanel } from '@/components/game/ChatPanel'
import { RefreshCw, Flag, MessageCircle } from 'lucide-react'

interface GameControlsProps {
  gameState: GameState | null
  isSpectator?: boolean
}

export const GameControls: React.FC<GameControlsProps> = ({
  gameState,
  isSpectator = false,
}) => {
  const { toggleBoardFlip, toggleChat, showChat, chatMessages } = useGameStore()
  const [showForfeitModal, setShowForfeitModal] = useState(false)

  if (!gameState) return null

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

  // Determine if forfeit button should show
  const isGameCompleted = gameState.status === GameStatus.Completed
  const showForfeitButton = !gameState.isAnalysisMode && !isGameCompleted

  // Calculate grid columns based on visible buttons
  const getGridCols = () => {
    const buttonCount = 1 + (showChatButton ? 1 : 0) + (showForfeitButton ? 1 : 0)
    if (buttonCount === 1) return 'grid-cols-1'
    if (buttonCount === 2) return 'grid-cols-2'
    return 'grid-cols-3'
  }

  return (
    <div className="space-y-2">
      {/* Utility Buttons */}
      <div className={`grid gap-2 ${getGridCols()}`}>
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

        {showForfeitButton && (
          <Button
            variant="destructive"
            size="lg"
            onClick={() => setShowForfeitModal(true)}
            className="w-full h-12"
          >
            <div className="text-center">
              <Flag className="h-5 w-5 mx-auto mb-1" />
              <div className="text-xs">Forfeit</div>
            </div>
          </Button>
        )}
      </div>

      {/* Chat Panel - only show in multiplayer games */}
      {showChatButton && <ChatPanel />}

      <ForfeitConfirmModal
        isOpen={showForfeitModal}
        onClose={() => setShowForfeitModal(false)}
      />
    </div>
  )
}
