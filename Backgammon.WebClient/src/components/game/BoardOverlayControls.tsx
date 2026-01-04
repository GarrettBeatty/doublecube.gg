import React from 'react'
import { GameState, GameStatus, CheckerColor } from '@/types/game.types'
import { Button } from '@/components/ui/button'
import { Dice1, Check, Undo } from 'lucide-react'
import { useSignalR } from '@/contexts/SignalRContext'
import { HubMethods } from '@/types/signalr.types'

interface BoardOverlayControlsProps {
  gameState: GameState
  isSpectator?: boolean
  isAnalysisMode?: boolean
}

export const BoardOverlayControls: React.FC<BoardOverlayControlsProps> = ({
  gameState,
  isSpectator = false,
  isAnalysisMode = false,
}) => {
  const { invoke } = useSignalR()

  if (isSpectator) return null

  const isGameInProgress = gameState.status === GameStatus.InProgress
  const isYourTurn = gameState.isYourTurn
  const hasDiceRolled =
    gameState.dice && gameState.dice.length > 0 && gameState.dice.some((d) => d > 0)

  // Opening roll logic
  const isOpeningRoll = gameState.isOpeningRoll || false
  const yourColor = gameState.yourColor
  const youHaveRolled = isOpeningRoll && (
    (yourColor === CheckerColor.White && gameState.whiteOpeningRoll != null) ||
    (yourColor === CheckerColor.Red && gameState.redOpeningRoll != null)
  )
  const opponentHasRolled = isOpeningRoll && (
    (yourColor === CheckerColor.White && gameState.redOpeningRoll != null) ||
    (yourColor === CheckerColor.Red && gameState.whiteOpeningRoll != null)
  )

  // In analysis mode, don't show roll button (user sets dice manually)
  const canRoll = !isAnalysisMode && isGameInProgress && (isOpeningRoll ? (!youHaveRolled || gameState.isOpeningRollTie) : (isYourTurn && !hasDiceRolled))

  // End turn button logic:
  // Analysis mode: Show when all moves used OR no valid moves remain
  // Regular mode: Show when no valid moves remain AND it's your turn
  const hasUsedAllMoves = gameState.remainingMoves && gameState.remainingMoves.length === 0
  const canEndTurn = isGameInProgress && !isOpeningRoll && hasDiceRolled && (
    isAnalysisMode ? (hasUsedAllMoves || !gameState.hasValidMoves) : (!gameState.hasValidMoves && isYourTurn)
  )
  
  // Undo button: Only show if at least one move has been made
  const totalMoves = hasDiceRolled ? gameState.dice.length : 0
  const remainingMovesCount = gameState.remainingMoves?.length ?? 0
  const movesMade = totalMoves > 0 && remainingMovesCount < totalMoves
  const canUndo = isGameInProgress && !isOpeningRoll && hasDiceRolled && movesMade

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

  return (
    <>
      {/* Roll Dice Button - Center of right side of board */}
      {canRoll && (
        <div
          className="absolute top-1/2 -translate-y-1/2 -translate-x-1/2 z-10"
          style={{ left: '72.65%' }}
        >
          <Button
            variant="default"
            size="lg"
            onClick={handleRollDice}
            className="h-20 w-20 rounded-full shadow-lg"
          >
            <div className="text-center">
              <Dice1 className="h-8 w-8 mx-auto mb-1" />
              <div className="text-xs font-semibold">Roll</div>
            </div>
          </Button>
        </div>
      )}

      {/* Opening Roll Display - Show both player's rolls */}
      {isOpeningRoll && (youHaveRolled || opponentHasRolled) && (
        <div
          className="absolute top-1/2 -translate-x-1/2 -translate-y-1/2 z-10"
          style={{ left: '46.5%' }}
        >
          <div className="flex flex-col gap-2 items-center">
            {/* White's roll (top) */}
            <div className="flex flex-col items-center gap-1">
              <div className="text-white text-[10px] font-semibold">{gameState.whitePlayerName}</div>
              {gameState.whiteOpeningRoll != null ? (
                <div className="w-9 h-9 bg-white rounded-md flex items-center justify-center text-lg font-bold text-gray-800 shadow-xl">
                  {gameState.whiteOpeningRoll}
                </div>
              ) : (
                <div className="w-9 h-9 bg-gray-600 rounded-md flex items-center justify-center text-xs text-gray-400">
                  ?
                </div>
              )}
            </div>

            {/* Red's roll (bottom) */}
            <div className="flex flex-col items-center gap-1">
              <div className="text-white text-[10px] font-semibold">{gameState.redPlayerName}</div>
              {gameState.redOpeningRoll != null ? (
                <div className="w-9 h-9 bg-white rounded-md flex items-center justify-center text-lg font-bold text-gray-800 shadow-xl">
                  {gameState.redOpeningRoll}
                </div>
              ) : (
                <div className="w-9 h-9 bg-gray-600 rounded-md flex items-center justify-center text-xs text-gray-400">
                  ?
                </div>
              )}
            </div>

            {/* Tie message */}
            {gameState.isOpeningRollTie && (
              <div className="mt-1 text-yellow-400 text-xs font-bold animate-pulse">
                Tie - Roll Again!
              </div>
            )}
          </div>
        </div>
      )}

      {/* Regular Dice Display - Now rendered in SVG */}
      {/* Dice are now rendered directly in BoardSVG for accurate positioning */}

      {/* Undo Button - Center of left side of board */}
      {canUndo && (
        <div
          className="absolute top-1/2 -translate-y-1/2 -translate-x-1/2 z-10"
          style={{ left: '22.16%' }}
        >
          <Button
            variant="outline"
            size="lg"
            onClick={handleUndo}
            className="h-16 w-16 rounded-full shadow-lg bg-background/95 backdrop-blur-sm"
          >
            <div className="text-center">
              <Undo className="h-6 w-6 mx-auto" />
            </div>
          </Button>
        </div>
      )}

      {/* End Turn Button - Center of right side of board */}
      {canEndTurn && (
        <div
          className="absolute top-1/2 -translate-y-1/2 -translate-x-1/2 z-10"
          style={{ left: '72.65%' }}
        >
          <Button
            variant="default"
            size="lg"
            onClick={handleEndTurn}
            className="h-20 w-20 rounded-full shadow-lg bg-green-600 hover:bg-green-700"
          >
            <div className="text-center">
              <Check className="h-8 w-8 mx-auto mb-1" />
              <div className="text-xs font-semibold">End</div>
            </div>
          </Button>
        </div>
      )}
    </>
  )
}
