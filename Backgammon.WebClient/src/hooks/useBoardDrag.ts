import { useRef, useCallback, useState, RefObject } from 'react'
import {
  BOARD_CONFIG,
  BOARD_COLORS,
  getPointAtPosition,
  clientToSVGCoords,
} from '@/lib/boardConstants'

export interface DragState {
  isDragging: boolean
  sourcePoint: number | null
  ghostPosition: { x: number; y: number } | null
  ghostColor: 'white' | 'red' | null
}

export interface UseBoardDragOptions {
  svgRef: RefObject<SVGSVGElement | null>
  isBoardFlipped?: boolean
  onMoveComplete: (from: number, to: number) => void
  getValidDestinations: (from: number) => number[]
}

export interface UseBoardDragReturn {
  dragState: DragState
  validDestinations: number[]
  startDrag: (
    event: React.MouseEvent | React.TouchEvent,
    pointNum: number,
    color: 'white' | 'red'
  ) => void
  // For rendering ghost checker
  ghostChecker: {
    visible: boolean
    x: number
    y: number
    color: 'white' | 'red'
  } | null
}

const initialDragState: DragState = {
  isDragging: false,
  sourcePoint: null,
  ghostPosition: null,
  ghostColor: null,
}

export function useBoardDrag({
  svgRef,
  isBoardFlipped = false,
  onMoveComplete,
  getValidDestinations,
}: UseBoardDragOptions): UseBoardDragReturn {
  const [dragState, setDragState] = useState<DragState>(initialDragState)
  const [validDestinations, setValidDestinations] = useState<number[]>([])

  // Use ref for drag state to avoid stale closures in event handlers
  const dragStateRef = useRef<DragState>(initialDragState)

  const handleDragMove = useCallback(
    (event: MouseEvent | TouchEvent) => {
      if (!dragStateRef.current.isDragging || !svgRef.current) return

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

      const svgCoords = clientToSVGCoords(svgRef.current, clientX, clientY)
      if (!svgCoords) return

      // Update ghost position
      dragStateRef.current = {
        ...dragStateRef.current,
        ghostPosition: svgCoords,
      }

      setDragState((prev) => ({
        ...prev,
        ghostPosition: svgCoords,
      }))
    },
    [svgRef]
  )

  const handleDragEnd = useCallback(
    (event: MouseEvent | TouchEvent) => {
      if (!dragStateRef.current.isDragging || !svgRef.current) return

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
      const targetPoint = getPointAtPosition(
        svgRef.current,
        clientX,
        clientY,
        isBoardFlipped
      )

      const sourcePoint = dragStateRef.current.sourcePoint

      // Remove global listeners
      document.removeEventListener('mousemove', handleDragMove)
      document.removeEventListener('mouseup', handleDragEnd)
      document.removeEventListener('touchmove', handleDragMove)
      document.removeEventListener('touchend', handleDragEnd)

      // Reset drag state
      dragStateRef.current = initialDragState
      setDragState(initialDragState)
      setValidDestinations([])

      // Execute move if valid target
      if (
        targetPoint !== null &&
        targetPoint !== sourcePoint &&
        sourcePoint !== null
      ) {
        onMoveComplete(sourcePoint, targetPoint)
      }
    },
    [svgRef, isBoardFlipped, onMoveComplete, handleDragMove]
  )

  const startDrag = useCallback(
    (
      event: React.MouseEvent | React.TouchEvent,
      pointNum: number,
      color: 'white' | 'red'
    ) => {
      if (!svgRef.current) return

      event.preventDefault()

      let clientX: number, clientY: number
      if ('touches' in event) {
        const touch = event.touches[0]
        clientX = touch.clientX
        clientY = touch.clientY
      } else {
        clientX = event.clientX
        clientY = event.clientY
      }

      const svgCoords = clientToSVGCoords(svgRef.current, clientX, clientY)
      if (!svgCoords) return

      // Get valid destinations for highlighting
      const destinations = getValidDestinations(pointNum)

      // Update state
      const newDragState: DragState = {
        isDragging: true,
        sourcePoint: pointNum,
        ghostPosition: svgCoords,
        ghostColor: color,
      }

      dragStateRef.current = newDragState
      setDragState(newDragState)
      setValidDestinations(destinations)

      // Add global listeners
      document.addEventListener('mousemove', handleDragMove)
      document.addEventListener('mouseup', handleDragEnd)
      document.addEventListener('touchmove', handleDragMove, { passive: false })
      document.addEventListener('touchend', handleDragEnd)
    },
    [svgRef, getValidDestinations, handleDragMove, handleDragEnd]
  )

  // Compute ghost checker for rendering
  const ghostChecker =
    dragState.isDragging && dragState.ghostPosition && dragState.ghostColor
      ? {
          visible: true,
          x: dragState.ghostPosition.x,
          y: dragState.ghostPosition.y,
          color: dragState.ghostColor,
        }
      : null

  return {
    dragState,
    validDestinations,
    startDrag,
    ghostChecker,
  }
}

// Re-export constants for convenience
export { BOARD_CONFIG, BOARD_COLORS }
