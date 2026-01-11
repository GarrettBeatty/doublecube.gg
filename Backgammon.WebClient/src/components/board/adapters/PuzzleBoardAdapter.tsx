import { memo, useMemo, useCallback } from 'react'
import { DailyPuzzle, PendingPuzzleMove } from '@/types/puzzle.types'
import { usePuzzleStore } from '@/stores/puzzleStore'
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
  const {
    selectedPoint,
    validDestinations,
    pendingMoves,
    remainingDice,
    addMove,
    setSelectedPoint,
    setValidDestinations,
  } = usePuzzleStore()

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

  // Calculate valid moves from a point
  const getValidMovesFrom = useCallback(
    (from: number): number[] => {
      if (remainingDice.length === 0) return []

      const dests: number[] = []
      const movingColor = puzzle.currentPlayer.toLowerCase() as 'white' | 'red'
      const direction = movingColor === 'white' ? -1 : 1

      // Check if player has checkers on bar
      const onBar = movingColor === 'white' ? position.whiteOnBar : position.redOnBar
      if (onBar > 0 && from !== 0) {
        return [] // Must move from bar first
      }

      for (const die of remainingDice) {
        let to: number

        if (from === 0) {
          // Coming from bar
          to = movingColor === 'white' ? 25 - die : die
        } else {
          to = from + direction * die
        }

        // Check if destination is valid
        if (to >= 1 && to <= 24) {
          const destPoint = position.points.find((p) => p.position === to)
          if (destPoint) {
            if (
              destPoint.color === null ||
              destPoint.color === movingColor ||
              (destPoint.color !== movingColor && destPoint.count === 1)
            ) {
              if (!dests.includes(to)) dests.push(to)
            }
          }
        }

        // Check bear off
        const canBearOff = () => {
          if (movingColor === 'white') {
            if (position.whiteOnBar > 0) return false
            for (const point of position.points) {
              if (point.color === 'white' && point.position > 6) return false
            }
            return true
          } else {
            if (position.redOnBar > 0) return false
            for (const point of position.points) {
              if (point.color === 'red' && point.position < 19) return false
            }
            return true
          }
        }

        if (movingColor === 'white' && to <= 0 && canBearOff()) {
          if (!dests.includes(0)) dests.push(0)
        } else if (movingColor === 'red' && to >= 25 && canBearOff()) {
          if (!dests.includes(25)) dests.push(25)
        }
      }

      return dests
    },
    [puzzle.currentPlayer, remainingDice, position]
  )

  // Find all moveable sources
  const moveableSources = useMemo(() => {
    const sources = new Set<number>()
    const movingColor = puzzle.currentPlayer.toLowerCase() as 'white' | 'red'

    // Check bar first
    const onBar = movingColor === 'white' ? position.whiteOnBar : position.redOnBar
    if (onBar > 0) {
      if (getValidMovesFrom(0).length > 0) {
        sources.add(0)
      }
      return sources // Must move from bar first
    }

    // Check all points
    for (const point of position.points) {
      if (point.color === movingColor && point.count > 0) {
        if (getValidMovesFrom(point.position).length > 0) {
          sources.add(point.position)
        }
      }
    }

    return sources
  }, [position, puzzle.currentPlayer, getValidMovesFrom])

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
        const distance = selectedPoint === 0
          ? (movingColor === 'white' ? 25 - point : point)
          : Math.abs(point - selectedPoint)

        // Find which die value to use
        let dieValue = distance
        if (!remainingDice.includes(dieValue)) {
          // For bearing off, might use a larger die
          dieValue = remainingDice.find((d) => d >= distance) ?? distance
        }

        const isHit =
          pointData &&
          pointData.color !== null &&
          pointData.color !== movingColor &&
          pointData.count === 1

        const move: PendingPuzzleMove = {
          from: selectedPoint,
          to: point,
          dieValue,
          isHit: isHit ?? false,
        }

        addMove(move)
        setSelectedPoint(null)
        setValidDestinations([])
      }
    },
    [
      puzzle.currentPlayer,
      position,
      selectedPoint,
      validDestinations,
      remainingDice,
      getValidMovesFrom,
      addMove,
      setSelectedPoint,
      setValidDestinations,
    ]
  )

  // Handle move attempt from drag
  const handleMoveAttempt = useCallback(
    (from: number, to: number) => {
      const validDests = getValidMovesFrom(from)
      if (!validDests.includes(to)) return

      const movingColor = puzzle.currentPlayer.toLowerCase() as 'white' | 'red'
      const destPoint = position.points.find((p) => p.position === to)
      const distance = from === 0
        ? (movingColor === 'white' ? 25 - to : to)
        : Math.abs(to - from)

      let dieValue = distance
      if (!remainingDice.includes(dieValue)) {
        dieValue = remainingDice.find((d) => d >= distance) ?? distance
      }

      const isHit =
        destPoint &&
        destPoint.color !== null &&
        destPoint.color !== movingColor &&
        destPoint.count === 1

      const move: PendingPuzzleMove = {
        from,
        to,
        dieValue,
        isHit: isHit ?? false,
      }

      addMove(move)
    },
    [puzzle.currentPlayer, position.points, remainingDice, getValidMovesFrom, addMove]
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
