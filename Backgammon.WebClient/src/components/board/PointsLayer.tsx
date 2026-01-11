import { memo } from 'react'
import { Point } from './Point'
import { PointState, PointHighlight, HighlightType } from './board.types'

interface PointsLayerProps {
  points: PointState[]
  highlights: PointHighlight[]
  showNumbers?: boolean
  isDraggable?: (point: number) => boolean
  onPointClick?: (point: number) => void
  onCheckerDragStart?: (
    e: React.MouseEvent | React.TouchEvent,
    point: number,
    color: 'white' | 'red'
  ) => void
}

export const PointsLayer = memo(function PointsLayer({
  points,
  highlights,
  showNumbers = false,
  isDraggable,
  onPointClick,
  onCheckerDragStart,
}: PointsLayerProps) {
  // Create a lookup for quick highlight access
  const highlightMap = new Map<number, HighlightType>()
  for (const h of highlights) {
    highlightMap.set(h.point, h.type)
  }

  // Get point data by position
  const getPointData = (position: number): PointState => {
    const point = points.find((p) => p.position === position)
    return point || { position, color: null, count: 0 }
  }

  return (
    <g id="points">
      {Array.from({ length: 24 }, (_, i) => {
        const pointNum = i + 1
        const pointData = getPointData(pointNum)
        const highlight = highlightMap.get(pointNum) || null
        const canDrag = isDraggable ? isDraggable(pointNum) : false

        return (
          <Point
            key={pointNum}
            pointNum={pointNum}
            color={pointData.color}
            count={pointData.count}
            highlight={highlight}
            showNumber={showNumbers}
            isDraggable={canDrag}
            onClick={onPointClick ? () => onPointClick(pointNum) : undefined}
            onCheckerDragStart={
              onCheckerDragStart && pointData.color
                ? (e) => onCheckerDragStart(e, pointNum, pointData.color!)
                : undefined
            }
          />
        )
      })}
    </g>
  )
})
