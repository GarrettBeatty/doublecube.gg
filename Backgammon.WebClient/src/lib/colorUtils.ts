/**
 * Color conversion utilities for theme editor.
 * Converts between HSL strings (used by themes) and hex (used by color pickers).
 */

/**
 * Common CSS named colors mapped to hex values.
 */
const NAMED_COLORS: Record<string, string> = {
  white: '#ffffff',
  black: '#000000',
  red: '#ff0000',
  green: '#008000',
  blue: '#0000ff',
  yellow: '#ffff00',
  cyan: '#00ffff',
  magenta: '#ff00ff',
  gray: '#808080',
  grey: '#808080',
  silver: '#c0c0c0',
  maroon: '#800000',
  olive: '#808000',
  lime: '#00ff00',
  aqua: '#00ffff',
  teal: '#008080',
  navy: '#000080',
  fuchsia: '#ff00ff',
  purple: '#800080',
  orange: '#ffa500',
  pink: '#ffc0cb',
  brown: '#a52a2a',
  transparent: '#00000000',
}

/**
 * Parse HSL/HSLA string to components.
 * Supports formats: "hsl(0 0% 14%)", "hsla(47.9 95.8% 53.1% / 0.6)", "#ffffff", "white"
 */
function parseHslString(hsl: string): { h: number; s: number; l: number; a: number } | null {
  // Handle hex values
  if (hsl.startsWith('#')) {
    const rgb = hexToRgb(hsl)
    if (!rgb) return null
    const { h, s, l } = rgbToHsl(rgb.r, rgb.g, rgb.b)
    return { h, s, l, a: 1 }
  }

  // Handle named colors
  const namedHex = NAMED_COLORS[hsl.toLowerCase()]
  if (namedHex) {
    const rgb = hexToRgb(namedHex)
    if (!rgb) return null
    const { h, s, l } = rgbToHsl(rgb.r, rgb.g, rgb.b)
    return { h, s, l, a: 1 }
  }

  // Handle HSL format
  if (!hsl.includes('hsl')) {
    return null
  }

  // Match hsl(h s% l%) or hsla(h s% l% / a) format
  const match = hsl.match(/hsla?\(\s*([\d.]+)\s+([\d.]+)%\s+([\d.]+)%\s*(?:\/\s*([\d.]+))?\s*\)/)
  if (!match) return null

  return {
    h: parseFloat(match[1]),
    s: parseFloat(match[2]),
    l: parseFloat(match[3]),
    a: match[4] ? parseFloat(match[4]) : 1,
  }
}

/**
 * Convert hex color to RGB.
 */
function hexToRgb(hex: string): { r: number; g: number; b: number } | null {
  const result = /^#?([a-f\d]{2})([a-f\d]{2})([a-f\d]{2})$/i.exec(hex)
  if (!result) return null
  return {
    r: parseInt(result[1], 16),
    g: parseInt(result[2], 16),
    b: parseInt(result[3], 16),
  }
}

/**
 * Convert RGB to HSL.
 */
function rgbToHsl(r: number, g: number, b: number): { h: number; s: number; l: number } {
  r /= 255
  g /= 255
  b /= 255

  const max = Math.max(r, g, b)
  const min = Math.min(r, g, b)
  let h = 0
  let s = 0
  const l = (max + min) / 2

  if (max !== min) {
    const d = max - min
    s = l > 0.5 ? d / (2 - max - min) : d / (max + min)

    switch (max) {
      case r:
        h = ((g - b) / d + (g < b ? 6 : 0)) / 6
        break
      case g:
        h = ((b - r) / d + 2) / 6
        break
      case b:
        h = ((r - g) / d + 4) / 6
        break
    }
  }

  return {
    h: Math.round(h * 360 * 10) / 10,
    s: Math.round(s * 100 * 10) / 10,
    l: Math.round(l * 100 * 10) / 10,
  }
}

/**
 * Convert HSL to RGB.
 */
function hslToRgb(h: number, s: number, l: number): { r: number; g: number; b: number } {
  h /= 360
  s /= 100
  l /= 100

  let r: number, g: number, b: number

  if (s === 0) {
    r = g = b = l
  } else {
    const hue2rgb = (p: number, q: number, t: number) => {
      if (t < 0) t += 1
      if (t > 1) t -= 1
      if (t < 1 / 6) return p + (q - p) * 6 * t
      if (t < 1 / 2) return q
      if (t < 2 / 3) return p + (q - p) * (2 / 3 - t) * 6
      return p
    }

    const q = l < 0.5 ? l * (1 + s) : l + s - l * s
    const p = 2 * l - q
    r = hue2rgb(p, q, h + 1 / 3)
    g = hue2rgb(p, q, h)
    b = hue2rgb(p, q, h - 1 / 3)
  }

  return {
    r: Math.round(r * 255),
    g: Math.round(g * 255),
    b: Math.round(b * 255),
  }
}

/**
 * Convert RGB to hex string.
 */
function rgbToHex(r: number, g: number, b: number): string {
  return '#' + [r, g, b].map((x) => x.toString(16).padStart(2, '0')).join('')
}

/**
 * Convert HSL string to hex color.
 * Input: "hsl(0 0% 14%)" or "hsla(47.9 95.8% 53.1% / 0.6)" or "#ffffff" or "white"
 * Output: "#242424"
 */
export function hslToHex(hsl: string): string {
  // Already hex
  if (hsl.startsWith('#')) {
    return hsl.length === 7 ? hsl : hsl.slice(0, 7)
  }

  // Handle named colors directly
  const namedHex = NAMED_COLORS[hsl.toLowerCase()]
  if (namedHex) {
    return namedHex.slice(0, 7) // Remove alpha if present
  }

  const parsed = parseHslString(hsl)
  if (!parsed) {
    console.warn('Could not parse HSL color:', hsl)
    return '#000000'
  }

  const { r, g, b } = hslToRgb(parsed.h, parsed.s, parsed.l)
  return rgbToHex(r, g, b)
}

/**
 * Convert hex color to HSL string.
 * Input: "#242424"
 * Output: "hsl(0 0% 14.1%)"
 */
export function hexToHsl(hex: string): string {
  const rgb = hexToRgb(hex)
  if (!rgb) {
    console.warn('Could not parse hex color:', hex)
    return 'hsl(0 0% 0%)'
  }

  const { h, s, l } = rgbToHsl(rgb.r, rgb.g, rgb.b)
  return `hsl(${h} ${s}% ${l}%)`
}

/**
 * Convert hex color to HSLA string with alpha.
 * Input: "#242424", 0.6
 * Output: "hsla(0 0% 14.1% / 0.6)"
 */
export function hexToHsla(hex: string, alpha: number): string {
  const rgb = hexToRgb(hex)
  if (!rgb) {
    console.warn('Could not parse hex color:', hex)
    return 'hsla(0 0% 0% / 1)'
  }

  const { h, s, l } = rgbToHsl(rgb.r, rgb.g, rgb.b)
  return `hsla(${h} ${s}% ${l}% / ${alpha})`
}

/**
 * Get alpha value from HSL/HSLA string.
 */
export function getAlpha(hsl: string): number {
  const parsed = parseHslString(hsl)
  return parsed?.a ?? 1
}
