import React, { useRef, useEffect, useCallback, useState } from 'react'
import { GameState, Point, CheckerColor } from '@/types/game.types'
import { useGameStore } from '@/stores/gameStore'
import { useSignalR } from '@/contexts/SignalRContext'
import { HubMethods } from '@/types/signalr.types'

// SVG Namespace
const SVG_NS = 'http://www.w3.org/2000/svg'

// Configuration constants
const CONFIG: {
  viewBox: { width: number; height: number }
  sidebarWidth: number
  margin: number
  barWidth: number
  pointWidth: number
  pointHeight: number
  padding: number
  checkerRadius: number
  checkerSpacing: number
  bearoffWidth: number
  boardStartX?: number
  barX?: number
} = {
  viewBox: { width: 1020, height: 500 },
  sidebarWidth: 0,
  margin: 30,
  barWidth: 70,
  pointWidth: 72,
  pointHeight: 200,
  padding: 20,
  checkerRadius: 20,
  checkerSpacing: 38,
  bearoffWidth: 50,
}

CONFIG.boardStartX = CONFIG.sidebarWidth + CONFIG.margin
CONFIG.barX = CONFIG.boardStartX + 6 * CONFIG.pointWidth

// Color palette - matching shadcn default theme (neutral grays)
const COLORS = {
  boardBackground: 'hsl(0 0% 14%)',        // Dark neutral background
  boardBorder: 'hsl(0 0% 22%)',            // Slightly lighter border
  pointLight: 'hsl(0 0% 32%)',             // Light point color
  pointDark: 'hsl(0 0% 20%)',              // Dark point color
  bar: 'hsl(0 0% 11%)',                    // Bar area
  bearoff: 'hsl(0 0% 11%)',                // Bear-off area
  checkerWhite: 'hsl(0 0% 98%)',           // White checkers
  checkerWhiteStroke: 'hsl(0 0% 72%)',    // White checker border
  checkerRed: 'hsl(0 84.2% 60.2%)',        // Red checkers (primary color)
  checkerRedStroke: 'hsl(0 72.2% 50.6%)',  // Red checker border (darker primary)
  highlightSource: 'hsla(47.9 95.8% 53.1% / 0.6)',     // Warning/yellow for source
  highlightSelected: 'hsla(142.1 76.2% 36.3% / 0.7)',  // Success/green for selected
  highlightDest: 'hsla(221.2 83.2% 53.3% / 0.6)',      // Primary/blue for destinations
  highlightCapture: 'hsla(0 84.2% 60.2% / 0.6)',       // Destructive/red for captures
  textLight: 'hsla(0 0% 98% / 0.5)',       // Light text
  textDark: 'hsla(0 0% 9% / 0.7)',         // Dark text
}

// Pre-calculate point coordinates
const POINT_COORDS: Record<
  number,
  { x: number; y: number; direction: number }
> = {}

function calculatePointCoords() {
  const rightSideStart = CONFIG.barX! + CONFIG.barWidth

  // Top row - points 13-18 (left of bar)
  for (let i = 0; i < 6; i++) {
    const pointNum = 13 + i
    POINT_COORDS[pointNum] = {
      x: CONFIG.boardStartX! + i * CONFIG.pointWidth,
      y: CONFIG.padding,
      direction: 1,
    }
  }

  // Top row - points 19-24 (right of bar)
  for (let i = 0; i < 6; i++) {
    const pointNum = 19 + i
    POINT_COORDS[pointNum] = {
      x: rightSideStart + i * CONFIG.pointWidth,
      y: CONFIG.padding,
      direction: 1,
    }
  }

  // Bottom row - points 12-7 (left of bar)
  for (let i = 0; i < 6; i++) {
    const pointNum = 12 - i
    POINT_COORDS[pointNum] = {
      x: CONFIG.boardStartX! + i * CONFIG.pointWidth,
      y: CONFIG.viewBox.height - CONFIG.padding,
      direction: -1,
    }
  }

  // Bottom row - points 6-1 (right of bar)
  for (let i = 0; i < 6; i++) {
    const pointNum = 6 - i
    POINT_COORDS[pointNum] = {
      x: rightSideStart + i * CONFIG.pointWidth,
      y: CONFIG.viewBox.height - CONFIG.padding,
      direction: -1,
    }
  }
}

