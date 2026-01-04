/* eslint-disable react-refresh/only-export-components */
import React, { createContext, useContext, useState, useEffect, ReactNode } from 'react'
import { User, LoginRequest, RegisterRequest } from '@/types/game.types'
import { authService } from '@/services/auth.service'

interface AuthContextType {
  user: User | null
  token: string | null
  isAuthenticated: boolean
  isLoading: boolean
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

  // Initialize auth state on mount
  useEffect(() => {
    const initAuth = async () => {
      try {
        // Initialize services
        await authService.initialize()
        await authService.restoreSession()

        // Restore user from localStorage
        const storedUser = authService.getUser()
        const storedToken = authService.getToken()

        if (storedUser && storedToken) {
          setUser(storedUser)
          setToken(storedToken)
        }
      } catch (error) {
        console.error('[AuthContext] Failed to initialize:', error)
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
    login,
    register,
    logout,
    getEffectivePlayerId,
  }

  return <AuthContext.Provider value={value}>{children}</AuthContext.Provider>
}
