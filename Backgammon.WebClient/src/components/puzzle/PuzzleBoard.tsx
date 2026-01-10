import { useRef, useCallback } from 'react'
import { DailyPuzzle } from '@/types/puzzle.types'
import { usePuzzleStore } from '@/stores/puzzleStore'
import { useBoardDrag, BOARD_CONFIG, BOARD_COLORS } from '@/hooks/useBoardDrag'
import { POINT_COORDS } from '@/lib/boardConstants'

interface PuzzleBoardProps {
  puzzle: DailyPuzzle
}

export function PuzzleBoard({ puzzle }: PuzzleBoardProps) {
  const svgRef = useRef<SVGSVGElement>(null)
  const {
    selectedPoint,
    validDestinations: clickValidDestinations,
    pendingMoves,
    remainingDice,
    addMove,
    setSelectedPoint,
    setValidDestinations: setClickValidDestinations,
  } = usePuzzleStore()

  // Get board state with pending moves applied
  const getBoardWithMoves = useCallback(() => {
    // Start with original board state
    const board = puzzle.boardState.map((p) => ({ ...p }))
    let whiteOnBar = puzzle.whiteCheckersOnBar
    let redOnBar = puzzle.redCheckersOnBar
    let whiteBornOff = puzzle.whiteBornOff
    let redBornOff = puzzle.redBornOff

    // Apply pending moves
    for (const move of pendingMoves) {
      const movingColor = puzzle.currentPlayer

      // Remove from source
      if (move.from === 0) {
        // From bar
        if (movingColor === 'White') whiteOnBar--
        else redOnBar--
      } else {
        const sourcePoint = board.find((p) => p.position === move.from)
        if (sourcePoint && sourcePoint.count > 0) {
          sourcePoint.count--
          if (sourcePoint.count === 0) sourcePoint.color = null
        }
      }

      // Add to destination
      if (move.to === 0 || move.to === 25) {
        // Bear off
        if (movingColor === 'White') whiteBornOff++
        else redBornOff++
      } else {
        const destPoint = board.find((p) => p.position === move.to)
        if (destPoint) {
          // Handle hit
          if (move.isHit && destPoint.count === 1 && destPoint.color !== movingColor) {
            if (destPoint.color === 'White') whiteOnBar++
            else redOnBar++
            destPoint.count = 0
          }
          destPoint.count++
          destPoint.color = movingColor
        }
      }
    }

    return { board, whiteOnBar, redOnBar, whiteBornOff, redBornOff }
  }, [puzzle, pendingMoves])

  const { board, whiteOnBar, redOnBar, whiteBornOff, redBornOff } = getBoardWithMoves()

  // Calculate valid moves from a point
  const getValidMovesFrom = useCallback(
    (from: number): number[] => {
      if (remainingDice.length === 0) return []

      const dests: number[] = []
      const movingColor = puzzle.currentPlayer
      const direction = movingColor === 'White' ? -1 : 1

      // Check if player has checkers on bar - must enter first
      const currentBoard = getBoardWithMoves()
      const onBar = movingColor === 'White' ? currentBoard.whiteOnBar : currentBoard.redOnBar
      if (onBar > 0 && from !== 0) {
        return [] // Must move from bar first
      }

      for (const die of remainingDice) {
        let to: number

        if (from === 0) {
          // Coming from bar
          to = movingColor === 'White' ? 25 - die : die
        } else {
          to = from + direction * die
        }

        // Check if destination is valid
        if (to >= 1 && to <= 24) {
          const destPoint = currentBoard.board.find((p) => p.position === to)
          if (destPoint) {
            // Can land if empty, own color, or single opponent (blot)
            if (
              destPoint.color === null ||
              destPoint.color === movingColor ||
              (destPoint.color !== movingColor && destPoint.count === 1)
            ) {
              if (!dests.includes(to)) dests.push(to)
            }
          }
        }

        // Check bear off - only allowed if all checkers are in home board
        const canBearOff = () => {
          if (movingColor === 'White') {
            // White's home board is points 1-6, must have no checkers on bar or points 7-24
            if (currentBoard.whiteOnBar > 0) return false
            for (const point of currentBoard.board) {
              if (point.color === 'White' && point.position > 6) return false
            }
            return true
          } else {
            // Red's home board is points 19-24, must have no checkers on bar or points 1-18
            if (currentBoard.redOnBar > 0) return false
            for (const point of currentBoard.board) {
              if (point.color === 'Red' && point.position < 19) return false
            }
            return true
          }
        }

        if (movingColor === 'White' && to <= 0 && canBearOff()) {
          if (!dests.includes(0)) dests.push(0)
        } else if (movingColor === 'Red' && to >= 25 && canBearOff()) {
          if (!dests.includes(25)) dests.push(25)
        }
      }

      return dests
    },
    [puzzle.currentPlayer, remainingDice, getBoardWithMoves]
  )

  // Handle completing a move (from drag or click)
  const handleMoveComplete = useCallback(
    (from: number, to: number) => {
      const currentPlayerColor = puzzle.currentPlayer
      const direction = currentPlayerColor === 'White' ? -1 : 1
      const destinations = getValidMovesFrom(from)

      // Check if this is a valid destination
      if (!destinations.includes(to)) return

      // Find the actual die that matches
      const matchingDie = remainingDice.find((d) => {
        const expectedTo =
          from === 0
            ? currentPlayerColor === 'White'
              ? 25 - d
              : d
            : from + direction * d
        return (
          expectedTo === to ||
          (to === 0 && expectedTo <= 0) ||
          (to === 25 && expectedTo >= 25)
        )
      })

      if (matchingDie) {
        // Check if this is a hit
        const currentBoard = getBoardWithMoves()
        const destPoint = currentBoard.board.find((p) => p.position === to)
        const isHit =
          destPoint &&
          destPoint.color !== null &&
          destPoint.color !== currentPlayerColor &&
          destPoint.count === 1

        addMove({
          from,
          to,
          dieValue: matchingDie,
          isHit: !!isHit,
        })
      }

      // Clear selection
      setSelectedPoint(null)
      setClickValidDestinations([])
    },
    [
      puzzle.currentPlayer,
      remainingDice,
      getValidMovesFrom,
      getBoardWithMoves,
      addMove,
      setSelectedPoint,
      setClickValidDestinations,
    ]
  )

  // Initialize drag hook
  const { dragState, validDestinations: dragValidDestinations, startDrag, ghostChecker } =
    useBoardDrag({
      svgRef,
      isBoardFlipped: false,
      onMoveComplete: handleMoveComplete,
      getValidDestinations: getValidMovesFrom,
    })

  // Combined valid destinations (from drag or click selection)
  const activeValidDestinations = dragState.isDragging
    ? dragValidDestinations
    : clickValidDestinations

  // Handle point click (for click-to-move fallback)
  const handlePointClick = (pointNum: number) => {
    // Ignore clicks while dragging
    if (dragState.isDragging) return

    const currentPlayerColor = puzzle.currentPlayer

    // If clicking on a valid destination
    if (selectedPoint !== null && clickValidDestinations.includes(pointNum)) {
      handleMoveComplete(selectedPoint, pointNum)
      return
    }

    // Check if clicking on a source point with our checkers
    const point = board.find((p) => p.position === pointNum)
    if (point && point.color === currentPlayerColor && point.count > 0) {
      // Check if player can move from this point (bar priority)
      const destinations = getValidMovesFrom(pointNum)
      if (destinations.length > 0) {
        setSelectedPoint(pointNum)
        setClickValidDestinations(destinations)
      }
    } else {
      // Deselect
      setSelectedPoint(null)
      setClickValidDestinations([])
    }
  }

  // Handle bar click
  const handleBarClick = () => {
    if (dragState.isDragging) return

    const currentPlayerColor = puzzle.currentPlayer
    const onBar = currentPlayerColor === 'White' ? whiteOnBar : redOnBar

    if (onBar > 0) {
      const destinations = getValidMovesFrom(0)
      if (destinations.length > 0) {
        setSelectedPoint(0)
        setClickValidDestinations(destinations)
      }
    }
  }

  // Check if a checker is draggable
  const isCheckerDraggable = (pointNum: number, color: string | null, isTopChecker: boolean) => {
    if (!isTopChecker) return false
    if (color !== puzzle.currentPlayer) return false
    if (remainingDice.length === 0) return false

    // Check bar priority
    const onBar = puzzle.currentPlayer === 'White' ? whiteOnBar : redOnBar
    if (onBar > 0 && pointNum !== 0) return false

    return getValidMovesFrom(pointNum).length > 0
  }

  // Get point coordinates
  const getPointCoords = (pointNum: number) => {
    return POINT_COORDS[pointNum] || { x: 0, y: 0, direction: 1 }
  }

  // Render point triangle
  const renderPoint = (pointNum: number) => {
    const coords = getPointCoords(pointNum)
    const isTop = coords.direction === 1
    const isDark = (pointNum % 2 === 0) !== isTop

    const points = isTop
      ? `${coords.x},${coords.y} ${coords.x + BOARD_CONFIG.pointWidth / 2},${coords.y + BOARD_CONFIG.pointHeight} ${coords.x + BOARD_CONFIG.pointWidth},${coords.y}`
      : `${coords.x},${coords.y} ${coords.x + BOARD_CONFIG.pointWidth / 2},${coords.y - BOARD_CONFIG.pointHeight} ${coords.x + BOARD_CONFIG.pointWidth},${coords.y}`

    const point = board.find((p) => p.position === pointNum)
    const isSelected = selectedPoint === pointNum || dragState.sourcePoint === pointNum
    const isValidDest = activeValidDestinations.includes(pointNum)

    // Check if destination would be a capture
    const isCapture =
      isValidDest &&
      point &&
      point.color !== null &&
      point.color !== puzzle.currentPlayer &&
      point.count === 1

    // Determine fill color
    let fillColor = isDark ? BOARD_COLORS.pointDark : BOARD_COLORS.pointLight
    if (isSelected) {
      fillColor = BOARD_COLORS.highlightSelected
    } else if (isCapture) {
      fillColor = BOARD_COLORS.highlightCapture
    } else if (isValidDest) {
      fillColor = BOARD_COLORS.highlightDest
    }

    return (
      <g key={pointNum} onClick={() => handlePointClick(pointNum)} style={{ cursor: 'pointer' }}>
        {/* Point triangle */}
        <polygon points={points} fill={fillColor} />

        {/* Checkers on this point */}
        {point &&
          point.count > 0 &&
          renderCheckersOnPoint(pointNum, point.color!, point.count, coords, isTop)}

        {/* Point number */}
        <text
          x={coords.x + BOARD_CONFIG.pointWidth / 2}
          y={isTop ? coords.y - 8 : coords.y + 15}
          textAnchor="middle"
          fill={BOARD_COLORS.textLight}
          fontSize="12"
        >
          {pointNum}
        </text>
      </g>
    )
  }

  // Render checkers on a point
  const renderCheckersOnPoint = (
    pointNum: number,
    color: string,
    count: number,
    coords: { x: number; y: number; direction: number },
    isTop: boolean
  ) => {
    const maxVisible = Math.min(count, 5)
    const checkerColor = color === 'White' ? BOARD_COLORS.checkerWhite : BOARD_COLORS.checkerRed
    const strokeColor =
      color === 'White' ? BOARD_COLORS.checkerWhiteStroke : BOARD_COLORS.checkerRedStroke

    // If dragging from this point, reduce visible count by 1
    const adjustedMaxVisible =
      dragState.isDragging && dragState.sourcePoint === pointNum
        ? Math.max(0, maxVisible - 1)
        : maxVisible
    const adjustedCount =
      dragState.isDragging && dragState.sourcePoint === pointNum
        ? Math.max(0, count - 1)
        : count

    return (
      <>
        {Array.from({ length: adjustedMaxVisible }).map((_, i) => {
          const cy = isTop
            ? coords.y + BOARD_CONFIG.checkerRadius + i * BOARD_CONFIG.checkerSpacing
            : coords.y - BOARD_CONFIG.checkerRadius - i * BOARD_CONFIG.checkerSpacing
          const cx = coords.x + BOARD_CONFIG.pointWidth / 2
          const isTopChecker = i === adjustedMaxVisible - 1
          const draggable = isCheckerDraggable(pointNum, color, isTopChecker)

          return (
            <circle
              key={i}
              cx={cx}
              cy={cy}
              r={BOARD_CONFIG.checkerRadius}
              fill={checkerColor}
              stroke={strokeColor}
              strokeWidth={2}
              style={{ cursor: draggable ? 'grab' : 'default' }}
              onMouseDown={(e) => {
                if (draggable) {
                  startDrag(e, pointNum, color.toLowerCase() as 'white' | 'red')
                }
              }}
              onTouchStart={(e) => {
                if (draggable) {
                  startDrag(e, pointNum, color.toLowerCase() as 'white' | 'red')
                }
              }}
            />
          )
        })}
        {/* Show count if more than 5 */}
        {adjustedCount > 5 && (
          <text
            x={coords.x + BOARD_CONFIG.pointWidth / 2}
            y={
              isTop
                ? coords.y + BOARD_CONFIG.checkerRadius + 4 * BOARD_CONFIG.checkerSpacing
                : coords.y - BOARD_CONFIG.checkerRadius - 4 * BOARD_CONFIG.checkerSpacing
            }
            textAnchor="middle"
            dominantBaseline="middle"
            fill={color === 'White' ? '#333' : '#fff'}
            fontSize="14"
            fontWeight="bold"
          >
            {adjustedCount}
          </text>
        )}
      </>
    )
  }

  // Render bar checkers
  const renderBar = () => {
    const barX = BOARD_CONFIG.barX
    const barY = BOARD_CONFIG.viewBox.height / 2
    const currentPlayerColor = puzzle.currentPlayer
    const currentOnBar = currentPlayerColor === 'White' ? whiteOnBar : redOnBar
    const isBarSelected = selectedPoint === 0 || dragState.sourcePoint === 0

    // Determine fill color
    let barFill = BOARD_COLORS.bar
    if (isBarSelected) {
      barFill = BOARD_COLORS.highlightSelected
    }

    // Adjust white bar count if dragging from bar
    const adjustedWhiteOnBar =
      dragState.isDragging && dragState.sourcePoint === 0 && currentPlayerColor === 'White'
        ? Math.max(0, whiteOnBar - 1)
        : whiteOnBar
    const adjustedRedOnBar =
      dragState.isDragging && dragState.sourcePoint === 0 && currentPlayerColor === 'Red'
        ? Math.max(0, redOnBar - 1)
        : redOnBar

    return (
      <g onClick={handleBarClick} style={{ cursor: currentOnBar > 0 ? 'pointer' : 'default' }}>
        {/* Bar background */}
        <rect
          x={barX}
          y={BOARD_CONFIG.padding}
          width={BOARD_CONFIG.barWidth}
          height={BOARD_CONFIG.viewBox.height - 2 * BOARD_CONFIG.padding}
          fill={barFill}
        />

        {/* White bar checkers (bottom of bar) */}
        {adjustedWhiteOnBar > 0 && (
          <>
            <circle
              cx={barX + BOARD_CONFIG.barWidth / 2}
              cy={barY + 50}
              r={BOARD_CONFIG.checkerRadius}
              fill={BOARD_COLORS.checkerWhite}
              stroke={BOARD_COLORS.checkerWhiteStroke}
              strokeWidth={2}
              style={{
                cursor: isCheckerDraggable(0, 'White', true) ? 'grab' : 'default',
              }}
              onMouseDown={(e) => {
                if (isCheckerDraggable(0, 'White', true)) {
                  startDrag(e, 0, 'white')
                }
              }}
              onTouchStart={(e) => {
                if (isCheckerDraggable(0, 'White', true)) {
                  startDrag(e, 0, 'white')
                }
              }}
            />
            {adjustedWhiteOnBar > 1 && (
              <text
                x={barX + BOARD_CONFIG.barWidth / 2}
                y={barY + 50}
                textAnchor="middle"
                dominantBaseline="middle"
                fill="#333"
                fontSize="14"
                fontWeight="bold"
              >
                {adjustedWhiteOnBar}
              </text>
            )}
          </>
        )}

        {/* Red bar checkers (top of bar) */}
        {adjustedRedOnBar > 0 && (
          <>
            <circle
              cx={barX + BOARD_CONFIG.barWidth / 2}
              cy={barY - 50}
              r={BOARD_CONFIG.checkerRadius}
              fill={BOARD_COLORS.checkerRed}
              stroke={BOARD_COLORS.checkerRedStroke}
              strokeWidth={2}
              style={{
                cursor: isCheckerDraggable(0, 'Red', true) ? 'grab' : 'default',
              }}
              onMouseDown={(e) => {
                if (isCheckerDraggable(0, 'Red', true)) {
                  startDrag(e, 0, 'red')
                }
              }}
              onTouchStart={(e) => {
                if (isCheckerDraggable(0, 'Red', true)) {
                  startDrag(e, 0, 'red')
                }
              }}
            />
            {adjustedRedOnBar > 1 && (
              <text
                x={barX + BOARD_CONFIG.barWidth / 2}
                y={barY - 50}
                textAnchor="middle"
                dominantBaseline="middle"
                fill="#fff"
                fontSize="14"
                fontWeight="bold"
              >
                {adjustedRedOnBar}
              </text>
            )}
          </>
        )}
      </g>
    )
  }

  // Render bear off area
  const renderBearoff = () => {
    const rightEdge = BOARD_CONFIG.viewBox.width - BOARD_CONFIG.margin
    const bearoffX = rightEdge - BOARD_CONFIG.bearoffWidth
    const isWhiteDest = activeValidDestinations.includes(0) && puzzle.currentPlayer === 'White'
    const isRedDest = activeValidDestinations.includes(25) && puzzle.currentPlayer === 'Red'

    return (
      <>
        {/* White bear off (bottom) */}
        <g
          onClick={() => isWhiteDest && handleMoveComplete(selectedPoint ?? dragState.sourcePoint ?? 0, 0)}
          style={{ cursor: isWhiteDest ? 'pointer' : 'default' }}
        >
          <rect
            x={bearoffX}
            y={BOARD_CONFIG.viewBox.height / 2 + 10}
            width={BOARD_CONFIG.bearoffWidth}
            height={BOARD_CONFIG.viewBox.height / 2 - BOARD_CONFIG.padding - 10}
            fill={isWhiteDest ? BOARD_COLORS.highlightDest : BOARD_COLORS.bearoff}
          />
          {whiteBornOff > 0 && (
            <text
              x={bearoffX + BOARD_CONFIG.bearoffWidth / 2}
              y={BOARD_CONFIG.viewBox.height - 50}
              textAnchor="middle"
              fill={BOARD_COLORS.checkerWhite}
              fontSize="18"
              fontWeight="bold"
            >
              {whiteBornOff}
            </text>
          )}
        </g>

        {/* Red bear off (top) */}
        <g
          onClick={() => isRedDest && handleMoveComplete(selectedPoint ?? dragState.sourcePoint ?? 0, 25)}
          style={{ cursor: isRedDest ? 'pointer' : 'default' }}
        >
          <rect
            x={bearoffX}
            y={BOARD_CONFIG.padding}
            width={BOARD_CONFIG.bearoffWidth}
            height={BOARD_CONFIG.viewBox.height / 2 - BOARD_CONFIG.padding - 10}
            fill={isRedDest ? BOARD_COLORS.highlightDest : BOARD_COLORS.bearoff}
          />
          {redBornOff > 0 && (
            <text
              x={bearoffX + BOARD_CONFIG.bearoffWidth / 2}
              y={50}
              textAnchor="middle"
              fill={BOARD_COLORS.checkerRed}
              fontSize="18"
              fontWeight="bold"
            >
              {redBornOff}
            </text>
          )}
        </g>
      </>
    )
  }

  // Render ghost checker (during drag)
  const renderGhostChecker = () => {
    if (!ghostChecker) return null

    const checkerColor =
      ghostChecker.color === 'white' ? BOARD_COLORS.checkerWhite : BOARD_COLORS.checkerRed
    const strokeColor =
      ghostChecker.color === 'white'
        ? BOARD_COLORS.checkerWhiteStroke
        : BOARD_COLORS.checkerRedStroke

    return (
      <circle
        cx={ghostChecker.x}
        cy={ghostChecker.y}
        r={BOARD_CONFIG.checkerRadius}
        fill={checkerColor}
        stroke={strokeColor}
        strokeWidth={2}
        opacity={0.7}
        pointerEvents="none"
        style={{ filter: 'drop-shadow(0 4px 6px rgba(0,0,0,0.3))' }}
      />
    )
  }

  return (
    <div className="w-full aspect-[2/1] bg-background rounded-lg overflow-hidden">
      <svg
        ref={svgRef}
        viewBox={`0 0 ${BOARD_CONFIG.viewBox.width} ${BOARD_CONFIG.viewBox.height}`}
        className="w-full h-full"
        preserveAspectRatio="xMidYMid meet"
      >
        {/* Board background */}
        <rect
          x={BOARD_CONFIG.margin}
          y={BOARD_CONFIG.margin}
          width={BOARD_CONFIG.viewBox.width - 2 * BOARD_CONFIG.margin}
          height={BOARD_CONFIG.viewBox.height - 2 * BOARD_CONFIG.margin}
          fill={BOARD_COLORS.boardBackground}
          stroke={BOARD_COLORS.boardBorder}
          strokeWidth={2}
          rx={8}
        />

        {/* Render points */}
        {Array.from({ length: 24 }, (_, i) => i + 1).map(renderPoint)}

        {/* Render bar */}
        {renderBar()}

        {/* Render bear off areas */}
        {renderBearoff()}

        {/* Render ghost checker during drag */}
        {renderGhostChecker()}
      </svg>
    </div>
  )
}
