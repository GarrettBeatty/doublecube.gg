import React, { useRef, useEffect, useCallback, useState } from 'react'
import { GameState, Point, CheckerColor, GameStatus } from '@/types/game.types'
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
  margin: 10,
  barWidth: 70,
  pointWidth: 72,
  pointHeight: 200,
  padding: 25,
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
  highlightAnalysis: 'hsla(142.1 76.2% 36.3% / 0.5)',  // Green for suggested moves
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
  isSpectator?: boolean
}

// Internal component (not exported)
const BoardSVGComponent: React.FC<BoardSVGProps> = ({ gameState, isSpectator = false }) => {
  const svgRef = useRef<SVGSVGElement>(null)
  const checkersGroupRef = useRef<SVGGElement | null>(null)
  const pointsGroupRef = useRef<SVGGElement | null>(null)
  const diceGroupRef = useRef<SVGGElement | null>(null)
  const buttonsGroupRef = useRef<SVGGElement | null>(null)

  const { selectedChecker, validSources, isBoardFlipped, isFreeMoveEnabled, highlightedMoves } =
    useGameStore()
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
    sources: number[]
    source: number | null
    destinations: number[]
    captures: number[]
    analysisMoves: Array<{ from: number; to: number }>
  }>({
    sources: [],
    source: null,
    destinations: [],
    captures: [],
    analysisMoves: [],
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

    // Selected checker gets a highlight (the triangle will be highlighted instead)
    if (isSelected) {
      circle.setAttribute('stroke', COLORS.highlightSelected)
      circle.setAttribute('stroke-width', '4')
    }

    // Add drag-and-drop event listeners for draggable checkers
    if (isDraggable) {
      circle.addEventListener('mousedown', (e: MouseEvent) => {
        e.preventDefault()
        startDrag(e, circle, e.clientX, e.clientY)
      })

      circle.addEventListener('touchstart', (e: TouchEvent) => {
        e.preventDefault()
        const touch = e.touches[0]
        startDrag(e, circle, touch.clientX, touch.clientY)
      })
    }

    return circle
  }

  // Get valid destinations for a source point
  const getValidDestinationsForPoint = useCallback(
    (sourcePoint: number) => {
      // No highlights in analysis mode with free movement enabled
      if (gameState?.isAnalysisMode && isFreeMoveEnabled) {
        return { destinations: [], captures: [] }
      }

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
    [gameState, isFreeMoveEnabled]
  )

  // Update point highlights
  const updateHighlights = useCallback(() => {
    if (!pointsGroupRef.current || !svgRef.current) return

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

    // Reset bar fill
    const barRect = svgRef.current.querySelector('#bar rect')
    if (barRect) {
      barRect.setAttribute('fill', COLORS.bar)
    }

    // Apply highlights
    // Highlight all valid source points in yellow
    highlightedPoints.sources.forEach((pointNum) => {
      if (pointNum === 0) {
        // Highlight the bar
        if (barRect) {
          barRect.setAttribute('fill', COLORS.highlightSource)
        }
      } else {
        const point = pointsGroupRef.current!.querySelector(`.point-${pointNum}`)
        if (point) {
          point.setAttribute('fill', COLORS.highlightSource)
        }
      }
    })

    // Highlight selected point in green (overrides source highlight if both)
    if (highlightedPoints.source !== null) {
      if (highlightedPoints.source === 0) {
        // Highlight the bar as selected
        if (barRect) {
          barRect.setAttribute('fill', COLORS.highlightSelected)
        }
      } else {
        const sourcePoint = pointsGroupRef.current.querySelector(
          `.point-${highlightedPoints.source}`
        )
        if (sourcePoint) {
          sourcePoint.setAttribute('fill', COLORS.highlightSelected)
        }
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

    // Highlight analysis suggested moves (both from and to points)
    highlightedPoints.analysisMoves.forEach((move) => {
      const fromPoint = pointsGroupRef.current!.querySelector(`.point-${move.from}`)
      const toPoint = pointsGroupRef.current!.querySelector(`.point-${move.to}`)

      if (fromPoint && move.from !== 0 && move.from !== 25) {
        fromPoint.setAttribute('fill', COLORS.highlightAnalysis)
      }
      if (toPoint && move.to !== 0 && move.to !== 25) {
        toPoint.setAttribute('fill', COLORS.highlightAnalysis)
      }
    })
  }, [highlightedPoints])

  // Apply highlights whenever highlightedPoints changes
  useEffect(() => {
    updateHighlights()
  }, [updateHighlights])

  // Update highlighted sources whenever validSources changes
  useEffect(() => {
    setHighlightedPoints((prev) => ({
      ...prev,
      sources: validSources,
    }))
  }, [validSources])

  // Update analysis move highlights
  useEffect(() => {
    setHighlightedPoints((prev) => ({
      ...prev,
      analysisMoves: highlightedMoves.map((move) => ({
        from: move.from,
        to: move.to,
      })),
    }))
  }, [highlightedMoves])

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

      // Check bear-off (align with visual bear-off box position)
      const bearoffStartX = CONFIG.barX! + CONFIG.barWidth + 6 * CONFIG.pointWidth
      if (x >= bearoffStartX) {
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

      // Clear highlights (keep sources)
      setHighlightedPoints((prev) => ({
        ...prev,
        source: null,
        destinations: [],
        captures: [],
      }))

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
          // Use direct move for analysis mode with free movement enabled
          if (gameState?.isAnalysisMode && isFreeMoveEnabled) {
            await invoke(HubMethods.MoveCheckerDirectly, sourcePoint, targetPoint)
          } else {
            await invoke(HubMethods.MakeMove, sourcePoint, targetPoint)
          }
        } catch (error) {
          console.error('Failed to execute move:', error)
        }
      }
    },
    [getPointAtPosition, invoke, handleDragMove, gameState, isFreeMoveEnabled]
  )

  const startDrag = useCallback(
    (
      _event: MouseEvent | TouchEvent,
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
      setHighlightedPoints((prev) => ({
        ...prev,
        source: pointNum,
        destinations,
        captures,
      }))

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
    diceGroupRef.current = diceGroup

    // Buttons group
    const buttonsGroup = createSVGElement('g', { id: 'buttons' }) as SVGGElement
    svgRef.current.appendChild(buttonsGroup)
    buttonsGroupRef.current = buttonsGroup
    // eslint-disable-next-line react-hooks/exhaustive-deps
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

    // Render bar checkers
    const renderBarCheckers = (
      count: number,
      color: CheckerColor,
      startY: number
    ) => {
      if (count === 0) return

      const barCenterX = CONFIG.barX! + CONFIG.barWidth / 2
      const maxVisible = Math.min(count, 5)

      for (let i = 0; i < maxVisible; i++) {
        const cy = startY + CONFIG.checkerSpacing * i
        const isTopChecker = i === maxVisible - 1
        const isSelected = selectedChecker?.point === 0 && isTopChecker
        const isDraggable = isTopChecker && validSources.includes(0)

        const checkerColor =
          color === CheckerColor.White ? COLORS.checkerWhite : COLORS.checkerRed
        const strokeColor =
          color === CheckerColor.White
            ? COLORS.checkerWhiteStroke
            : COLORS.checkerRedStroke

        const circle = createSVGElement('circle', {
          cx: barCenterX,
          cy,
          r: CONFIG.checkerRadius,
          fill: checkerColor,
          stroke: strokeColor,
          'stroke-width': 2,
          class: `checker ${isDraggable ? 'draggable' : ''}`,
          'data-point': 0,
          'data-color': color,
        }) as SVGCircleElement

        if (isSelected) {
          circle.setAttribute('stroke', COLORS.highlightSelected)
          circle.setAttribute('stroke-width', '4')
        }

        if (isDraggable) {
          circle.addEventListener('mousedown', (e: MouseEvent) => {
            e.preventDefault()
            startDrag(e, circle, e.clientX, e.clientY)
          })

          circle.addEventListener('touchstart', (e: TouchEvent) => {
            e.preventDefault()
            const touch = e.touches[0]
            startDrag(e, circle, touch.clientX, touch.clientY)
          })
        }

        checkersGroupRef.current!.appendChild(circle)
      }

      // Show count if more than 5
      if (count > 5) {
        const textColor =
          color === CheckerColor.White ? COLORS.textDark : COLORS.textLight

        const text = createSVGElement('text', {
          x: barCenterX,
          y: startY + CONFIG.checkerSpacing * 4,
          'text-anchor': 'middle',
          'dominant-baseline': 'middle',
          fill: textColor,
          'font-size': 16,
          'font-weight': 'bold',
        })
        text.textContent = String(count)
        checkersGroupRef.current!.appendChild(text)
      }
    }

    // Render white bar checkers (from top)
    if (gameState.whiteCheckersOnBar > 0) {
      renderBarCheckers(
        gameState.whiteCheckersOnBar,
        CheckerColor.White,
        CONFIG.padding + CONFIG.checkerSpacing
      )
    }

    // Render red bar checkers (from bottom)
    if (gameState.redCheckersOnBar > 0) {
      renderBarCheckers(
        gameState.redCheckersOnBar,
        CheckerColor.Red,
        CONFIG.viewBox.height - CONFIG.padding - CONFIG.checkerSpacing
      )
    }

    // Render borne-off checkers in the bear-off area
    const renderBornOffCheckers = (
      count: number,
      color: CheckerColor,
      startY: number
    ) => {
      if (count === 0) return

      const bearoffCenterX = CONFIG.viewBox.width - CONFIG.bearoffWidth / 2
      const maxVisible = Math.min(count, 5)

      for (let i = 0; i < maxVisible; i++) {
        // White checkers stack upward (subtract), Red checkers stack downward (add)
        const cy = color === CheckerColor.White
          ? startY - CONFIG.checkerSpacing * i
          : startY + CONFIG.checkerSpacing * i
        
        const checkerColor =
          color === CheckerColor.White ? COLORS.checkerWhite : COLORS.checkerRed
        const strokeColor =
          color === CheckerColor.White
            ? COLORS.checkerWhiteStroke
            : COLORS.checkerRedStroke

        const circle = createSVGElement('circle', {
          cx: bearoffCenterX,
          cy,
          r: CONFIG.checkerRadius,
          fill: checkerColor,
          stroke: strokeColor,
          'stroke-width': 2,
          class: 'checker born-off',
        }) as SVGCircleElement

        checkersGroupRef.current!.appendChild(circle)
      }

      // Show count if more than 5
      if (count > 5) {
        const textColor =
          color === CheckerColor.White ? COLORS.textDark : COLORS.textLight

        // Position text on the 5th checker (index 4)
        const textY = color === CheckerColor.White
          ? startY - CONFIG.checkerSpacing * 4
          : startY + CONFIG.checkerSpacing * 4

        const text = createSVGElement('text', {
          x: bearoffCenterX,
          y: textY,
          'text-anchor': 'middle',
          'dominant-baseline': 'middle',
          fill: textColor,
          'font-size': 16,
          'font-weight': 'bold',
        })
        text.textContent = String(count)
        checkersGroupRef.current!.appendChild(text)
      }
    }

    // Red borne-off checkers (from top - points 19-24)
    if (gameState.redBornOff > 0) {
      renderBornOffCheckers(
        gameState.redBornOff,
        CheckerColor.Red,
        CONFIG.padding + CONFIG.checkerSpacing
      )
    }

    // White borne-off checkers (from bottom - points 1-6)
    if (gameState.whiteBornOff > 0) {
      renderBornOffCheckers(
        gameState.whiteBornOff,
        CheckerColor.White,
        CONFIG.viewBox.height - CONFIG.padding - CONFIG.checkerSpacing
      )
    }
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [gameState, selectedChecker, validSources, startDrag])

  // Render dice
  const renderDice = useCallback(() => {
    if (!diceGroupRef.current || !gameState) return

    diceGroupRef.current.innerHTML = ''

    const barCenterX = CONFIG.barX! + CONFIG.barWidth / 2
    const centerY = CONFIG.viewBox.height / 2
    const diceSize = 36
    const diceGap = 6

    // Counter-rotate if board is flipped
    if (isBoardFlipped) {
      diceGroupRef.current.setAttribute('transform', `rotate(180 ${barCenterX} ${centerY})`)
    } else {
      diceGroupRef.current.removeAttribute('transform')
    }

    // Only render dice if they're rolled
    if (gameState.dice && gameState.dice.length > 0 && gameState.dice.some(d => d > 0)) {
      const totalHeight = gameState.dice.length * diceSize + (gameState.dice.length - 1) * diceGap
      const startY = centerY - totalHeight / 2

      gameState.dice.forEach((die, index) => {
        const y = startY + index * (diceSize + diceGap)

        // Dice background
        const rect = createSVGElement('rect', {
          x: barCenterX - diceSize / 2,
          y,
          width: diceSize,
          height: diceSize,
          rx: 4,
          fill: 'white',
          stroke: 'none',
          filter: 'drop-shadow(0 4px 6px rgba(0,0,0,0.3))',
        })
        diceGroupRef.current!.appendChild(rect)

        // Dice value text
        const text = createSVGElement('text', {
          x: barCenterX,
          y: y + diceSize / 2,
          'text-anchor': 'middle',
          'dominant-baseline': 'middle',
          fill: 'hsl(0 0% 9%)',
          'font-size': 20,
          'font-weight': 'bold',
        })
        text.textContent = String(die)
        diceGroupRef.current!.appendChild(text)
      })

      // "X moves left" text
      // Only show move status if game is not completed
      if (gameState.status !== GameStatus.Completed && gameState.remainingMoves && gameState.remainingMoves.length > 0) {
        const text = createSVGElement('text', {
          x: barCenterX,
          y: startY + totalHeight + 12,
          'text-anchor': 'middle',
          'dominant-baseline': 'hanging',
          fill: 'white',
          'font-size': 10,
          'font-weight': 'bold',
        })

        // Show "No valid moves" if player has dice but can't move
        if (!gameState.hasValidMoves) {
          text.textContent = 'No valid moves'
        } else {
          text.textContent = `${gameState.remainingMoves.length} move${gameState.remainingMoves.length !== 1 ? 's' : ''} left`
        }

        diceGroupRef.current!.appendChild(text)
      }
    }
  }, [gameState, isBoardFlipped])

  // Render buttons (Roll, Undo, End)
  const renderButtons = useCallback(() => {
    if (!buttonsGroupRef.current || !gameState) return

    buttonsGroupRef.current.innerHTML = ''

    const { isCustomDiceEnabled } = useGameStore.getState()

    // Counter-rotate if board is flipped
    const centerX = CONFIG.viewBox.width / 2
    const centerY = CONFIG.viewBox.height / 2
    if (isBoardFlipped) {
      buttonsGroupRef.current.setAttribute('transform', `rotate(180 ${centerX} ${centerY})`)
    } else {
      buttonsGroupRef.current.removeAttribute('transform')
    }

    const isGameInProgress = gameState.status === GameStatus.InProgress
    const isYourTurn = gameState.isYourTurn
    const hasDiceRolled =
      gameState.dice && gameState.dice.length > 0 && gameState.dice.some((d) => d > 0)

    // Opening roll logic
    const isOpeningRoll = gameState.isOpeningRoll || false
    const yourColor = gameState.yourColor
    const youHaveRolled = isOpeningRoll && (
      (yourColor === CheckerColor.White && gameState.whiteOpeningRoll != null) ||
      (yourColor === CheckerColor.Red && gameState.redOpeningRoll != null)
    )

    // Button visibility logic (from BoardOverlayControls) - hide all buttons for spectators
    const hideRollForCustomDice = gameState.isAnalysisMode && isCustomDiceEnabled
    const canRoll = !isSpectator && !hideRollForCustomDice && isGameInProgress && (isOpeningRoll ? (!youHaveRolled || gameState.isOpeningRollTie) : (isYourTurn && !hasDiceRolled))

    const hasUsedAllMoves = gameState.remainingMoves && gameState.remainingMoves.length === 0
    const canEndTurn = !isSpectator && isGameInProgress && !isOpeningRoll && hasDiceRolled && (
      gameState.isAnalysisMode ? (hasUsedAllMoves || !gameState.hasValidMoves) : (!gameState.hasValidMoves && isYourTurn)
    )

    const isDoubles = hasDiceRolled && gameState.dice.length === 2 && gameState.dice[0] === gameState.dice[1]
    const totalMoves = hasDiceRolled ? (isDoubles ? 4 : gameState.dice.length) : 0
    const remainingMovesCount = gameState.remainingMoves?.length ?? 0
    const movesMade = totalMoves > 0 && remainingMovesCount < totalMoves
    const canUndo = !isSpectator && isGameInProgress && !isOpeningRoll && hasDiceRolled && movesMade && (gameState.isAnalysisMode || isYourTurn)

    // Button positions
    const leftSideX = CONFIG.viewBox.width * 0.2216  // 22.16%
    const rightSideX = CONFIG.viewBox.width * 0.7265  // 72.65%

    // Helper function to create a circular button
    const createButton = (
      x: number,
      y: number,
      radius: number,
      label: string,
      onClick: () => void,
      color: string = 'hsl(0 0% 98%)',  // Default white/light
      textColor: string = 'hsl(0 0% 9%)'  // Default dark text
    ) => {
      const buttonGroup = createSVGElement('g', { class: 'button-group' }) as SVGGElement
      buttonGroup.style.cursor = 'pointer'

      // Button circle background
      const circle = createSVGElement('circle', {
        cx: x,
        cy: y,
        r: radius,
        fill: color,
        stroke: 'hsl(0 0% 72%)',
        'stroke-width': 2,
        class: 'button-bg',
        filter: 'drop-shadow(0 4px 6px rgba(0,0,0,0.3))',
      })
      buttonGroup.appendChild(circle)

      // Button text
      const text = createSVGElement('text', {
        x: x,
        y: y,
        'text-anchor': 'middle',
        'dominant-baseline': 'middle',
        fill: textColor,
        'font-size': 16,
        'font-weight': 'bold',
        'pointer-events': 'none',
      })
      text.textContent = label
      buttonGroup.appendChild(text)

      // Add hover effect
      buttonGroup.addEventListener('mouseenter', () => {
        circle.setAttribute('stroke-width', '3')
        circle.setAttribute('r', String(radius + 2))
      })
      buttonGroup.addEventListener('mouseleave', () => {
        circle.setAttribute('stroke-width', '2')
        circle.setAttribute('r', String(radius))
      })

      // Add click handler
      buttonGroup.addEventListener('click', async (e) => {
        e.preventDefault()
        try {
          onClick()
        } catch (error) {
          console.error('Button click error:', error)
        }
      })

      return buttonGroup
    }

    // Render Roll button
    if (canRoll) {
      const rollButton = createButton(
        rightSideX,
        centerY,
        40,  // radius
        'Roll',
        async () => {
          await invoke(HubMethods.RollDice)
        }
      )
      buttonsGroupRef.current.appendChild(rollButton)
    }

    // Render Undo button
    if (canUndo) {
      const undoButton = createButton(
        leftSideX,
        centerY,
        32,  // smaller radius
        'Undo',
        async () => {
          await invoke(HubMethods.UndoLastMove)
        },
        'hsl(0 0% 98%)',  // light background
        'hsl(0 0% 9%)'  // dark text
      )
      buttonsGroupRef.current.appendChild(undoButton)
    }

    // Render End Turn button
    if (canEndTurn) {
      const endButton = createButton(
        rightSideX,
        centerY,
        40,  // radius
        'End',
        async () => {
          await invoke(HubMethods.EndTurn)
        },
        'hsl(142.1 76.2% 36.3%)',  // green background
        'hsl(0 0% 98%)'  // light text
      )
      buttonsGroupRef.current.appendChild(endButton)
    }
  }, [gameState, invoke, isBoardFlipped, isSpectator])

  // Initialize board
  useEffect(() => {
    renderBoard()
  }, [renderBoard])

  // Re-render checkers when game state changes
  useEffect(() => {
    renderCheckers()
  }, [renderCheckers])

  // Re-render dice when game state changes
  useEffect(() => {
    renderDice()
  }, [renderDice])

  // Re-render buttons when game state changes
  useEffect(() => {
    renderButtons()
  }, [renderButtons])

  return (
    <svg
      ref={svgRef}
      viewBox={`0 0 ${CONFIG.viewBox.width} ${CONFIG.viewBox.height}`}
      className={`board-svg w-full h-auto ${isBoardFlipped ? 'rotate-180' : ''}`}
      style={{ transition: 'transform 0.6s' }}
    />
  )
}

// Custom comparison function to prevent re-renders when only timer values change
const arePropsEqual = (prevProps: BoardSVGProps, nextProps: BoardSVGProps): boolean => {
  const prev = prevProps.gameState
  const next = nextProps.gameState

  // If both are null, they're equal
  if (prev === null && next === null) return true

  // If one is null and the other isn't, they're different
  if (prev === null || next === null) return false

  // Compare all fields EXCEPT timer-related ones
  // Timer fields that should be IGNORED (don't cause re-render):
  // - whiteReserveSeconds, redReserveSeconds (change every second)
  // - whiteDelayRemaining, redDelayRemaining (change every second)
  // - whiteIsInDelay, redIsInDelay (can change during turn)

  return (
    prev.gameId === next.gameId &&
    prev.currentPlayer === next.currentPlayer &&
    prev.isYourTurn === next.isYourTurn &&
    JSON.stringify(prev.dice) === JSON.stringify(next.dice) &&
    JSON.stringify(prev.remainingMoves) === JSON.stringify(next.remainingMoves) &&
    JSON.stringify(prev.validMoves) === JSON.stringify(next.validMoves) &&
    JSON.stringify(prev.board) === JSON.stringify(next.board) &&
    prev.whiteCheckersOnBar === next.whiteCheckersOnBar &&
    prev.redCheckersOnBar === next.redCheckersOnBar &&
    prev.whiteBornOff === next.whiteBornOff &&
    prev.redBornOff === next.redBornOff &&
    prev.doublingCubeValue === next.doublingCubeValue &&
    prev.doublingCubeOwner === next.doublingCubeOwner &&
    prev.winner === next.winner &&
    prev.isOpeningRoll === next.isOpeningRoll &&
    prev.whiteOpeningRoll === next.whiteOpeningRoll &&
    prev.redOpeningRoll === next.redOpeningRoll &&
    prev.isOpeningRollTie === next.isOpeningRollTie
    // Deliberately NOT comparing timer fields:
    // - timeControlType, delaySeconds (config, shouldn't change mid-game)
    // - whiteReserveSeconds, redReserveSeconds (changes every second!)
    // - whiteIsInDelay, redIsInDelay (changes during turn)
    // - whiteDelayRemaining, redDelayRemaining (changes every second!)
  )
}

// Export memoized version to prevent re-renders during drag
export const BoardSVG = React.memo(BoardSVGComponent, arePropsEqual)
