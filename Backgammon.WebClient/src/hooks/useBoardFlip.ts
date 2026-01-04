import { useGameStore } from '@/stores/gameStore'

export const useBoardFlip = () => {
  const { isBoardFlipped, toggleBoardFlip, setBoardFlipped } = useGameStore()

  return {
    isBoardFlipped,
    toggleBoardFlip,
    setBoardFlipped,
  }
}
