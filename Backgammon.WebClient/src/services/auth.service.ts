import {
  AuthResponse,
  LoginRequest,
  RegisterRequest,
  User,
} from '@/types/game.types'
import { apiService } from './api.service'

const TOKEN_KEY = 'auth_token'
const USER_KEY = 'auth_user'
const PLAYER_ID_KEY = 'playerId'

class AuthService {
  private baseUrl: string = ''

  async initialize(): Promise<void> {
    // Wait for API service to initialize
    await apiService.initialize()
    this.baseUrl = apiService['baseUrl'] // Access private baseUrl
  }

  async login(credentials: LoginRequest): Promise<AuthResponse | null> {
    try {
      const response = await fetch(`${this.baseUrl}/api/auth/login`, {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify(credentials),
      })

      if (!response.ok) {
        const error = await response.text()
        throw new Error(error || 'Login failed')
      }

      const data: AuthResponse = await response.json()

      // Store token and user info
      this.setToken(data.token)
      this.setUser({
        userId: data.user.userId,
        username: data.user.username,
        displayName: data.user.displayName,
        email: null,
        createdAt: data.user.createdAt,
        rating: data.user.rating,
        peakRating: data.user.peakRating,
        ratedGamesCount: data.user.ratedGamesCount,
      })

      // Update API service with token
      apiService.setAuthToken(data.token)

      return data
    } catch (error) {
      console.error('[AuthService] Login failed:', error)
      throw error
    }
  }

  async register(data: RegisterRequest): Promise<AuthResponse | null> {
    try {
      const response = await fetch(`${this.baseUrl}/api/auth/register`, {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify(data),
      })

      if (!response.ok) {
        const error = await response.text()
        throw new Error(error || 'Registration failed')
      }

      const authData: AuthResponse = await response.json()

      // Store token and user info
      this.setToken(authData.token)
      this.setUser({
        userId: authData.user.userId,
        username: authData.user.username,
        displayName: authData.user.displayName,
        email: data.email || null,
        createdAt: authData.user.createdAt,
        rating: authData.user.rating,
        peakRating: authData.user.peakRating,
        ratedGamesCount: authData.user.ratedGamesCount,
      })

      // Update API service with token
      apiService.setAuthToken(authData.token)

      return authData
    } catch (error) {
      console.error('[AuthService] Registration failed:', error)
      throw error
    }
  }

  logout(): void {
    localStorage.removeItem(TOKEN_KEY)
    localStorage.removeItem(USER_KEY)
    apiService.setAuthToken(null)
  }

  getToken(): string | null {
    return localStorage.getItem(TOKEN_KEY)
  }

  setToken(token: string): void {
    localStorage.setItem(TOKEN_KEY, token)
  }

  getUser(): User | null {
    const userJson = localStorage.getItem(USER_KEY)
    return userJson ? JSON.parse(userJson) : null
  }

  setUser(user: User): void {
    localStorage.setItem(USER_KEY, JSON.stringify(user))
  }

  isAuthenticated(): boolean {
    return !!this.getToken()
  }

  // Player ID management (for anonymous users)
  getOrCreatePlayerId(): string {
    let playerId = localStorage.getItem(PLAYER_ID_KEY)

    if (!playerId) {
      playerId = this.generatePlayerId()
      localStorage.setItem(PLAYER_ID_KEY, playerId)
    }

    return playerId
  }

  getEffectivePlayerId(): string {
    const user = this.getUser()
    return user?.userId || this.getOrCreatePlayerId()
  }

  getDisplayName(): string | null {
    const user = this.getUser()
    return user?.displayName || null
  }

  private generatePlayerId(): string {
    return `player_${Date.now()}_${Math.random().toString(36).substr(2, 9)}`
  }

  // Initialize auth state on app load
  async restoreSession(): Promise<void> {
    const token = this.getToken()
    if (token) {
      apiService.setAuthToken(token)

      // Validate token with server - if user no longer exists, clear session
      try {
        const response = await fetch(`${this.baseUrl}/api/auth/me`, {
          headers: {
            Authorization: `Bearer ${token}`,
          },
        })

        if (!response.ok) {
          // Token invalid or user deleted - clear session
          console.warn('[AuthService] Token validation failed, clearing session')
          this.logout()
        }
      } catch (error) {
        console.error('[AuthService] Failed to validate token:', error)
        // Clear session on network error to be safe
        this.logout()
      }
    }
  }
}

export const authService = new AuthService()
