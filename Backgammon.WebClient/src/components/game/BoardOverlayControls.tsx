import React from 'react'
import { GameState, GameStatus, CheckerColor } from '@/types/game.types'
import { Button } from '@/components/ui/button'
import { Dice1, Check, Undo } from 'lucide-react'
import { useSignalR } from '@/contexts/SignalRContext'
import { HubMethods } from '@/types/signalr.types'

interface BoardOverlayControlsProps {
  gameState: GameState
  isSpectator?: boolean
}

export const BoardOverlayControls: React.FC<BoardOverlayControlsProps> = ({
  gameState,
  isSpectator = false,
}) => {
  const { invoke } = useSignalR()

  if (isSpectator) return null

  const isGameInProgress = gameState.status === GameStatus.InProgress
  const isYourTurn = gameState.isYourTurn
  const hasDiceRolled =
    gameState.dice && gameState.dice.length > 0 && gameState.dice.some((d) => d > 0)
  const hasMovesLeft = gameState.remainingMoves && gameState.remainingMoves.length > 0

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

  const canRoll = isGameInProgress && (isOpeningRoll ? (!youHaveRolled || gameState.isOpeningRollTie) : (isYourTurn && !hasDiceRolled))
  const canEndTurn = isGameInProgress && !isOpeningRoll && isYourTurn && hasDiceRolled && !hasMovesLeft
  const canUndo = isGameInProgress && !isOpeningRoll && isYourTurn && hasDiceRolled

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
          style={{ left: '71%' }}
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
          style={{ left: '47%' }}
        >
          <div className="flex flex-col gap-3 items-center">
            {/* White's roll (top) */}
            <div className="flex flex-col items-center gap-1">
              <div className="text-white text-xs font-semibold">{gameState.whitePlayerName}</div>
              {gameState.whiteOpeningRoll != null ? (
                <div className="w-12 h-12 bg-white rounded-lg flex items-center justify-center text-2xl font-bold text-gray-800 shadow-xl">
                  {gameState.whiteOpeningRoll}
                </div>
              ) : (
                <div className="w-12 h-12 bg-gray-600 rounded-lg flex items-center justify-center text-sm text-gray-400">
                  ?
                </div>
              )}
            </div>

            {/* Red's roll (bottom) */}
            <div className="flex flex-col items-center gap-1">
              <div className="text-white text-xs font-semibold">{gameState.redPlayerName}</div>
              {gameState.redOpeningRoll != null ? (
                <div className="w-12 h-12 bg-white rounded-lg flex items-center justify-center text-2xl font-bold text-gray-800 shadow-xl">
                  {gameState.redOpeningRoll}
                </div>
              ) : (
                <div className="w-12 h-12 bg-gray-600 rounded-lg flex items-center justify-center text-sm text-gray-400">
                  ?
                </div>
              )}
            </div>

            {/* Tie message */}
            {gameState.isOpeningRollTie && (
              <div className="mt-2 text-yellow-400 text-sm font-bold animate-pulse">
                Tie - Roll Again!
              </div>
            )}
          </div>
        </div>
      )}

      {/* Regular Dice Display - Center on bar (vertical) */}
      {!isOpeningRoll && hasDiceRolled && (
        <div
          className="absolute top-1/2 -translate-x-1/2 -translate-y-1/2 z-10"
          style={{ left: '47%' }}
        >
          <div className="flex flex-col gap-2">
            {gameState.dice.map((die, index) => (
              <div
                key={index}
                className="w-12 h-12 bg-white rounded-lg flex items-center justify-center text-2xl font-bold text-gray-800 shadow-xl"
              >
                {die}
              </div>
            ))}
          </div>
          {gameState.remainingMoves && gameState.remainingMoves.length > 0 && (
            <div className="text-center text-white text-xs mt-2 font-semibold">
              {gameState.remainingMoves.length} move{gameState.remainingMoves.length !== 1 ? 's' : ''} left
            </div>
          )}
        </div>
      )}

      {/* Undo Button - Center of left side of board */}
      {canUndo && (
        <div
          className="absolute top-1/2 -translate-y-1/2 -translate-x-1/2 z-10"
          style={{ left: '29%' }}
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
          style={{ left: '71%' }}
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
