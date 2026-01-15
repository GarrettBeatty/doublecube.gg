/**
 * Board theme types for customizable board appearance
 */

/**
 * All customizable colors for a board theme
 */
export interface ThemeColors {
  // Board structure
  boardBackground: string
  boardBorder: string
  bar: string
  bearoff: string

  // Triangles/Points
  pointLight: string
  pointDark: string

  // Checkers
  checkerWhite: string
  checkerWhiteStroke: string
  checkerRed: string
  checkerRedStroke: string

  // Dice
  diceBackground: string
  diceDots: string

  // Doubling cube
  doublingCubeBackground: string
  doublingCubeStroke: string
  doublingCubeText: string

  // Highlights (game state feedback)
  highlightSource: string
  highlightSelected: string
  highlightDest: string
  highlightCapture: string
  highlightAnalysis: string

  // Text
  textLight: string
  textDark: string
}

/**
 * Theme visibility options
 */
export type ThemeVisibility = 'Public' | 'Private' | 'Unlisted'

/**
 * Full board theme with metadata
 */
export interface BoardTheme {
  themeId: string
  name: string
  description: string
  authorId: string
  authorUsername: string
  visibility: ThemeVisibility
  isDefault: boolean
  createdAt: string
  updatedAt: string
  usageCount: number
  likeCount: number
  colors: ThemeColors
  thumbnailUrl?: string
}

/**
 * Summary view of a theme for listings
 */
export interface ThemeSummary {
  themeId: string
  name: string
  description: string
  authorId: string
  authorUsername: string
  thumbnailUrl?: string
  usageCount: number
  likeCount: number
  isDefault: boolean
  isLikedByUser?: boolean
}

/**
 * Request payload for creating a new theme
 */
export interface CreateThemeRequest {
  name: string
  description: string
  visibility: ThemeVisibility
  colors: ThemeColors
}

/**
 * Request payload for updating a theme
 */
export interface UpdateThemeRequest {
  name?: string
  description?: string
  visibility?: ThemeVisibility
  colors?: Partial<ThemeColors>
}

/**
 * Response for paginated theme listings
 */
export interface ThemeListResponse {
  themes: BoardTheme[]
  nextCursor?: string
  totalCount?: number
}

/**
 * Theme sort options for browsing
 */
export type ThemeSortOption = 'popular' | 'recent' | 'name' | 'likes'

/**
 * Theme filter options
 */
export interface ThemeFilters {
  search?: string
  authorId?: string
  sort?: ThemeSortOption
  includeDefaults?: boolean
}
