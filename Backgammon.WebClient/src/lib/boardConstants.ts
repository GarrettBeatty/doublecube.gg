// Shared board configuration constants used by BoardSVG and PuzzleBoard

export const BOARD_CONFIG = {
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
  get boardStartX() {
    return this.sidebarWidth + this.margin
  },
  get barX() {
    return this.boardStartX + 6 * this.pointWidth
  },
} as const

// Color palette - matching shadcn default theme (neutral grays)
// Note: Not using `as const` to allow reassignment of color values in components
export const BOARD_COLORS: Record<string, string> = {
  boardBackground: 'hsl(0 0% 14%)',
  boardBorder: 'hsl(0 0% 22%)',
  pointLight: 'hsl(0 0% 32%)',
  pointDark: 'hsl(0 0% 20%)',
  bar: 'hsl(0 0% 11%)',
  bearoff: 'hsl(0 0% 11%)',
  checkerWhite: 'hsl(0 0% 98%)',
  checkerWhiteStroke: 'hsl(0 0% 72%)',
  checkerRed: 'hsl(0 84.2% 60.2%)',
  checkerRedStroke: 'hsl(0 72.2% 50.6%)',
  highlightSource: 'hsla(47.9 95.8% 53.1% / 0.6)',
  highlightSelected: 'hsla(142.1 76.2% 36.3% / 0.7)',
  highlightDest: 'hsla(221.2 83.2% 53.3% / 0.6)',
  highlightCapture: 'hsla(0 84.2% 60.2% / 0.6)',
  highlightAnalysis: 'hsla(142.1 76.2% 36.3% / 0.5)',
  textLight: 'hsla(0 0% 98% / 0.5)',
  textDark: 'hsla(0 0% 9% / 0.7)',
}

// Pre-calculated point coordinates
export interface PointCoords {
  x: number
  y: number
  direction: number // 1 for top (points down), -1 for bottom (points up)
}

function calculatePointCoords(): Record<number, PointCoords> {
  const coords: Record<number, PointCoords> = {}
  const rightSideStart = BOARD_CONFIG.barX + BOARD_CONFIG.barWidth

  // Top row - points 13-18 (left of bar)
  for (let i = 0; i < 6; i++) {
    const pointNum = 13 + i
    coords[pointNum] = {
      x: BOARD_CONFIG.boardStartX + i * BOARD_CONFIG.pointWidth,
      y: BOARD_CONFIG.padding,
      direction: 1,
    }
  }

  // Top row - points 19-24 (right of bar)
  for (let i = 0; i < 6; i++) {
    const pointNum = 19 + i
    coords[pointNum] = {
      x: rightSideStart + i * BOARD_CONFIG.pointWidth,
      y: BOARD_CONFIG.padding,
      direction: 1,
    }
  }

  // Bottom row - points 12-7 (left of bar)
  for (let i = 0; i < 6; i++) {
    const pointNum = 12 - i
    coords[pointNum] = {
      x: BOARD_CONFIG.boardStartX + i * BOARD_CONFIG.pointWidth,
      y: BOARD_CONFIG.viewBox.height - BOARD_CONFIG.padding,
      direction: -1,
    }
  }

  // Bottom row - points 6-1 (right of bar)
  for (let i = 0; i < 6; i++) {
    const pointNum = 6 - i
    coords[pointNum] = {
      x: rightSideStart + i * BOARD_CONFIG.pointWidth,
      y: BOARD_CONFIG.viewBox.height - BOARD_CONFIG.padding,
      direction: -1,
    }
  }

  return coords
}

export const POINT_COORDS = calculatePointCoords()

/**
 * Get the point number at the given client coordinates relative to an SVG element
 */
export function getPointAtPosition(
  svgElement: SVGSVGElement,
  clientX: number,
  clientY: number,
  isBoardFlipped: boolean = false
): number | null {
  const rect = svgElement.getBoundingClientRect()

  // Calculate aspect ratios
  const renderedAspect = rect.width / rect.height
  const viewBoxAspect = BOARD_CONFIG.viewBox.width / BOARD_CONFIG.viewBox.height

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

  const scaleX = BOARD_CONFIG.viewBox.width / effectiveWidth
  const scaleY = BOARD_CONFIG.viewBox.height / effectiveHeight

  let x = (clientX - rect.left - offsetX) * scaleX
  let y = (clientY - rect.top - offsetY) * scaleY

  // Account for board flip
  if (isBoardFlipped) {
    x = BOARD_CONFIG.viewBox.width - x
    y = BOARD_CONFIG.viewBox.height - y
  }

  // Check bar
  if (x >= BOARD_CONFIG.barX && x <= BOARD_CONFIG.barX + BOARD_CONFIG.barWidth) {
    return 0 // Bar
  }

  // Check bear-off (align with visual bear-off box position)
  const bearoffStartX = BOARD_CONFIG.barX + BOARD_CONFIG.barWidth + 6 * BOARD_CONFIG.pointWidth
  if (x >= bearoffStartX) {
    return 25 // Bear-off
  }

  // Check points
  const isTop = y < BOARD_CONFIG.viewBox.height / 2
  const isLeftSide = x < BOARD_CONFIG.barX

  let pointIndex: number
  if (isLeftSide) {
    pointIndex = Math.floor((x - BOARD_CONFIG.boardStartX) / BOARD_CONFIG.pointWidth)
    if (pointIndex < 0 || pointIndex > 5) return null
  } else {
    const rightStart = BOARD_CONFIG.barX + BOARD_CONFIG.barWidth
    pointIndex = Math.floor((x - rightStart) / BOARD_CONFIG.pointWidth)
    if (pointIndex < 0 || pointIndex > 5) return null
  }

  // Map to point number
  if (isTop) {
    // Top row: 13-18 (left), 19-24 (right)
    return isLeftSide ? 13 + pointIndex : 19 + pointIndex
  } else {
    // Bottom row: 12-7 (left), 6-1 (right)
    return isLeftSide ? 12 - pointIndex : 6 - pointIndex
  }
}

/**
 * Convert client coordinates to SVG coordinates
 */
export function clientToSVGCoords(
  svgElement: SVGSVGElement,
  clientX: number,
  clientY: number
): { x: number; y: number } | null {
  const svgPoint = svgElement.createSVGPoint()
  svgPoint.x = clientX
  svgPoint.y = clientY
  const ctm = svgElement.getScreenCTM()
  if (!ctm) return null

  const svgCoords = svgPoint.matrixTransform(ctm.inverse())
  return { x: svgCoords.x, y: svgCoords.y }
}
