import type {
  BoardTheme,
  ThemeListResponse,
  CreateThemeRequest,
  UpdateThemeRequest,
} from '@/types/theme.types'

const API_BASE = import.meta.env.VITE_API_URL || 'http://localhost:5000'

function getAuthHeaders(): HeadersInit {
  const token = localStorage.getItem('backgammon_token')
  return token
    ? {
        'Content-Type': 'application/json',
        Authorization: `Bearer ${token}`,
      }
    : {
        'Content-Type': 'application/json',
      }
}

export const themeService = {
  /**
   * Get public themes (paginated)
   */
  async getPublicThemes(
    limit: number = 50,
    cursor?: string
  ): Promise<ThemeListResponse> {
    const params = new URLSearchParams({ limit: limit.toString() })
    if (cursor) params.append('cursor', cursor)

    const response = await fetch(`${API_BASE}/api/themes?${params}`)
    if (!response.ok) {
      throw new Error('Failed to fetch themes')
    }
    return response.json()
  },

  /**
   * Get default (system) themes
   */
  async getDefaultThemes(): Promise<BoardTheme[]> {
    const response = await fetch(`${API_BASE}/api/themes/defaults`)
    if (!response.ok) {
      throw new Error('Failed to fetch default themes')
    }
    return response.json()
  },

  /**
   * Get a theme by ID
   */
  async getThemeById(themeId: string): Promise<BoardTheme | null> {
    const response = await fetch(`${API_BASE}/api/themes/${themeId}`)
    if (response.status === 404) {
      return null
    }
    if (!response.ok) {
      throw new Error('Failed to fetch theme')
    }
    return response.json()
  },

  /**
   * Get current user's created themes
   */
  async getMyThemes(): Promise<BoardTheme[]> {
    const response = await fetch(`${API_BASE}/api/themes/my`, {
      headers: getAuthHeaders(),
    })
    if (!response.ok) {
      throw new Error('Failed to fetch user themes')
    }
    return response.json()
  },

  /**
   * Create a new theme
   */
  async createTheme(request: CreateThemeRequest): Promise<BoardTheme> {
    const response = await fetch(`${API_BASE}/api/themes`, {
      method: 'POST',
      headers: getAuthHeaders(),
      body: JSON.stringify(request),
    })
    if (!response.ok) {
      throw new Error('Failed to create theme')
    }
    return response.json()
  },

  /**
   * Update an existing theme
   */
  async updateTheme(
    themeId: string,
    request: UpdateThemeRequest
  ): Promise<BoardTheme> {
    const response = await fetch(`${API_BASE}/api/themes/${themeId}`, {
      method: 'PUT',
      headers: getAuthHeaders(),
      body: JSON.stringify(request),
    })
    if (!response.ok) {
      throw new Error('Failed to update theme')
    }
    return response.json()
  },

  /**
   * Delete a theme
   */
  async deleteTheme(themeId: string): Promise<void> {
    const response = await fetch(`${API_BASE}/api/themes/${themeId}`, {
      method: 'DELETE',
      headers: getAuthHeaders(),
    })
    if (!response.ok) {
      throw new Error('Failed to delete theme')
    }
  },

  /**
   * Like a theme
   */
  async likeTheme(themeId: string): Promise<void> {
    const response = await fetch(`${API_BASE}/api/themes/${themeId}/like`, {
      method: 'POST',
      headers: getAuthHeaders(),
    })
    if (!response.ok) {
      throw new Error('Failed to like theme')
    }
  },

  /**
   * Unlike a theme
   */
  async unlikeTheme(themeId: string): Promise<void> {
    const response = await fetch(`${API_BASE}/api/themes/${themeId}/like`, {
      method: 'DELETE',
      headers: getAuthHeaders(),
    })
    if (!response.ok) {
      throw new Error('Failed to unlike theme')
    }
  },

  /**
   * Get user's theme preference
   */
  async getThemePreference(): Promise<{ selectedThemeId: string | null }> {
    const response = await fetch(`${API_BASE}/api/themes/preference`, {
      headers: getAuthHeaders(),
    })
    if (!response.ok) {
      throw new Error('Failed to fetch theme preference')
    }
    return response.json()
  },

  /**
   * Set user's theme preference
   */
  async setThemePreference(themeId: string | null): Promise<void> {
    const params = themeId ? `?themeId=${themeId}` : ''
    const response = await fetch(`${API_BASE}/api/themes/preference${params}`, {
      method: 'PUT',
      headers: getAuthHeaders(),
    })
    if (!response.ok) {
      throw new Error('Failed to set theme preference')
    }
  },

  /**
   * Search themes by name
   */
  async searchThemes(query: string): Promise<BoardTheme[]> {
    const response = await fetch(
      `${API_BASE}/api/themes/search?q=${encodeURIComponent(query)}`
    )
    if (!response.ok) {
      throw new Error('Failed to search themes')
    }
    return response.json()
  },
}
