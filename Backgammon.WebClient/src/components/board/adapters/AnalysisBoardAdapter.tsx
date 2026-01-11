import { memo } from 'react'
import { GameState } from '@/types/game.types'
import { GameBoardAdapter } from './GameBoardAdapter'

interface AnalysisBoardAdapterProps {
  gameState: GameState
}

/**
 * AnalysisBoardAdapter is essentially the same as GameBoardAdapter
 * but configured for analysis mode (always non-spectator).
 *
 * The GameBoardAdapter already handles analysis mode via:
 * - gameState.isAnalysisMode flag
 * - isFreeMoveEnabled from gameStore
 * - Different button logic for analysis
 */
export const AnalysisBoardAdapter = memo(function AnalysisBoardAdapter({
  gameState,
}: AnalysisBoardAdapterProps) {
  return (
    <GameBoardAdapter
      gameState={gameState}
      isSpectator={false}
    />
  )
})
