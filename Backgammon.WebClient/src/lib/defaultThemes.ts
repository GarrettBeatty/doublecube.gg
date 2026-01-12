import type { ThemeColors } from '@/types/theme.types'

/**
 * Default/premade themes that ship with the application.
 * These can be seeded to the database as system defaults.
 */

export const THEME_CLASSIC: ThemeColors = {
  // Current default - Modern Dark
  boardBackground: 'hsl(0 0% 14%)',
  boardBorder: 'hsl(0 0% 22%)',
  bar: 'hsl(0 0% 11%)',
  bearoff: 'hsl(0 0% 11%)',
  pointLight: 'hsl(0 0% 32%)',
  pointDark: 'hsl(0 0% 20%)',
  checkerWhite: 'hsl(0 0% 98%)',
  checkerWhiteStroke: 'hsl(0 0% 72%)',
  checkerRed: 'hsl(0 84.2% 60.2%)',
  checkerRedStroke: 'hsl(0 72.2% 50.6%)',
  diceBackground: 'white',
  diceDots: 'hsl(0 0% 9%)',
  doublingCubeBackground: '#fbbf24',
  doublingCubeStroke: '#f59e0b',
  doublingCubeText: '#111827',
  highlightSource: 'hsla(47.9 95.8% 53.1% / 0.6)',
  highlightSelected: 'hsla(142.1 76.2% 36.3% / 0.7)',
  highlightDest: 'hsla(221.2 83.2% 53.3% / 0.6)',
  highlightCapture: 'hsla(0 84.2% 60.2% / 0.6)',
  highlightAnalysis: 'hsla(142.1 76.2% 36.3% / 0.5)',
  textLight: 'hsla(0 0% 98% / 0.5)',
  textDark: 'hsla(0 0% 9% / 0.7)',
}

export const THEME_WOOD: ThemeColors = {
  // Warm wood tones
  boardBackground: 'hsl(30 30% 25%)',
  boardBorder: 'hsl(30 25% 35%)',
  bar: 'hsl(30 35% 18%)',
  bearoff: 'hsl(30 35% 18%)',
  pointLight: 'hsl(35 40% 55%)',
  pointDark: 'hsl(25 35% 35%)',
  checkerWhite: 'hsl(40 30% 90%)',
  checkerWhiteStroke: 'hsl(35 25% 70%)',
  checkerRed: 'hsl(15 70% 40%)',
  checkerRedStroke: 'hsl(15 60% 30%)',
  diceBackground: 'hsl(40 30% 92%)',
  diceDots: 'hsl(30 40% 20%)',
  doublingCubeBackground: 'hsl(35 60% 50%)',
  doublingCubeStroke: 'hsl(30 50% 40%)',
  doublingCubeText: 'hsl(30 40% 15%)',
  highlightSource: 'hsla(45 90% 50% / 0.6)',
  highlightSelected: 'hsla(120 60% 40% / 0.7)',
  highlightDest: 'hsla(200 70% 50% / 0.6)',
  highlightCapture: 'hsla(0 70% 50% / 0.6)',
  highlightAnalysis: 'hsla(120 60% 40% / 0.5)',
  textLight: 'hsla(40 30% 95% / 0.6)',
  textDark: 'hsla(30 40% 15% / 0.7)',
}

export const THEME_OCEAN: ThemeColors = {
  // Cool blue ocean tones
  boardBackground: 'hsl(210 40% 18%)',
  boardBorder: 'hsl(210 35% 28%)',
  bar: 'hsl(210 45% 12%)',
  bearoff: 'hsl(210 45% 12%)',
  pointLight: 'hsl(200 50% 45%)',
  pointDark: 'hsl(220 40% 30%)',
  checkerWhite: 'hsl(200 30% 95%)',
  checkerWhiteStroke: 'hsl(200 25% 75%)',
  checkerRed: 'hsl(15 80% 55%)',
  checkerRedStroke: 'hsl(15 70% 45%)',
  diceBackground: 'hsl(200 30% 95%)',
  diceDots: 'hsl(210 40% 15%)',
  doublingCubeBackground: 'hsl(45 90% 55%)',
  doublingCubeStroke: 'hsl(40 80% 45%)',
  doublingCubeText: 'hsl(210 40% 15%)',
  highlightSource: 'hsla(50 95% 55% / 0.6)',
  highlightSelected: 'hsla(160 70% 45% / 0.7)',
  highlightDest: 'hsla(190 80% 55% / 0.6)',
  highlightCapture: 'hsla(0 80% 55% / 0.6)',
  highlightAnalysis: 'hsla(160 70% 45% / 0.5)',
  textLight: 'hsla(200 30% 95% / 0.6)',
  textDark: 'hsla(210 40% 15% / 0.7)',
}

