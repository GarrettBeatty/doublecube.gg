import { createContext, useContext, useState, useCallback, useMemo } from 'react'
import { getPointAtPosition, clientToSVGCoords } from '@/lib/boardConstants'
import {
  BoardPosition,
  BoardDisplayOptions,
  PointHighlight,
  HighlightType,
  DragState,
  InteractionCallbacks,
  BoardContextValue,
} from './board.types'

const BoardContext = createContext<BoardContextValue | null>(null)

interface BoardProviderProps {
  children: React.ReactNode
  position: BoardPosition
  display: Required<BoardDisplayOptions>
  highlights: PointHighlight[]
  interaction?: InteractionCallbacks
  svgRef: React.RefObject<SVGSVGElement>
}

export function BoardProvider({
  children,
  position,
  display,
  highlights,
  interaction,
  svgRef,
}: BoardProviderProps) {
  const [dragState, setDragState] = useState<DragState>({
    isDragging: false,
    sourcePoint: null,
    ghostPosition: null,
    ghostColor: null,
  })

  // Create highlight lookup
  const highlightMap = useMemo(() => {
    const map = new Map<number, HighlightType>()
    for (const h of highlights) {
      map.set(h.point, h.type)
    }
    return map
  }, [highlights])

  const isHighlighted = useCallback(
    (point: number): HighlightType | null => {
      return highlightMap.get(point) || null
    },
    [highlightMap]
  )

  const isDraggable = useCallback(
    (point: number): boolean => {
      if (display.interactionMode === 'none') return false
      if (display.interactionMode === 'click') return false
      return interaction?.isDraggable?.(point) ?? false
    },
    [display.interactionMode, interaction]
  )

  const startDrag = useCallback(
    (
      e: React.MouseEvent | React.TouchEvent,
      point: number,
      color: 'white' | 'red'
    ) => {
      if (!svgRef.current) return
      if (display.interactionMode === 'none' || display.interactionMode === 'click') return

      e.preventDefault()

      // Get initial position
      const clientX = 'touches' in e ? e.touches[0].clientX : e.clientX
      const clientY = 'touches' in e ? e.touches[0].clientY : e.clientY
      const svgCoords = clientToSVGCoords(svgRef.current, clientX, clientY)

      if (!svgCoords) return

      setDragState({
        isDragging: true,
        sourcePoint: point,
        ghostPosition: svgCoords,
        ghostColor: color,
      })

      // Set up move and end handlers
      const handleMove = (moveEvent: MouseEvent | TouchEvent) => {
        if (!svgRef.current) return
        const moveClientX = 'touches' in moveEvent ? moveEvent.touches[0].clientX : moveEvent.clientX
        const moveClientY = 'touches' in moveEvent ? moveEvent.touches[0].clientY : moveEvent.clientY
        const coords = clientToSVGCoords(svgRef.current, moveClientX, moveClientY)

        if (coords) {
          setDragState((prev) => ({
            ...prev,
            ghostPosition: coords,
          }))
        }
      }

      const handleEnd = (endEvent: MouseEvent | TouchEvent) => {
        if (!svgRef.current) return

        const endClientX = 'changedTouches' in endEvent
          ? endEvent.changedTouches[0].clientX
          : endEvent.clientX
        const endClientY = 'changedTouches' in endEvent
          ? endEvent.changedTouches[0].clientY
          : endEvent.clientY

        const targetPoint = getPointAtPosition(
          svgRef.current,
          endClientX,
          endClientY,
          display.isFlipped
        )

        // Complete the move if valid destination
        if (targetPoint !== null && interaction?.onMoveAttempt) {
          interaction.onMoveAttempt(point, targetPoint)
        }

        // Reset drag state
        setDragState({
          isDragging: false,
          sourcePoint: null,
          ghostPosition: null,
          ghostColor: null,
        })

        // Remove listeners
        document.removeEventListener('mousemove', handleMove)
        document.removeEventListener('mouseup', handleEnd)
        document.removeEventListener('touchmove', handleMove)
        document.removeEventListener('touchend', handleEnd)
      }

      // Add listeners
      document.addEventListener('mousemove', handleMove)
      document.addEventListener('mouseup', handleEnd)
      document.addEventListener('touchmove', handleMove)
      document.addEventListener('touchend', handleEnd)
    },
    [svgRef, display.interactionMode, display.isFlipped, interaction]
  )

  const contextValue = useMemo<BoardContextValue>(
    () => ({
      position,
      display,
      highlights,
      dragState,
      startDrag,
      isHighlighted,
      isDraggable,
    }),
    [position, display, highlights, dragState, startDrag, isHighlighted, isDraggable]
  )

  return (
    <BoardContext.Provider value={contextValue}>
      {children}
    </BoardContext.Provider>
  )
}

// eslint-disable-next-line react-refresh/only-export-components
export function useBoardContext() {
  const context = useContext(BoardContext)
  if (!context) {
    throw new Error('useBoardContext must be used within a BoardProvider')
  }
  return context
}
