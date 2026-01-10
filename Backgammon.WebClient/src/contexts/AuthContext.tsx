/* eslint-disable react-refresh/only-export-components */
import React, { createContext, useContext, useState, useEffect, ReactNode } from 'react'
import { User, LoginRequest, RegisterRequest } from '@/types/game.types'
import { authService } from '@/services/auth.service'

interface AuthContextType {
  user: User | null
  token: string | null
  isAuthenticated: boolean
  isLoading: boolean
  isReady: boolean // True when auth initialization is complete (including anonymous registration)
  login: (credentials: LoginRequest) => Promise<void>
  register: (data: RegisterRequest) => Promise<void>
  logout: () => void
  getEffectivePlayerId: () => string
}

const AuthContext = createContext<AuthContextType | undefined>(undefined)

export const useAuth = () => {
  const context = useContext(AuthContext)
  if (!context) {
    throw new Error('useAuth must be used within an AuthProvider')
  }
  return context
}

interface AuthProviderProps {
  children: ReactNode
}

export const AuthProvider: React.FC<AuthProviderProps> = ({ children }) => {
  const [user, setUser] = useState<User | null>(null)
  const [token, setToken] = useState<string | null>(null)
  const [isLoading, setIsLoading] = useState(true)
  const [isReady, setIsReady] = useState(false)

  // Initialize auth state on mount
  useEffect(() => {
    const initAuth = async () => {
      try {
        console.log('[AuthContext] Starting authentication initialization...')

        // Initialize services
        await authService.initialize()

        // CRITICAL: Complete registration/login BEFORE SignalR connects
        // This ensures JWT with displayName exists before any SignalR operations
        await authService.restoreSession()

        console.log('[AuthContext] Authentication complete, fetching user state...')

        // Restore user from localStorage (only if token still valid after validation)
        const storedUser = authService.getUser()
        const storedToken = authService.getToken()

        if (storedUser && storedToken) {
          setUser(storedUser)
          setToken(storedToken)
          console.log('[AuthContext] User authenticated:', storedUser.username)
        } else {
          // Token was invalid, ensure state is cleared
          setUser(null)
          setToken(null)
          console.log('[AuthContext] No valid authentication found')
        }

        // Mark as ready - SignalR can now safely connect
        setIsReady(true)
        console.log('[AuthContext] Auth initialization complete - ready for SignalR connection')
      } catch (error) {
        console.error('[AuthContext] Failed to initialize:', error)
        // Clear auth state on error
        setUser(null)
        setToken(null)
        setIsReady(true) // Still mark as ready to not block the app
      } finally {
        setIsLoading(false)
      }
    }

    initAuth()
  }, [])

  const login = async (credentials: LoginRequest) => {
    try {
      const response = await authService.login(credentials)

      if (response) {
        const newUser: User = {
          userId: response.user.userId,
          username: response.user.username,
          email: null,
          createdAt: response.user.createdAt,
          rating: response.user.rating,
          peakRating: response.user.peakRating,
          ratedGamesCount: response.user.ratedGamesCount,
        }

        setUser(newUser)
        setToken(response.token)
      }
    } catch (error) {
      console.error('[AuthContext] Login failed:', error)
      throw error
    }
  }

  const register = async (data: RegisterRequest) => {
    try {
      const response = await authService.register(data)

      if (response) {
        const newUser: User = {
          userId: response.user.userId,
          username: response.user.username,
          email: data.email || null,
          createdAt: response.user.createdAt,
          rating: response.user.rating,
          peakRating: response.user.peakRating,
          ratedGamesCount: response.user.ratedGamesCount,
        }

        setUser(newUser)
        setToken(response.token)
      }
    } catch (error) {
      console.error('[AuthContext] Registration failed:', error)
      throw error
    }
  }

  const logout = () => {
    authService.logout()
    setUser(null)
    setToken(null)
  }

  const getEffectivePlayerId = (): string => {
    return authService.getEffectivePlayerId()
  }

  const value: AuthContextType = {
    user,
    token,
    isAuthenticated: !!token,
    isLoading,
    isReady,
    login,
    register,
    logout,
    getEffectivePlayerId,
  }

  return <AuthContext.Provider value={value}>{children}</AuthContext.Provider>
}
