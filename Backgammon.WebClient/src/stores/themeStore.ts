import { create } from 'zustand'
import { persist } from 'zustand/middleware'
import type { BoardTheme, ThemeColors } from '@/types/theme.types'
import { themeService } from '@/services/theme.service'

// Default theme colors (matches current BOARD_COLORS)
export const DEFAULT_THEME_COLORS: ThemeColors = {
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

interface ThemeState {
  // State
  selectedThemeId: string | null
  currentTheme: BoardTheme | null
  isLoading: boolean
  error: string | null

  // Preview state (for theme editor)
  previewColors: Partial<ThemeColors> | null

  // Computed colors (combines preview over theme over defaults)
  resolvedColors: ThemeColors

  // Actions
  setSelectedTheme: (themeId: string | null) => Promise<void>
  loadUserPreference: () => Promise<void>
  setPreviewColors: (colors: Partial<ThemeColors> | null) => void
  clearPreview: () => void
  refreshTheme: () => Promise<void>
}

export const useThemeStore = create<ThemeState>()(
  persist(
    (set, get) => ({
      // Initial state
      selectedThemeId: null,
      currentTheme: null,
      isLoading: false,
      error: null,
      previewColors: null,
      resolvedColors: DEFAULT_THEME_COLORS,

      // Set and persist selected theme
      setSelectedTheme: async (themeId: string | null) => {
        set({ isLoading: true, error: null })

        try {
          // Load the theme data if a theme is selected
          let theme: BoardTheme | null = null
          if (themeId) {
            theme = await themeService.getThemeById(themeId)
            if (!theme) {
              set({
                error: 'Theme not found',
                isLoading: false,
              })
              return
            }
          }

          // Try to save preference to server (will fail if not logged in)
          try {
            await themeService.setThemePreference(themeId)
          } catch {
            // Silently fail - theme will still be applied locally
            console.warn('Could not save theme preference to server')
          }

          // Compute resolved colors
          const resolvedColors = theme
            ? { ...DEFAULT_THEME_COLORS, ...theme.colors }
            : DEFAULT_THEME_COLORS

          set({
            selectedThemeId: themeId,
            currentTheme: theme,
            resolvedColors,
            previewColors: null,
            isLoading: false,
          })
        } catch (error) {
          set({
            error: error instanceof Error ? error.message : 'Failed to set theme',
            isLoading: false,
          })
        }
      },

      // Load user's theme preference from server
      loadUserPreference: async () => {
        try {
          const { selectedThemeId } = await themeService.getThemePreference()
          if (selectedThemeId) {
            await get().setSelectedTheme(selectedThemeId)
          }
        } catch {
          // User might not be logged in, use local storage value
          console.warn('Could not load theme preference from server')
        }
      },

      // Set preview colors (for theme editor)
      setPreviewColors: (colors: Partial<ThemeColors> | null) => {
        const state = get()
        const baseColors = state.currentTheme?.colors ?? DEFAULT_THEME_COLORS

        const resolvedColors = colors
          ? { ...DEFAULT_THEME_COLORS, ...baseColors, ...colors }
          : state.currentTheme
            ? { ...DEFAULT_THEME_COLORS, ...state.currentTheme.colors }
            : DEFAULT_THEME_COLORS

        set({ previewColors: colors, resolvedColors })
      },

      // Clear preview colors
      clearPreview: () => {
        const state = get()
        const resolvedColors = state.currentTheme
          ? { ...DEFAULT_THEME_COLORS, ...state.currentTheme.colors }
          : DEFAULT_THEME_COLORS

        set({ previewColors: null, resolvedColors })
      },

      // Refresh current theme from server
      refreshTheme: async () => {
        const { selectedThemeId } = get()
        if (selectedThemeId) {
          await get().setSelectedTheme(selectedThemeId)
        }
      },
    }),
    {
      name: 'backgammon_theme',
      partialize: (state) => ({
        selectedThemeId: state.selectedThemeId,
      }),
    }
  )
)

// Convenience hook to get just the resolved colors
export function useThemeColors(): ThemeColors {
  return useThemeStore((state) => state.resolvedColors)
}
