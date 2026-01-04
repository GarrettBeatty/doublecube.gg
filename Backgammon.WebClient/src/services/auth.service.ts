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
        userId: data.userId,
        username: data.username,
        email: null,
        createdAt: new Date().toISOString(),
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
        userId: authData.userId,
        username: authData.username,
        email: data.email || null,
        createdAt: new Date().toISOString(),
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
    return user?.username || null
  }

  private generatePlayerId(): string {
    return `player_${Date.now()}_${Math.random().toString(36).substr(2, 9)}`
  }

  // Initialize auth state on app load
  async restoreSession(): Promise<void> {
    const token = this.getToken()
    if (token) {
      apiService.setAuthToken(token)
    }
  }
}

export const authService = new AuthService()