calculatePointCoords()

// Drag state type
interface DragState {
  isDragging: boolean
  sourcePoint: number | null
  draggedChecker: SVGCircleElement | null
  ghostChecker: SVGCircleElement | null
  draggedCheckerOriginalPos: { cx: number; cy: number } | null
  offset: { x: number; y: number } | null
}

interface BoardSVGProps {
  gameState: GameState | null
}

export const BoardSVG: React.FC<BoardSVGProps> = ({ gameState }) => {
  const svgRef = useRef<SVGSVGElement>(null)
  const checkersGroupRef = useRef<SVGGElement | null>(null)
  const pointsGroupRef = useRef<SVGGElement | null>(null)

  const { selectedChecker, validSources, isBoardFlipped } = useGameStore()
  const { invoke } = useSignalR()

  // Drag state - use ref to avoid stale closures in event handlers
  const dragStateRef = useRef<DragState>({
    isDragging: false,
    sourcePoint: null,
    draggedChecker: null,
    ghostChecker: null,
    draggedCheckerOriginalPos: null,
    offset: null,
  })

  // Highlighted points state
  const [highlightedPoints, setHighlightedPoints] = useState<{
    source: number | null
    destinations: number[]
    captures: number[]
  }>({
    source: null,
    destinations: [],
    captures: [],
  })

  // Helper: Create SVG element
  const createSVGElement = (
    tag: string,
    attributes: Record<string, string | number> = {}
  ) => {
    const el = document.createElementNS(SVG_NS, tag)
    Object.entries(attributes).forEach(([key, value]) => {
      el.setAttribute(key, String(value))
    })
    return el
  }

  // Create point triangle
  const createPointTriangle = (pointNum: number) => {
    const coords = POINT_COORDS[pointNum]
    const { x, y, direction } = coords
    const w = CONFIG.pointWidth
    const h = CONFIG.pointHeight * direction

    const points = `${x},${y} ${x + w / 2},${y + h} ${x + w},${y}`

    const color = pointNum % 2 === 0 ? COLORS.pointLight : COLORS.pointDark

    return createSVGElement('polygon', {
      points,
      fill: color,
      stroke: 'none',
      class: `point point-${pointNum}`,
    })
  }

  // Create checker
  const createChecker = (
    pointNum: number,
    index: number,
    color: CheckerColor,
    isSelected: boolean,
    isDraggable: boolean
  ) => {
    const coords = POINT_COORDS[pointNum]
    const { x, y, direction } = coords

    const cx = x + CONFIG.pointWidth / 2
    const cy = y + CONFIG.checkerSpacing * index * direction

    const checkerColor =
      color === CheckerColor.White ? COLORS.checkerWhite : COLORS.checkerRed
    const strokeColor =
      color === CheckerColor.White
        ? COLORS.checkerWhiteStroke
        : COLORS.checkerRedStroke

    const circle = createSVGElement('circle', {
      cx,
      cy,
      r: CONFIG.checkerRadius,
      fill: checkerColor,
      stroke: strokeColor,
      'stroke-width': 2,
      class: `checker ${isDraggable ? 'draggable' : ''}`,
      'data-point': pointNum,
      'data-color': color,
    }) as SVGCircleElement

    // Highlight draggable checkers with yellow glow
    if (isDraggable && !isSelected) {
      circle.setAttribute('stroke', COLORS.highlightSource)
      circle.setAttribute('stroke-width', '3')
    }

    // Selected checker gets a stronger highlight
    if (isSelected) {
      circle.setAttribute('stroke', 'yellow')
      circle.setAttribute('stroke-width', '4')
    }

    // Add drag-and-drop event listeners for draggable checkers
    if (isDraggable) {
      circle.addEventListener('mousedown', (e: MouseEvent) => {
        e.preventDefault()
        startDrag(e as any, circle, e.clientX, e.clientY)
      })

      circle.addEventListener('touchstart', (e: TouchEvent) => {
        e.preventDefault()
        const touch = e.touches[0]
        startDrag(e as any, circle, touch.clientX, touch.clientY)
      })
    }

    return circle
  }

  // Get valid destinations for a source point
  const getValidDestinationsForPoint = useCallback(
    (sourcePoint: number) => {
      if (!gameState?.validMoves) return { destinations: [], captures: [] }

      const movesFromPoint = gameState.validMoves.filter(
        (move) => move.from === sourcePoint
      )

      const destinations: number[] = []
      const captures: number[] = []

      movesFromPoint.forEach((move) => {
        if (!destinations.includes(move.to) && !captures.includes(move.to)) {
          // Check if this is a capture by looking at the destination point
          const destPoint = gameState.board[move.to - 1]
          const currentPlayerColor = gameState.currentPlayer

          if (
            destPoint &&
            destPoint.count === 1 &&
            destPoint.color !== null &&
            destPoint.color !== currentPlayerColor
          ) {
            captures.push(move.to)
          } else {
            destinations.push(move.to)
          }
        }
      })

      return { destinations, captures }
    },
    [gameState]
  )

  // Update point highlights
  const updateHighlights = useCallback(() => {
    if (!pointsGroupRef.current) return

    // Reset all point fills
    const points = pointsGroupRef.current.querySelectorAll('polygon.point')
    points.forEach((point) => {
      const pointNum = parseInt(
        point.getAttribute('class')?.match(/point-(\d+)/)?.[1] || '0'
      )
      const baseColor =
        pointNum % 2 === 0 ? COLORS.pointLight : COLORS.pointDark
      point.setAttribute('fill', baseColor)
    })

    // Apply highlights
    if (highlightedPoints.source !== null) {
      const sourcePoint = pointsGroupRef.current.querySelector(
        `.point-${highlightedPoints.source}`
      )
      if (sourcePoint) {
        sourcePoint.setAttribute('fill', COLORS.highlightSource)
      }
    }

    highlightedPoints.destinations.forEach((pointNum) => {
      const point = pointsGroupRef.current!.querySelector(`.point-${pointNum}`)
      if (point) {
        point.setAttribute('fill', COLORS.highlightDest)
      }
    })

    highlightedPoints.captures.forEach((pointNum) => {
      const point = pointsGroupRef.current!.querySelector(`.point-${pointNum}`)
      if (point) {
        point.setAttribute('fill', COLORS.highlightCapture)
      }
    })
  }, [highlightedPoints])

  // Apply highlights whenever highlightedPoints changes
  useEffect(() => {
    updateHighlights()
  }, [updateHighlights])

  // Get point number at client coordinates
  const getPointAtPosition = useCallback(
    (clientX: number, clientY: number): number | null => {
      if (!svgRef.current) return null

      const rect = svgRef.current.getBoundingClientRect()

      // Calculate aspect ratios
      const renderedAspect = rect.width / rect.height
      const viewBoxAspect = CONFIG.viewBox.width / CONFIG.viewBox.height

      // Adjust for non-uniform scaling (letterboxing/pillarboxing)
      let effectiveWidth = rect.width
      let effectiveHeight = rect.height
      let offsetX = 0
      let offsetY = 0

      if (renderedAspect > viewBoxAspect) {
        // Letterboxed (black bars on sides)
        effectiveWidth = rect.height * viewBoxAspect
        offsetX = (rect.width - effectiveWidth) / 2
      } else if (renderedAspect < viewBoxAspect) {
        // Pillarboxed (black bars on top/bottom)
        effectiveHeight = rect.width / viewBoxAspect
        offsetY = (rect.height - effectiveHeight) / 2
      }

      const scaleX = CONFIG.viewBox.width / effectiveWidth
      const scaleY = CONFIG.viewBox.height / effectiveHeight

      let x = (clientX - rect.left - offsetX) * scaleX
      let y = (clientY - rect.top - offsetY) * scaleY

      // Account for board flip
      if (isBoardFlipped) {
        x = CONFIG.viewBox.width - x
        y = CONFIG.viewBox.height - y
      }

      // Check bar
      if (x >= CONFIG.barX! && x <= CONFIG.barX! + CONFIG.barWidth) {
        return 0 // Bar
      }

      // Check bear-off
      if (x >= CONFIG.viewBox.width - CONFIG.bearoffWidth - 10) {
        return 25 // Bear-off
      }

      // Check points
      const isTop = y < CONFIG.viewBox.height / 2
      const isLeftSide = x < CONFIG.barX!

      let pointIndex: number
      if (isLeftSide) {
        pointIndex = Math.floor((x - CONFIG.boardStartX!) / CONFIG.pointWidth)
        if (pointIndex < 0 || pointIndex > 5) return null
      } else {
        const rightStart = CONFIG.barX! + CONFIG.barWidth
        pointIndex = Math.floor((x - rightStart) / CONFIG.pointWidth)
        if (pointIndex < 0 || pointIndex > 5) return null
      }

      // Map to point number
      let result: number
      if (isTop) {
        // Top row: 13-18 (left), 19-24 (right)
        result = isLeftSide ? 13 + pointIndex : 19 + pointIndex
      } else {
        // Bottom row: 12-7 (left), 6-1 (right)
        result = isLeftSide ? 12 - pointIndex : 6 - pointIndex
      }

      return result
    },
    [isBoardFlipped]
  )

  // Drag handlers
  const handleDragMove = useCallback((event: MouseEvent | TouchEvent) => {
    const dragState = dragStateRef.current
    if (!dragState.isDragging || !dragState.ghostChecker) return

    event.preventDefault()

    let clientX: number, clientY: number
    if (event.type === 'touchmove') {
      const touch = (event as TouchEvent).touches[0]
      clientX = touch.clientX
      clientY = touch.clientY
    } else {
      clientX = (event as MouseEvent).clientX
      clientY = (event as MouseEvent).clientY
    }

    if (!svgRef.current) return

    // Convert to SVG coordinates
    const svgPoint = svgRef.current.createSVGPoint()
    svgPoint.x = clientX
    svgPoint.y = clientY
    const ctm = svgRef.current.getScreenCTM()
    if (!ctm) return

    const svgCoords = svgPoint.matrixTransform(ctm.inverse())

    // Update ghost checker position
    dragState.ghostChecker.setAttribute(
      'cx',
      String(svgCoords.x - (dragState.offset?.x || 0))
    )
    dragState.ghostChecker.setAttribute(
      'cy',
      String(svgCoords.y - (dragState.offset?.y || 0))
    )
  }, [])

  const handleDragEnd = useCallback(
    async (event: MouseEvent | TouchEvent) => {
      const dragState = dragStateRef.current
      if (!dragState.isDragging) return

      event.preventDefault()

      let clientX: number, clientY: number
      if (event.type === 'touchend') {
        const touch = (event as TouchEvent).changedTouches[0]
        clientX = touch.clientX
        clientY = touch.clientY
      } else {
        clientX = (event as MouseEvent).clientX
        clientY = (event as MouseEvent).clientY
      }

      // Determine drop target
      const targetPoint = getPointAtPosition(clientX, clientY)

      // Clean up drag state
      if (dragState.ghostChecker) {
        dragState.ghostChecker.remove()
      }
      if (dragState.draggedChecker) {
        dragState.draggedChecker.style.opacity = '1'
        dragState.draggedChecker.style.cursor = 'grab'
      }

      // Remove global listeners
      document.removeEventListener('mousemove', handleDragMove)
      document.removeEventListener('mouseup', handleDragEnd)
      document.removeEventListener('touchmove', handleDragMove)
      document.removeEventListener('touchend', handleDragEnd)

      const sourcePoint = dragState.sourcePoint

      // Clear highlights
      setHighlightedPoints({
        source: null,
        destinations: [],
        captures: [],
      })

      // Reset drag state
      dragStateRef.current = {
        isDragging: false,
        sourcePoint: null,
        draggedChecker: null,
        ghostChecker: null,
        draggedCheckerOriginalPos: null,
        offset: null,
      }

      // Execute move if valid target
      if (
        targetPoint !== null &&
        targetPoint !== sourcePoint &&
        sourcePoint !== null
      ) {
        try {
          await invoke(HubMethods.MakeMove, sourcePoint, targetPoint)
        } catch (error) {
          console.error('Failed to execute move:', error)
        }
      }
    },
    [getPointAtPosition, invoke, handleDragMove]
  )

  const startDrag = useCallback(
    (
      _event: React.MouseEvent | React.TouchEvent,
      checkerElement: SVGCircleElement,
      clientX: number,
      clientY: number
    ) => {
      const pointNum = parseInt(
        checkerElement.getAttribute('data-point') || '0'
      )
      const color = parseInt(checkerElement.getAttribute('data-color') || '0')

      if (!svgRef.current || !checkersGroupRef.current) return

      // Get SVG coordinates
      const svgPoint = svgRef.current.createSVGPoint()
      svgPoint.x = clientX
      svgPoint.y = clientY
      const ctm = svgRef.current.getScreenCTM()
      if (!ctm) return

      const svgCoords = svgPoint.matrixTransform(ctm.inverse())

      // Get original position
      const cx = parseFloat(checkerElement.getAttribute('cx') || '0')
      const cy = parseFloat(checkerElement.getAttribute('cy') || '0')

      // Calculate offset
      const offset = {
        x: svgCoords.x - cx,
        y: svgCoords.y - cy,
      }

      // Create ghost checker
      const ghostChecker = createSVGElement('circle', {
        class: `checker ${color === CheckerColor.White ? 'checker-white' : 'checker-red'} dragging`,
        cx: svgCoords.x - offset.x,
        cy: svgCoords.y - offset.y,
        r: CONFIG.checkerRadius,
        opacity: '0.7',
        'pointer-events': 'none',
        fill:
          color === CheckerColor.White
            ? COLORS.checkerWhite
            : COLORS.checkerRed,
        stroke:
          color === CheckerColor.White
            ? COLORS.checkerWhiteStroke
            : COLORS.checkerRedStroke,
        'stroke-width': 2,
      }) as SVGCircleElement
      checkersGroupRef.current.appendChild(ghostChecker)

      // Hide original checker
      checkerElement.style.opacity = '0.3'
      checkerElement.style.cursor = 'grabbing'

      // Update drag state
      dragStateRef.current = {
        isDragging: true,
        sourcePoint: pointNum,
        draggedChecker: checkerElement,
        ghostChecker,
        draggedCheckerOriginalPos: { cx, cy },
        offset,
      }

      // Set highlights for valid destinations
      const { destinations, captures } = getValidDestinationsForPoint(pointNum)
      setHighlightedPoints({
        source: pointNum,
        destinations,
        captures,
      })

      // Add global listeners
      document.addEventListener(
        'mousemove',
        handleDragMove as unknown as EventListener
      )
      document.addEventListener(
        'mouseup',
        handleDragEnd as unknown as EventListener
      )
      document.addEventListener(
        'touchmove',
        handleDragMove as unknown as EventListener,
        {
          passive: false,
        }
      )
      document.addEventListener(
        'touchend',
        handleDragEnd as unknown as EventListener
      )
    },
    [handleDragMove, handleDragEnd, getValidDestinationsForPoint]
  )

  // Render board structure
  const renderBoard = useCallback(() => {
    if (!svgRef.current) return

    // Clear existing
    svgRef.current.innerHTML = ''

    // Background
    const bg = createSVGElement('rect', {
      x: 0,
      y: 0,
      width: CONFIG.viewBox.width,
      height: CONFIG.viewBox.height,
      fill: COLORS.boardBackground,
      stroke: COLORS.boardBorder,
      'stroke-width': 4,
    })
    svgRef.current.appendChild(bg)

    // Points group
    const pointsGroup = createSVGElement('g', { id: 'points' }) as SVGGElement
    for (let i = 1; i <= 24; i++) {
      const triangle = createPointTriangle(i)
      pointsGroup.appendChild(triangle)
    }
    svgRef.current.appendChild(pointsGroup)
    pointsGroupRef.current = pointsGroup

    // Bar
    const barGroup = createSVGElement('g', { id: 'bar' }) as SVGGElement
    const barRect = createSVGElement('rect', {
      x: CONFIG.barX!,
      y: CONFIG.padding,
      width: CONFIG.barWidth,
      height: CONFIG.viewBox.height - 2 * CONFIG.padding,
      fill: COLORS.bar,
    })
    barGroup.appendChild(barRect)
    svgRef.current.appendChild(barGroup)

    // Bearoff areas
    const bearoffGroup = createSVGElement('g', { id: 'bearoff' }) as SVGGElement
    const bearoffRight = createSVGElement('rect', {
      x: CONFIG.barX! + CONFIG.barWidth + 6 * CONFIG.pointWidth,
      y: CONFIG.padding,
      width: CONFIG.bearoffWidth,
      height: CONFIG.viewBox.height - 2 * CONFIG.padding,
      fill: COLORS.bearoff,
    })
    bearoffGroup.appendChild(bearoffRight)
    svgRef.current.appendChild(bearoffGroup)

    // Checkers group
    const checkersGroup = createSVGElement('g', { id: 'checkers' }) as SVGGElement
    svgRef.current.appendChild(checkersGroup)
    checkersGroupRef.current = checkersGroup

    // Dice group
    const diceGroup = createSVGElement('g', { id: 'dice' }) as SVGGElement
    svgRef.current.appendChild(diceGroup)
  }, [])

  // Render checkers
  const renderCheckers = useCallback(() => {
    if (!checkersGroupRef.current || !gameState?.board) return

    checkersGroupRef.current.innerHTML = ''

    gameState.board.forEach((point: Point, index: number) => {
      const pointNum = index + 1
      if (point.color !== null && point.count > 0) {
        const maxVisible = Math.min(point.count, 5)

        for (let i = 0; i < maxVisible; i++) {
          const isTopChecker = i === maxVisible - 1
          const isSelected =
            selectedChecker?.point === pointNum && isTopChecker
          const isDraggable = isTopChecker && validSources.includes(pointNum)

          const checker = createChecker(
            pointNum,
            i,
            point.color,
            isSelected,
            isDraggable
          )

          checkersGroupRef.current!.appendChild(checker)
        }

        // Show count if more than 5
        if (point.count > 5) {
          const coords = POINT_COORDS[pointNum]
          const textColor =
            point.color === CheckerColor.White
              ? COLORS.textDark
              : COLORS.textLight

          const text = createSVGElement('text', {
            x: coords.x + CONFIG.pointWidth / 2,
            y: coords.y + CONFIG.checkerSpacing * 4 * coords.direction,
            'text-anchor': 'middle',
            'dominant-baseline': 'middle',
            fill: textColor,
            'font-size': 16,
            'font-weight': 'bold',
          })
          text.textContent = String(point.count)
          checkersGroupRef.current!.appendChild(text)
        }
      }
    })
  }, [gameState, selectedChecker, validSources, startDrag])

  // Initialize board
  useEffect(() => {
    renderBoard()
  }, [renderBoard])

  // Re-render checkers when game state changes
  useEffect(() => {
    renderCheckers()
  }, [renderCheckers])

  return (
    <svg
      ref={svgRef}
      viewBox={`0 0 ${CONFIG.viewBox.width} ${CONFIG.viewBox.height}`}
      className={`board-svg w-full h-auto ${isBoardFlipped ? 'rotate-180' : ''}`}
      style={{ transition: 'transform 0.6s' }}
    />
  )
}
