import { useRef, useCallback } from 'react'
import type { GameState } from '@/types/generated/Backgammon.Server.Models'
import { CheckerColor } from '@/types/generated/Backgammon.Core'
import { audioService } from '@/services/audio.service'

/**
 * Hook to manage game audio feedback.
 * Detects state changes and plays appropriate sounds.
 */
export function useGameAudio() {
  const prevGameStateRef = useRef<GameState | null>(null)

  /**
   * Detect and play sounds based on game state transitions.
   * Call this BEFORE updating the store state.
   */
  const playGameSounds = useCallback((newState: GameState) => {
    const prevState = prevGameStateRef.current

    if (prevState) {
      // Dice roll detection - check if dice went from no values to having values
      const prevHasDice = prevState.dice && prevState.dice.length > 0 && prevState.dice.some((d) => d > 0)
      const nowHasDice = newState.dice && newState.dice.length > 0 && newState.dice.some((d) => d > 0)
      if (!prevHasDice && nowHasDice) {
        audioService.playSound('dice-roll')
      }

      // Turn change detection - only play sound when it becomes YOUR turn
      const wasPrevYourTurn = prevState.isYourTurn
      const isNowYourTurn = newState.isYourTurn
      if (!wasPrevYourTurn && isNowYourTurn) {
        audioService.playSound('turn-change')
      }

      // Move detection - detect when a move was made by checking if remainingMoves decreased
      const prevBornOff = prevState.whiteBornOff + prevState.redBornOff
      const newBornOff = newState.whiteBornOff + newState.redBornOff
      const prevBar = prevState.whiteCheckersOnBar + prevState.redCheckersOnBar
      const newBar = newState.whiteCheckersOnBar + newState.redCheckersOnBar
      const prevRemainingCount = prevState.remainingMoves?.length ?? 0
      const newRemainingCount = newState.remainingMoves?.length ?? 0

      // Detect what type of move occurred (only if remainingMoves decreased, meaning a move was made)
      if (prevRemainingCount > newRemainingCount && prevRemainingCount > 0) {
        if (newBornOff > prevBornOff) {
          // Checker was born off
          audioService.playSound('bear-off')
        } else if (newBar > prevBar) {
          // Checker was hit (sent to bar)
          audioService.playSound('checker-hit')
        } else {
          // Regular move
          audioService.playSound('checker-move')
        }
      }
    }

    // Update ref for next comparison
    prevGameStateRef.current = newState
  }, [])

  /**
   * Play game over sound based on result.
   */
  const playGameOverSound = useCallback((yourColor: CheckerColor | undefined, winner: CheckerColor | undefined) => {
    if (yourColor === winner) {
      audioService.playSound('game-won')
    } else {
      audioService.playSound('game-lost')
    }
  }, [])

  /**
   * Play double offer sound.
   */
  const playDoubleOfferSound = useCallback(() => {
    audioService.playSound('double-offer')
  }, [])

  /**
   * Play chat message sound.
   */
  const playChatMessageSound = useCallback(() => {
    audioService.playSound('chat-message')
  }, [])

  /**
   * Play timeout sound.
   */
  const playTimeoutSound = useCallback(() => {
    audioService.playSound('game-lost')
  }, [])

  /**
   * Update the previous game state ref (for cases where we receive state without playing sounds).
   */
  const updatePrevGameState = useCallback((state: GameState) => {
    prevGameStateRef.current = state
  }, [])

  return {
    playGameSounds,
    playGameOverSound,
    playDoubleOfferSound,
    playChatMessageSound,
    playTimeoutSound,
    updatePrevGameState,
    prevGameStateRef,
  }
}
