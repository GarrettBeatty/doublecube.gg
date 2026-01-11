import { memo, useMemo, useCallback, useEffect, useState } from 'react'
import { DailyPuzzle, PendingPuzzleMove, PuzzleMove } from '@/types/puzzle.types'
import { usePuzzleStore } from '@/stores/puzzleStore'
import { useSignalR } from '@/contexts/SignalRContext'
import { HubMethods } from '@/types/signalr.types'
import {
  UnifiedBoard,
  BoardPosition,
  PointHighlight,
} from '../index'

interface PuzzleBoardAdapterProps {
  puzzle: DailyPuzzle
}

export const PuzzleBoardAdapter = memo(function PuzzleBoardAdapter({
  puzzle,
}: PuzzleBoardAdapterProps) {
  const { invoke } = useSignalR()
  const {
    selectedPoint,
    validDestinations,
    pendingMoves,
    remainingDice,
    addMove,
    setSelectedPoint,
    setValidDestinations,
  } = usePuzzleStore()

  // Store server-validated moves
  const [serverValidMoves, setServerValidMoves] = useState<PuzzleMove[]>([])

  // Apply pending moves to get current board state
  const position = useMemo<BoardPosition>(() => {
    // Start with original board state
    const points = puzzle.boardState.map((p) => ({
      position: p.position,
      color: p.color?.toLowerCase() as 'white' | 'red' | null,
      count: p.count,
    }))

    let whiteOnBar = puzzle.whiteCheckersOnBar
    let redOnBar = puzzle.redCheckersOnBar
    let whiteBornOff = puzzle.whiteBornOff
    let redBornOff = puzzle.redBornOff

    // Apply pending moves
    for (const move of pendingMoves) {
      const movingColor = puzzle.currentPlayer.toLowerCase() as 'white' | 'red'

      // Remove from source
      if (move.from === 0) {
        // From bar
        if (movingColor === 'white') whiteOnBar--
        else redOnBar--
      } else {
        const sourcePoint = points.find((p) => p.position === move.from)
        if (sourcePoint && sourcePoint.count > 0) {
          sourcePoint.count--
          if (sourcePoint.count === 0) sourcePoint.color = null
        }
      }

      // Add to destination
      if (move.to === 0 || move.to === 25) {
        // Bear off
        if (movingColor === 'white') whiteBornOff++
        else redBornOff++
      } else {
        const destPoint = points.find((p) => p.position === move.to)
        if (destPoint) {
          // Handle hit
          if (move.isHit && destPoint.count === 1 && destPoint.color !== movingColor) {
            if (destPoint.color === 'white') whiteOnBar++
            else redOnBar++
            destPoint.count = 0
          }
          destPoint.count++
          destPoint.color = movingColor
        }
      }
    }

    return {
      points,
      whiteOnBar,
      redOnBar,
      whiteBornOff,
      redBornOff,
    }
  }, [puzzle, pendingMoves])

  // Fetch valid moves from server when position/dice/pendingMoves change
  useEffect(() => {
    const fetchValidMoves = async () => {
      if (remainingDice.length === 0) {
        setServerValidMoves([])
        return
      }

      try {
        const request = {
          boardState: puzzle.boardState.map((p) => ({
            position: p.position,
            color: p.color,
            count: p.count,
          })),
          currentPlayer: puzzle.currentPlayer,
          dice: puzzle.dice,
          whiteCheckersOnBar: puzzle.whiteCheckersOnBar,
          redCheckersOnBar: puzzle.redCheckersOnBar,
          whiteBornOff: puzzle.whiteBornOff,
          redBornOff: puzzle.redBornOff,
          pendingMoves: pendingMoves.map((m) => ({
            from: m.from,
            to: m.to,
            dieValue: m.dieValue,
            isHit: m.isHit,
          })),
        }

        const moves = await invoke<PuzzleMove[]>(HubMethods.GetPuzzleValidMoves, request)
        setServerValidMoves(moves ?? [])
      } catch (error) {
        console.error('Error fetching valid moves:', error)
        setServerValidMoves([])
      }
    }

    fetchValidMoves()
  }, [puzzle, pendingMoves, remainingDice.length, invoke])

  // Get valid moves from a point using server-validated moves
  const getValidMovesFrom = useCallback(
    (from: number): number[] => {
      return serverValidMoves
        .filter((m) => m.from === from)
        .map((m) => m.to)
    },
    [serverValidMoves]
  )

  // Find all moveable sources from server-validated moves
  const moveableSources = useMemo(() => {
    const sources = new Set<number>()
    for (const move of serverValidMoves) {
      sources.add(move.from)
    }
    return sources
  }, [serverValidMoves])

  // Build highlights
  const highlights = useMemo<PointHighlight[]>(() => {
    const result: PointHighlight[] = []

    // Highlight moveable sources
    for (const point of moveableSources) {
      if (point !== selectedPoint) {
        result.push({ point, type: 'source' })
      }
    }

    // Highlight selected point
    if (selectedPoint !== null) {
      result.push({ point: selectedPoint, type: 'selected' })
    }

    // Highlight valid destinations
    for (const dest of validDestinations) {
      const destPoint = position.points.find((p) => p.position === dest)
      const movingColor = puzzle.currentPlayer.toLowerCase()
      const isCapture =
        destPoint &&
        destPoint.color !== null &&
        destPoint.color !== movingColor &&
        destPoint.count === 1

      result.push({ point: dest, type: isCapture ? 'capture' : 'destination' })
    }

    return result
  }, [moveableSources, selectedPoint, validDestinations, position.points, puzzle.currentPlayer])

  // Handle point click
  const handlePointClick = useCallback(
    (point: number) => {
      const movingColor = puzzle.currentPlayer.toLowerCase() as 'white' | 'red'
      const pointData = position.points.find((p) => p.position === point)

      // If clicking on own checker, select it
      if (
        (point === 0 && (movingColor === 'white' ? position.whiteOnBar > 0 : position.redOnBar > 0)) ||
        (pointData?.color === movingColor && pointData.count > 0)
      ) {
        if (selectedPoint === point) {
          // Deselect
          setSelectedPoint(null)
          setValidDestinations([])
        } else {
          // Select and show valid moves
          setSelectedPoint(point)
          setValidDestinations(getValidMovesFrom(point))
        }
        return
      }

      // If clicking on a valid destination, make the move
      if (selectedPoint !== null && validDestinations.includes(point)) {
        // Find the matching move from server-validated moves
        const validMove = serverValidMoves.find(
          (m) => m.from === selectedPoint && m.to === point
        )

        if (validMove) {
          const move: PendingPuzzleMove = {
            from: validMove.from,
            to: validMove.to,
            dieValue: validMove.dieValue,
            isHit: validMove.isHit,
          }

          addMove(move)
          setSelectedPoint(null)
          setValidDestinations([])
        }
      }
    },
    [
      puzzle.currentPlayer,
      position,
      selectedPoint,
      validDestinations,
      serverValidMoves,
      getValidMovesFrom,
      addMove,
      setSelectedPoint,
      setValidDestinations,
    ]
  )

  // Handle move attempt from drag
  const handleMoveAttempt = useCallback(
    (from: number, to: number) => {
      // Find the matching move from server-validated moves
      const validMove = serverValidMoves.find(
        (m) => m.from === from && m.to === to
      )

      if (validMove) {
        const move: PendingPuzzleMove = {
          from: validMove.from,
          to: validMove.to,
          dieValue: validMove.dieValue,
          isHit: validMove.isHit,
        }

        addMove(move)
      }
    },
    [serverValidMoves, addMove]
  )

  // Check if a point is draggable
  const isDraggable = useCallback(
    (point: number): boolean => {
      return moveableSources.has(point)
    },
    [moveableSources]
  )

  return (
    <UnifiedBoard
      position={position}
      display={{
        showPointNumbers: true,
        interactionMode: 'both',
      }}
      highlights={highlights}
      interaction={{
        onMoveAttempt: handleMoveAttempt,
        getValidDestinations: getValidMovesFrom,
        isDraggable,
        onPointClick: handlePointClick,
      }}
    />
  )
})
