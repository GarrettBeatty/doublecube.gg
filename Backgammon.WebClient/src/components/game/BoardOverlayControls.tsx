import React from 'react'
import type { GameState } from '@/types/generated/Backgammon.Server.Models'
import { CheckerColor } from '@/types/generated/Backgammon.Core'

interface BoardOverlayControlsProps {
  gameState: GameState
  isSpectator?: boolean
  isAnalysisMode?: boolean
}

export const BoardOverlayControls: React.FC<BoardOverlayControlsProps> = ({
  gameState,
  isSpectator = false,
}) => {
  if (isSpectator) return null

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

  return (
    <>

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

    </>
  )
}