export const THEME_FOREST: ThemeColors = {
  // Deep green forest tones
  boardBackground: 'hsl(150 30% 15%)',
  boardBorder: 'hsl(150 25% 25%)',
  bar: 'hsl(150 35% 10%)',
  bearoff: 'hsl(150 35% 10%)',
  pointLight: 'hsl(140 35% 40%)',
  pointDark: 'hsl(160 30% 25%)',
  checkerWhite: 'hsl(80 25% 92%)',
  checkerWhiteStroke: 'hsl(80 20% 72%)',
  checkerRed: 'hsl(25 75% 45%)',
  checkerRedStroke: 'hsl(25 65% 35%)',
  diceBackground: 'hsl(80 25% 94%)',
  diceDots: 'hsl(150 35% 12%)',
  doublingCubeBackground: 'hsl(50 85% 50%)',
  doublingCubeStroke: 'hsl(45 75% 40%)',
  doublingCubeText: 'hsl(150 35% 12%)',
  highlightSource: 'hsla(55 90% 50% / 0.6)',
  highlightSelected: 'hsla(100 65% 45% / 0.7)',
  highlightDest: 'hsla(180 70% 50% / 0.6)',
  highlightCapture: 'hsla(0 75% 50% / 0.6)',
  highlightAnalysis: 'hsla(100 65% 45% / 0.5)',
  textLight: 'hsla(80 25% 95% / 0.6)',
  textDark: 'hsla(150 35% 12% / 0.7)',
}

export const THEME_HIGH_CONTRAST: ThemeColors = {
  // High contrast for accessibility
  boardBackground: 'hsl(0 0% 5%)',
  boardBorder: 'hsl(0 0% 40%)',
  bar: 'hsl(0 0% 8%)',
  bearoff: 'hsl(0 0% 8%)',
  pointLight: 'hsl(0 0% 60%)',
  pointDark: 'hsl(0 0% 25%)',
  checkerWhite: 'hsl(0 0% 100%)',
  checkerWhiteStroke: 'hsl(0 0% 70%)',
  checkerRed: 'hsl(0 100% 50%)',
  checkerRedStroke: 'hsl(0 100% 35%)',
  diceBackground: 'hsl(0 0% 100%)',
  diceDots: 'hsl(0 0% 0%)',
  doublingCubeBackground: 'hsl(60 100% 50%)',
  doublingCubeStroke: 'hsl(55 100% 40%)',
  doublingCubeText: 'hsl(0 0% 0%)',
  highlightSource: 'hsla(60 100% 50% / 0.8)',
  highlightSelected: 'hsla(120 100% 40% / 0.8)',
  highlightDest: 'hsla(200 100% 50% / 0.8)',
  highlightCapture: 'hsla(0 100% 50% / 0.8)',
  highlightAnalysis: 'hsla(120 100% 40% / 0.6)',
  textLight: 'hsla(0 0% 100% / 0.9)',
  textDark: 'hsla(0 0% 0% / 0.9)',
}

/**
 * All default themes with metadata
 */
export const DEFAULT_THEMES = [
  {
    name: 'Classic',
    description: 'The default modern dark theme',
    colors: THEME_CLASSIC,
  },
  {
    name: 'Wood',
    description: 'Warm wooden board with natural tones',
    colors: THEME_WOOD,
  },
  {
    name: 'Ocean',
    description: 'Cool blue ocean-inspired theme',
    colors: THEME_OCEAN,
  },
  {
    name: 'Forest',
    description: 'Deep green forest theme',
    colors: THEME_FOREST,
  },
  {
    name: 'High Contrast',
    description: 'High contrast theme for better visibility',
    colors: THEME_HIGH_CONTRAST,
  },
]
