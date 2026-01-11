import { memo, useMemo } from 'react'
import { UnifiedBoard, BoardPosition, DiceState, DoublingCubeState } from '../index'

interface MiniPoint {
  position: number
  color: 'White' | 'Red' | null
  count: number
}

interface MiniBoardAdapterProps {
  board?: MiniPoint[]
  whiteOnBar?: number
  redOnBar?: number
  whiteBornOff?: number
  redBornOff?: number
  dice?: number[]
  cubeValue?: number
  cubeOwner?: 'White' | 'Red' | 'Center'
}

export const MiniBoardAdapter = memo(function MiniBoardAdapter({
  board,
  whiteOnBar = 0,
  redOnBar = 0,
  whiteBornOff = 0,
  redBornOff = 0,
  dice,
  cubeValue = 1,
  cubeOwner = 'Center',
}: MiniBoardAdapterProps) {
  // Transform to BoardPosition
  const position = useMemo<BoardPosition>(
    () => ({
      points:
        board?.map((p) => ({
          position: p.position,
          color: p.color?.toLowerCase() as 'white' | 'red' | null,
          count: p.count,
        })) ?? [],
      whiteOnBar,
      redOnBar,
      whiteBornOff,
      redBornOff,
    }),
    [board, whiteOnBar, redOnBar, whiteBornOff, redBornOff]
  )

  // Transform dice
  const diceState = useMemo<DiceState | undefined>(() => {
    if (!dice || dice.length === 0) return undefined
    return {
      values: dice,
      remainingMoves: dice,
    }
  }, [dice])

  // Transform doubling cube
  const cubeState = useMemo<DoublingCubeState | undefined>(() => {
    if (!cubeValue || cubeValue <= 1) return undefined
    return {
      value: cubeValue,
      owner: cubeOwner.toLowerCase() as 'white' | 'red' | 'center',
    }
  }, [cubeValue, cubeOwner])

  return (
    <UnifiedBoard
      position={position}
      display={{
        interactionMode: 'none',
        showPointNumbers: false,
      }}
      dice={diceState}
      doublingCube={cubeState}
    />
  )
})
