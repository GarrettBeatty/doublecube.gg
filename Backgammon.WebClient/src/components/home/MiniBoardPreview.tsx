import { MiniPoint } from '@/types/home.types'

// Match the real board colors from BoardSVG.tsx
const COLORS = {
  boardBackground: 'hsl(0 0% 14%)',
  boardBorder: 'hsl(0 0% 22%)',
  pointLight: 'hsl(0 0% 32%)',
  pointDark: 'hsl(0 0% 20%)',
  bar: 'hsl(0 0% 11%)',
  checkerWhite: 'hsl(0 0% 98%)',
  checkerWhiteStroke: 'hsl(0 0% 72%)',
  checkerRed: 'hsl(0 84.2% 60.2%)',
  checkerRedStroke: 'hsl(0 72.2% 50.6%)',
}

interface MiniBoardPreviewProps {
  board?: MiniPoint[]
  whiteOnBar?: number
  redOnBar?: number
  whiteBornOff?: number
  redBornOff?: number
  dice?: number[]
  cubeValue?: number
  cubeOwner?: 'White' | 'Red' | 'Center'
  size?: number
}

export function MiniBoardPreview({
  board,
  whiteOnBar = 0,
  redOnBar = 0,
  whiteBornOff = 0,
  redBornOff = 0,
  dice,
  cubeValue = 1,
  cubeOwner = 'Center',
  size = 160,
}: MiniBoardPreviewProps) {
  const aspectRatio = 1.3
  const width = size
  const height = size / aspectRatio

  // Board dimensions
  const boardPadding = 2
  const barWidth = width * 0.06
  const bearoffWidth = width * 0.06
  const playableWidth = width - barWidth - bearoffWidth * 2 - boardPadding * 2
  const pointWidth = playableWidth / 12
  const pointHeight = height * 0.42
  const checkerRadius = pointWidth * 0.42

  // Get checker count at a point
  const getPointData = (position: number) => {
    if (!board) return { color: null, count: 0 }
    const point = board.find(p => p.position === position)
    return point || { color: null, count: 0 }
  }

  // Calculate point X position
  const getPointX = (position: number) => {
    // Points 13-24 are on top (left to right: 13-18, bar, 19-24)
    // Points 1-12 are on bottom (right to left: 12-7, bar, 6-1)
    if (position >= 13 && position <= 24) {
      const idx = 24 - position // 0-11 from left
      if (idx < 6) {
        // Left side (points 24-19)
        return boardPadding + bearoffWidth + idx * pointWidth
      } else {
        // Right side (points 18-13)
        return boardPadding + bearoffWidth + barWidth + idx * pointWidth
      }
    } else {
      // Bottom row (1-12)
      const idx = position - 1 // 0-11
      if (idx < 6) {
        // Right side (points 1-6)
        return width - boardPadding - bearoffWidth - (idx + 1) * pointWidth
      } else {
        // Left side (points 7-12)
        return width - boardPadding - bearoffWidth - barWidth - (idx + 1) * pointWidth
      }
    }
  }

  // Render a triangular point
  const renderPoint = (position: number, isTop: boolean) => {
    const x = getPointX(position)
    const y = isTop ? boardPadding : height - boardPadding
    const tipY = isTop ? boardPadding + pointHeight : height - boardPadding - pointHeight
    const isDark = position % 2 === 0

    return (
      <polygon
        key={`point-${position}`}
        points={`${x},${y} ${x + pointWidth},${y} ${x + pointWidth / 2},${tipY}`}
        fill={isDark ? COLORS.pointDark : COLORS.pointLight}
      />
    )
  }

  // Render checkers on a point
  const renderCheckers = (position: number, isTop: boolean) => {
    const { color, count } = getPointData(position)
    if (!color || count === 0) return null

    const x = getPointX(position) + pointWidth / 2
    const baseY = isTop ? boardPadding + checkerRadius + 1 : height - boardPadding - checkerRadius - 1
    const spacing = checkerRadius * 1.7
    const direction = isTop ? 1 : -1
    const displayCount = Math.min(count, 5)

    const checkerColor = color === 'White' ? COLORS.checkerWhite : COLORS.checkerRed
    const strokeColor = color === 'White' ? COLORS.checkerWhiteStroke : COLORS.checkerRedStroke

    return (
      <g key={`checkers-${position}`}>
        {Array.from({ length: displayCount }).map((_, i) => (
          <circle
            key={i}
            cx={x}
            cy={baseY + i * spacing * direction}
            r={checkerRadius}
            fill={checkerColor}
            stroke={strokeColor}
            strokeWidth={0.5}
          />
        ))}
        {count > 5 && (
          <text
            x={x}
            y={baseY + 2 * spacing * direction}
            textAnchor="middle"
            dominantBaseline="middle"
            fontSize={checkerRadius * 0.9}
            fill={color === 'White' ? '#333' : '#fff'}
            fontWeight="bold"
          >
            {count}
          </text>
        )}
      </g>
    )
  }

  // Render bar checkers
  const renderBarCheckers = () => {
    const barX = boardPadding + bearoffWidth + 6 * pointWidth + barWidth / 2
    const elements = []

    if (whiteOnBar > 0) {
      const displayCount = Math.min(whiteOnBar, 2)
      for (let i = 0; i < displayCount; i++) {
        elements.push(
          <circle
            key={`white-bar-${i}`}
            cx={barX}
            cy={height * 0.3 + i * checkerRadius * 1.8}
            r={checkerRadius * 0.85}
            fill={COLORS.checkerWhite}
            stroke={COLORS.checkerWhiteStroke}
            strokeWidth={0.5}
          />
        )
      }
    }

    if (redOnBar > 0) {
      const displayCount = Math.min(redOnBar, 2)
      for (let i = 0; i < displayCount; i++) {
        elements.push(
          <circle
            key={`red-bar-${i}`}
            cx={barX}
            cy={height * 0.7 - i * checkerRadius * 1.8}
            r={checkerRadius * 0.85}
            fill={COLORS.checkerRed}
            stroke={COLORS.checkerRedStroke}
            strokeWidth={0.5}
          />
        )
      }
    }

    return elements
  }

  // Render born off indicators
  const renderBornOff = () => {
    const elements = []
    const rightX = width - boardPadding - bearoffWidth / 2

    if (whiteBornOff > 0) {
      elements.push(
        <g key="white-off">
          <circle
            cx={rightX}
            cy={height * 0.8}
            r={checkerRadius * 0.7}
            fill={COLORS.checkerWhite}
            stroke={COLORS.checkerWhiteStroke}
            strokeWidth={0.5}
          />
          {whiteBornOff > 1 && (
            <text
              x={rightX}
              y={height * 0.8}
              textAnchor="middle"
              dominantBaseline="middle"
              fontSize={7}
              fill="#333"
              fontWeight="bold"
            >
              {whiteBornOff}
            </text>
          )}
        </g>
      )
    }

    if (redBornOff > 0) {
      elements.push(
        <g key="red-off">
          <circle
            cx={rightX}
            cy={height * 0.2}
            r={checkerRadius * 0.7}
            fill={COLORS.checkerRed}
            stroke={COLORS.checkerRedStroke}
            strokeWidth={0.5}
          />
          {redBornOff > 1 && (
            <text
              x={rightX}
              y={height * 0.2}
              textAnchor="middle"
              dominantBaseline="middle"
              fontSize={7}
              fill="#fff"
              fontWeight="bold"
            >
              {redBornOff}
            </text>
          )}
        </g>
      )
    }

    return elements
  }

  // Render dice
  const renderDice = () => {
    if (!dice || dice.length !== 2) return null

    const dieSize = Math.min(width * 0.08, 20)
    const centerX = width / 2
    const centerY = height / 2
    const gap = dieSize * 0.4

    return (
      <g>
        {dice.map((value, i) => {
          const x = centerX + (i === 0 ? -dieSize - gap / 2 : gap / 2)
          const y = centerY - dieSize / 2
          return (
            <g key={`die-${i}`}>
              <rect
                x={x}
                y={y}
                width={dieSize}
                height={dieSize}
                rx={dieSize * 0.15}
                fill="#fff"
                stroke="#999"
                strokeWidth={0.5}
              />
              <text
                x={x + dieSize / 2}
                y={y + dieSize / 2}
                textAnchor="middle"
                dominantBaseline="middle"
                fontSize={dieSize * 0.6}
                fontWeight="bold"
                fill="#333"
              >
                {value}
              </text>
            </g>
          )
        })}
      </g>
    )
  }

  // Render doubling cube
  const renderCube = () => {
    if (cubeValue <= 1) return null

    const cubeSize = Math.min(width * 0.07, 16)
    const leftX = boardPadding + bearoffWidth / 2

    // Position based on owner: Center = middle, White = bottom, Red = top
    let cubeY: number
    if (cubeOwner === 'White') {
      cubeY = height - boardPadding - cubeSize - 2
    } else if (cubeOwner === 'Red') {
      cubeY = boardPadding + 2
    } else {
      cubeY = height / 2 - cubeSize / 2
    }

    return (
      <g>
        <rect
          x={leftX - cubeSize / 2}
          y={cubeY}
          width={cubeSize}
          height={cubeSize}
          rx={cubeSize * 0.1}
          fill="#222"
          stroke="#444"
          strokeWidth={0.5}
        />
        <text
          x={leftX}
          y={cubeY + cubeSize / 2}
          textAnchor="middle"
          dominantBaseline="middle"
          fontSize={cubeSize * 0.55}
          fontWeight="bold"
          fill="#fff"
        >
          {cubeValue}
        </text>
      </g>
    )
  }

  return (
    <svg
      width={width}
      height={height}
      viewBox={`0 0 ${width} ${height}`}
      className="rounded"
    >
      {/* Board background */}
      <rect
        x={0}
        y={0}
        width={width}
        height={height}
        fill={COLORS.boardBackground}
        stroke={COLORS.boardBorder}
        strokeWidth={1}
        rx={3}
      />

      {/* Left bearoff area */}
      <rect
        x={boardPadding}
        y={boardPadding}
        width={bearoffWidth}
        height={height - boardPadding * 2}
        fill={COLORS.bar}
        rx={2}
      />

      {/* Bar */}
      <rect
        x={boardPadding + bearoffWidth + 6 * pointWidth}
        y={0}
        width={barWidth}
        height={height}
        fill={COLORS.bar}
      />

      {/* Right bearoff area */}
      <rect
        x={width - boardPadding - bearoffWidth}
        y={boardPadding}
        width={bearoffWidth}
        height={height - boardPadding * 2}
        fill={COLORS.bar}
        rx={2}
      />

      {/* Points - Top row (13-24) */}
      {[24, 23, 22, 21, 20, 19, 18, 17, 16, 15, 14, 13].map(pos =>
        renderPoint(pos, true)
      )}

      {/* Points - Bottom row (1-12) */}
      {[1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12].map(pos =>
        renderPoint(pos, false)
      )}

      {/* Checkers - Top row */}
      {[24, 23, 22, 21, 20, 19, 18, 17, 16, 15, 14, 13].map(pos =>
        renderCheckers(pos, true)
      )}

      {/* Checkers - Bottom row */}
      {[1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12].map(pos =>
        renderCheckers(pos, false)
      )}

      {/* Bar checkers */}
      {renderBarCheckers()}

      {/* Born off indicators */}
      {renderBornOff()}

      {/* Dice */}
      {renderDice()}

      {/* Doubling cube */}
      {renderCube()}
    </svg>
  )
}
